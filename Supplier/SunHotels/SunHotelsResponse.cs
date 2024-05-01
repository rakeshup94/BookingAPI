using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
//using TravillioXMLOutService.Models.Juniper;
using TravillioXMLOutService.Models.SunHotels;
using System.IO;
using System.Configuration;
using TravillioXMLOutService.Models;
using System.Data;
using System.Threading;
using System.Text;

namespace TravillioXMLOutService.Supplier.SunHotels
{
    public class SunHotelsResponse
    {
        //XDocument citymapping = XDocument.Load(Path.Combine(HttpRuntime.AppDomainAppPath, ConfigurationManager.AppSettings["RestelPath"] + @"RestelCityMapping.xml"));
        XElement citymapping = null;
        int _chunksize = 100;
        //string XmlPath = ConfigurationManager.AppSettings["RestelPath"];
        SunHotelOuter serverRequest = new SunHotelOuter();
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        XNamespace xsd = "http://www.w3.org/2001/XMLSchema";
        XNamespace soap12 = "http://www.w3.org/2003/05/soap-envelope";
        XNamespace sunhotel = "http://xml.sunhotels.net/15/";

        SunHotelData jdata = new SunHotelData();
        XNamespace soap = "http://www.juniper.es/webservice/2007/";
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        string imageURL = "http://www.sunhotels.net/Sunhotels.net/HotelInfo/hotelImage.aspx";
        XDocument serverResponse;
        //string JunpVersion = "1.1";
        int _SupplierID;
        string _SupplierName = "", _HostUrl = "", _HotSearURL = "", _HotPreBookURL = "", _HotBookURL = "", _HotBookCanURL = "";
        string _LoginID = "", _Password = "", _AgentEmailID = "", _Version = "";
        string dmc = string.Empty, SupplierCurrency = string.Empty, hotelcode = string.Empty;
        string customerid = string.Empty;

        public SunHotelsResponse(int SuppID, int CustID)
        {
            _SupplierID = SuppID;
            SupplierConfiguration(SuppID, CustID);
        }
        public SunHotelsResponse()
        {
        }
        #region Hotel Search
        public List<XElement> ThreadedHotelSearch(XElement Req, string custID, string xtype)
        {
            customerid = custID;
            dmc = xtype;
            List<XElement> HotelList = new List<XElement>();
            try
            {
                string HtId = Req.Descendants("HotelID").FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(HtId))
                {
                    using (SunHotelService sunSrv = new SunHotelService(customerid, _SupplierID.ToString(), xtype))
                    {
                        var list = sunSrv.SearchByHotel(Req);
                        if(list == null)
                        {
                            return null;
                        }
                        HotelList = list.Descendants("Hotel").ToList();
                    }
                }
                else
                {

                    DataTable cityMapping = jdata.CityMapping(Req.Descendants("CityID").FirstOrDefault().Value, _SupplierID.ToString());
                    if (cityMapping.Rows.Count == 0)
                        return null;
                    string suppliercity = cityMapping.Rows[0]["SupCityId"].ToString();
                    List<string> hotelIDs = new List<string>();
                    List<string> citiesWithResult = new List<string>();
                    for (int i = 0; i < cityMapping.Rows.Count; i++)
                    {
                        List<string> citywiseHotels = HotelCodes(cityMapping.Rows[i]["SupCityId"].ToString());
                        foreach (string hid in citywiseHotels)
                        {
                            citiesWithResult.Add(cityMapping.Rows[i]["SupCityId"].ToString());
                            if (!hotelIDs.Contains(hid))
                            {
                                hotelIDs.Add(hid);
                            }
                        }
                    }
                    if (hotelIDs.Count == 0)
                        return null;
                    var chunklist = BreakIntoChunks(hotelIDs, _chunksize);
                    int Number = chunklist.Count;

                    List<XElement> tr1 = new List<XElement>();
                    List<XElement> tr2 = new List<XElement>();
                    List<XElement> tr3 = new List<XElement>();
                    List<XElement> tr4 = new List<XElement>();
                    List<XElement> tr5 = new List<XElement>();
                    try
                    {
                        for (int i = 0; i < Number; i += 5)
                        {
                            List<Thread> threadedlist;
                            int rangecount = 5;
                            if (chunklist.Count - i < 5)
                                rangecount = chunklist.Count - i;
                            var chn = chunklist.GetRange(i, rangecount);
                            #region rangecount equals 1
                            if (rangecount == 1)
                            {
                                threadedlist = new List<Thread>
                       {   
                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult))                       
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join());
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
                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult))                       
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join());
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
                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult)),
                           new Thread(()=> tr3 = HotelSearch(Req, chunklist.ElementAt(i+2),citiesWithResult))
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join());
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

                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult)),
                           new Thread(()=> tr3 = HotelSearch(Req, chunklist.ElementAt(i+2),citiesWithResult)),
                           new Thread(()=> tr4 = HotelSearch(Req, chunklist.ElementAt(i+3),citiesWithResult))

                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join());
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

                           new Thread(()=> tr1 = HotelSearch(Req, chunklist.ElementAt(i),citiesWithResult)),
                           new Thread(()=> tr2 = HotelSearch(Req, chunklist.ElementAt(i+1),citiesWithResult)),
                           new Thread(()=> tr3 = HotelSearch(Req, chunklist.ElementAt(i+2),citiesWithResult)),
                           new Thread(()=> tr4 = HotelSearch(Req, chunklist.ElementAt(i+3),citiesWithResult)),
                           new Thread(()=> tr5 = HotelSearch(Req, chunklist.ElementAt(i+4),citiesWithResult))
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join());
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
                            //}
                        }
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "ThreadedHotelSearch";
                        ex1.PageName = "SunHotelsResponses";
                        ex1.CustomerID = customerid;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        APILog.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    removetags(Req);
                }
                return HotelList;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ThreadedHotelSearch";
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                return null;
                #endregion
            }
        }
        public List<XElement> HotelSearch(XElement Req, List<string> chunkthread, List<string> CitiesWithHotels)
        {
            string cityID = Req.Descendants("CityID").FirstOrDefault().Value;
            List<XElement> ThreadResult = new List<XElement>();
            string listofhotels = null;
            try
            {
                foreach (var entry in chunkthread)
                    listofhotels += entry + ",";
                string strAdult = "";
                string strChildAge = "";
                string strInfant = "";
                var paxnumber = GetPaxNumber(Req);
                int adult = paxnumber.Item1, child = paxnumber.Item2, infant = paxnumber.Item3;
                strChildAge = paxnumber.Item4;
                #region Request XML
                XDocument HotelsearchRequest = new XDocument(
                                  new XDeclaration("1.0", "utf-8", "yes"),
                                  new XElement(soap12 + "Envelope",
                                      new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                      new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                      new XAttribute(XNamespace.Xmlns + "soap12", soap12),
                                      new XElement(soap12 + "Body",
                                         new XElement(sunhotel + "SearchV2",
                                           new XAttribute(XNamespace.None + "xmlns", sunhotel),
                                                            new XElement(sunhotel + "userName", _LoginID),
                                                            new XElement(sunhotel + "password", _Password),
                                                            new XElement(sunhotel + "language", "en"),
                                                            new XElement(sunhotel + "currencies", "USD"),
                                                            new XElement(sunhotel + "checkInDate", JuniperDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                                            new XElement(sunhotel + "checkOutDate", JuniperDate(Req.Descendants("ToDate").FirstOrDefault().Value)),
                                                            new XElement(sunhotel + "numberOfRooms", Req.Descendants("RoomPax").Count()),
                    //new XElement(sunhotel + "destinationID", ""),
                    //new XElement(sunhotel + "hotelIDs", "USD"),
                                                             new XElement(sunhotel + "resortIDs", listofhotels),
                                                             new XElement(sunhotel + "numberOfAdults", adult.ToString()),
                                                             new XElement(sunhotel + "numberOfChildren", child.ToString()),
                                                             new XElement(sunhotel + "childrenAges", strChildAge),
                                                             new XElement(sunhotel + "infant", infant.ToString()),
                                                             new XElement(sunhotel + "showCoordinates", "1"),
                                                             new XElement(sunhotel + "minStarRating", Req.Descendants("MinStarRating").FirstOrDefault().Value.Equals("0") ? "1" : Req.Descendants("MinStarRating").FirstOrDefault().Value),
                                                             new XElement(sunhotel + "maxStarRating", Req.Descendants("MaxStarRating").FirstOrDefault().Value),
                                                             new XElement(sunhotel + "paymentMethodId", "1"),
                                                             new XElement(sunhotel + "customerCountry", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                                             new XElement(sunhotel + "b2c", "0")))));
                #endregion
                XElement response = HotelSearchResponse(HotelsearchRequest, Req, CitiesWithHotels);
                int count = response.Descendants("hotel").Count();
                foreach (XElement hotel in response.Descendants("Hotel"))
                    ThreadResult.Add(hotel);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearch";
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            return ThreadResult;
        }
        public Tuple<int, int, int, string> GetPaxNumber(XElement Req)
        {
            int adult = 0, child = 0, infant = 0;
            string strChildAge = "";
            int IsInfant = 0;
            foreach (XElement room in Req.Descendants("RoomPax"))
            {
                adult += Convert.ToInt32(room.Element("Adult").Value);
                if (room.Descendants("ChildAge").Any())
                {
                    foreach (XElement age in room.Descendants("ChildAge"))
                    {
                        if (Convert.ToInt32(age.Value) < 2 && infant == 0)
                        {
                            infant++;
                            continue;
                        }
                        child++;
                        if (strChildAge == "")
                        {
                            strChildAge += age.Value.Equals("1") ? "2" : age.Value;
                        }
                        else
                        {
                            strChildAge += age.Value.Equals("1") ? ",2" : "," + age.Value;
                        }
                    }
                }
            }
            if (infant >= 1)
                IsInfant = 1;

            return new Tuple<int, int, int, string>(adult, child, IsInfant, strChildAge);
        }

        #region Response
        public XElement HotelSearchResponse(XDocument toRequest, XElement travReq, List<string> CitiesWithHotels)
        {
            DateTime starttime = DateTime.Now;
            XElement response = null;
            try
            {
                // serverResponse = XDocument.Load("D:\\Projects\\XML Integration\\W2M-Juniper\\Juniper_Response_SearchResult.xml");  //serverRequest.JuniperResponse(toRequest);
                serverResponse = serverRequest.SunHotelResponse(toRequest, "SearchV2", _HotSearURL, Convert.ToInt64(customerid), Convert.ToString(travReq.Descendants("TransID").FirstOrDefault().Value), _SupplierID);
                #region Log Save
                XElement responseRNS = removeAllNamespaces(serverResponse.Root);
                XElement requestRNS = removeAllNamespaces(toRequest.Root);
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(customerid);
                    log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = _SupplierID;
                    log.logrequestXML = requestRNS.ToString();
                    log.logresponseXML = responseRNS.ToString();
                    log.LogType = "Search";
                    log.LogTypeID = 1;
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelSearchResponse";
                    ex1.PageName = "SunHotelsResponses";
                    ex1.CustomerID = customerid;
                    ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion

                List<XElement> Hotels = new List<XElement>();
                List<string> HotelList = responseRNS.Descendants("hotel").Select(x => x.Element("hotel.id").Value).Distinct().ToList();
                StringBuilder HotelsString = new StringBuilder();
                StringBuilder CityString = new StringBuilder();
                foreach (string htl in HotelList)
                {
                    if (HotelsString.Length == 0)
                        HotelsString.Append("\'" + htl + "\'");
                    else
                        HotelsString.Append(",\'" + htl + "\'");
                }

                foreach (string city in CitiesWithHotels.Distinct().ToList())
                {
                    if (CityString.Length == 0)
                        CityString.Append("\'" + city + "\'");
                    else
                        CityString.Append(",\'" + city + "\'");
                }
                DataTable imagesFromDB = jdata.GetSunImages(HotelsString.ToString(), CityString.ToString());
                int count = responseRNS.Descendants("hotel").Count();
                #region Response XML
                if (responseRNS.Descendants("hotel").Any())
                {
                    string xmlouttype = string.Empty;
                    try
                    {
                        if (dmc == "SunHotels")
                        {
                            xmlouttype = "false";
                        }
                        else
                        { xmlouttype = "true"; }
                    }
                    catch { }
                    DataTable StaticData_All = new DataTable();
                    StaticData_All = jdata.GetSunHotelHotelDetails(CityString.ToString());
                    foreach (XElement ResHotels in responseRNS.Descendants("hotel"))
                    {
                        try
                        {
                            string minstar = travReq.Descendants("MinStarRating").FirstOrDefault().Value;
                            string maxstar = travReq.Descendants("MaxStarRating").FirstOrDefault().Value;
                            string hotelID = ResHotels.Element("hotel.id").Value;
                            string hotelstar = "", hotelName = "", hotelAddress = "", hotlatitude = "", hotLognitude = "", largeimage = imageURL, smallimage = imageURL;

                            var result = StaticData_All.AsEnumerable().Where(dt => dt.Field<string>("HotelID") == hotelID);
                            DataRow[] drow = result.ToArray();
                            if (result.ToArray().Length >= 1)
                            {
                                drow[0].ItemArray[1].ToString();
                                hotelName = drow[0].ItemArray[1].ToString();
                                hotelAddress = drow[0].ItemArray[6].ToString();
                                hotelstar = drow[0].ItemArray[2].ToString().TrimStart().Substring(0, 1);
                                hotlatitude = drow[0].ItemArray[4].ToString();
                                hotLognitude = drow[0].ItemArray[5].ToString();
                                var imgResult = imagesFromDB.AsEnumerable().Where(x => x.Field<string>("HotelID").Equals(hotelID));
                                DataRow[] imgRow = imgResult.ToArray();
                                if (imgRow.Count() > 0)
                                {
                                    largeimage += imgRow[0]["FullSizeImage"].ToString() + "&amp";
                                    smallimage += imgRow[0]["SmallImage"].ToString();
                                }
                                if (hotelAddress != null && StarRating(minstar, maxstar, hotelstar))
                                {
                                    try
                                    {

                                        Hotels.Add(new XElement("Hotel",
                                            //new XElement("HotelID", ResHotels.Attribute("Code").Value),
                                                                new XElement("HotelID", hotelID),
                                                                new XElement("HotelName", hotelName),
                                                                new XElement("PropertyTypeName"),
                                                            new XElement("CountryID", travReq.Descendants("CountryID").First().Value),
                                                            new XElement("CountryName", travReq.Descendants("CountryName").First().Value),
                                                            new XElement("CountryCode", travReq.Descendants("CountryCode").First().Value),
                                                            new XElement("CityId", travReq.Descendants("CityID").First().Value),
                                                            new XElement("CityCode", travReq.Descendants("CityCode").First().Value),
                                                            new XElement("CityName", travReq.Descendants("CityName").First().Value),
                                                                new XElement("AreaId"),
                                                                new XElement("AreaName"),
                                                                new XElement("RequestID"),
                                                                new XElement("Address", hotelAddress),
                                                                new XElement("Location"),
                                                                new XElement("Description"),
                                                                new XElement("StarRating", hotelstar),
                                                                new XElement("MinRate", MinRate(ResHotels.Descendants("roomtypes").FirstOrDefault())),
                                                                new XElement("HotelImgSmall", smallimage),
                                                                new XElement("HotelImgLarge", largeimage),
                                                                new XElement("MapLink"),
                                                                new XElement("Longitude", hotLognitude),
                                                                new XElement("Latitude", hotlatitude),
                                                                new XElement("xmloutcustid", customerid),
                                                                new XElement("xmlouttype", xmlouttype),
                                                                new XElement("DMC", dmc),
                                                                new XElement("SupplierID", _SupplierID.ToString()),
                                                                new XElement("Currency", "USD"),
                                                                new XElement("Offers"),
                                                                new XElement("Facilities", null),
                                                                new XElement("Rooms")));
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    }
                }
                #endregion
                response = new XElement("searchResponse", new XElement("Hotels", Hotels));
                if (response.Descendants("HotelID").Any())
                    response.Descendants("Hotel").Where(x => x.DescendantNodes().Count() == 0).Remove();

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearchResponse";
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = customerid;
                ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            return response;
        }
        #endregion
        #endregion
        #region Room Availability
        public XElement GetRoomAvail_sunhotelOUT(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "36").ToList();
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
                            customerid = htlele[i].Attribute("custID").Value;
                            dmc = htlele[i].Attribute("custName").Value;
                        }
                        catch { custName = "SunHotels"; }
                    }
                    else
                    {
                        try
                        {
                            customerid = htlele[i].Attribute("custID").Value;
                        }
                        catch { }
                        dmc = "SunHotels";
                    }
                    SunHotelsResponse objRs = new SunHotelsResponse(36, Convert.ToInt32(customerid));
                    roomavailabilityresponse.Add(objRs.RoomAvailability(req, dmc, htlid, customerid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        public XElement RoomAvailability(XElement Req, string xtype, string htlcode, string custoid)
        {
            try
            {
                customerid = custoid;
                dmc = xtype;
                hotelcode = htlcode;
                DateTime starttime = DateTime.Now;
                string hid = Req.Descendants("HotelID").FirstOrDefault().Value;
                string[] splitid = hid.Split(new char[] { '_' });
                //DataTable staticRoomDetails = jdata.GetSunHotelRoomDetails()//(splitid[0]);
                List<XElement> respon = LogXMLs(Req.Descendants("TransID").FirstOrDefault().Value, 1, _SupplierID);
                XElement respWithHotel = respon.Where(y => y.Descendants("Response").FirstOrDefault().Descendants("hotel")
                                .Where(x => x.Element("hotel.id").Value.Equals(hotelcode)).Any())
                                .FirstOrDefault();
                XElement resp = respWithHotel.Descendants("hotel").Where(x => x.Element("hotel.id").Value.Equals(hotelcode)).FirstOrDefault();
                #region Credentials
                string username = Req.Descendants("UserName").Single().Value;
                string password = Req.Descendants("Password").Single().Value;
                string AgentID = Req.Descendants("AgentID").Single().Value;
                string ServiceType = Req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
                #endregion
                XElement RoomResponse = new XElement("searchResponse");
                #region Log Save
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(customerid);
                    log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = _SupplierID;
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
                    ex1.PageName = "SunHotelsResponses";
                    ex1.CustomerID = customerid;
                    ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                if (resp != null)
                {
                    string hotelstar = "3";// resp.Descendants("HotelCategory").FirstOrDefault().Value.TrimStart().Substring(0, 1);
                    string hotelID = resp.Element("hotel.id").Value;
                    DataTable StaticData = jdata.GetSunHotelRoomDetails();// (hotelID);
                    DataRow dr = StaticData.Rows[0];
                    //string strname = dr["HotelName"].ToString();
                    #region Response XML
                    SupplierCurrency = resp.Descendants("price").FirstOrDefault().Attribute("currency").Value;
                    var availableRooms = new XElement("Hotel",
                        //new XElement("HotelID", resp.Attribute("Code").Value),
                                                   new XElement("HotelID", hotelID),
                                                   new XElement("HotelName"),
                                                   new XElement("PropertyTypeName"),
                        //new XElement("CountryID", Req.Descendants("CountryID").FirstOrDefault().Value),

                                                   //new XElement("CountryCode"),
                        //new XElement("CountryName", Req.Descendants("CountryName").FirstOrDefault().Value),
                        //new XElement("CityId", Req.Descendants("CityID").FirstOrDefault().Value),
                        //new XElement("CityCode", Req.Descendants("CityCode").FirstOrDefault().Value),
                        //new XElement("CityName", Req.Descendants("CityName").FirstOrDefault().Value),
                        //new XElement("AreaName"),
                        //new XElement("AreaId"),
                        //new XElement("Address", resp.Element("hotel.id").Value + "_Address"),
                        //new XElement("Location"),
                        //new XElement("Description"),
                        //new XElement("StarRating", hotelstar),
                        //new XElement("MinRate"),
                        //new XElement("HotelImgSmall", null),
                        //new XElement("HotelImgLarge", null),
                        //new XElement("MapLink"),
                        //new XElement("Longitude", ""),
                        //new XElement("Latitude", ""),
                                                   new XElement("DMC", dmc),
                                                   new XElement("SupplierID", _SupplierID.ToString()),
                                                   new XElement("Currency", resp.Descendants("price").FirstOrDefault().Attribute("currency").Value),
                                                  new XElement("Offers"),
                                                   new XElement("Facilities", null),
                                                   groupedRooms(resp.Descendants("roomtypes").FirstOrDefault(), Req, StaticData));
                    #endregion;
                    RoomResponse.Add(new XElement("Hotels", availableRooms));
                }
                #region Response Format
                removetags(Req);
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
                                                            new XElement(removeAllNamespaces(Req.Descendants("searchRequest").FirstOrDefault())),
                                                           removeAllNamespaces(RoomResponse))));
                #endregion
                return AvailablilityResponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomAvailability";
                ex1.PageName = "SunHotelsResponse";
                ex1.CustomerID = customerid;
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
                ex1.PageName = "SunHotelsResponses";
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
                                                        new XElement(req.Descendants("hoteldescRequest").FirstOrDefault()),
                                                       response)));
            #endregion
            return details;
        }
        #region Response
        public XElement HotelDetailsResponse(XDocument restelreq, XElement travayooReq)
        {
            DataTable cityMapping = jdata.CityMapping(travayooReq.Descendants("CityID").FirstOrDefault().Value, _SupplierID.ToString());
            string suppliercity = cityMapping.Rows[0]["SupCityId"].ToString();
            string hid = travayooReq.Descendants("HotelID").FirstOrDefault().Value;
            string[] splitid = hid.Split(new char[] { '_' });
            DataTable Details = jdata.GetSunSingleHotelDetails(splitid[0]);
            DataTable ImageFromDB = jdata.GetSunImages("\'" + hid + "\'", suppliercity);
            DataTable FacilitiesFromDb = jdata.GetSunHotelFacilities("\'" + hid + "\'");

            DataRow dr = Details.Rows[0];
            string desc = dr["Description"].ToString();
            List<XElement> Imag = new List<XElement>();
            if (ImageFromDB.Rows.Count > 0)
            {

                for (int i = 0; i < ImageFromDB.Rows.Count; i++)
                {
                    Imag.Add(new XElement("Image",
                                    new XAttribute("Path", imageURL + ImageFromDB.Rows[i]["FullSizeImage"].ToString() + "&amp"),
                                    new XAttribute("Caption", string.Empty)));
                }
            }
            List<XElement> Services = new List<XElement>();
            if (FacilitiesFromDb.Rows.Count > 0)
            {
                for (int i = 0; i < FacilitiesFromDb.Rows.Count; i++)
                    Services.Add(new XElement("Facility", FacilitiesFromDb.Rows[i]["FacilityName"].ToString()));
            }
            #region Response XML
            var hotels = new XElement("Hotels",
                              new XElement("Hotel",
                                  new XElement("HotelID", travayooReq.Descendants("HotelID").FirstOrDefault().Value),
                                  new XElement("Description", desc),
                                  new XElement("Images", Imag),
                                  new XElement("Facilities", Services),
                                  new XElement("ContactDetails",
                                      new XElement("Phone", dr["PhoneNumber"].ToString()),
                                      new XElement("Fax", dr["Fax"].ToString())),
                                  new XElement("CheckinTime"),
                                  new XElement("CheckoutTime")
                                  ));
            #endregion
            XElement response = new XElement("hoteldescResponse", hotels);
            return response;
        }
        #endregion
        #endregion
        #region Cancellation Policy Service
        public XElement cancellationPolicy(XElement Req)
        {
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            XElement response = null;

            try
            {

                #region Request XML
                XDocument BookingRulesRequest = null;
                #endregion
                response = cancellationPolicyResponse(BookingRulesRequest, Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancellationPolicy";
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
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
                                                        new XElement(Req.Descendants("hotelcancelpolicyrequest").Any() ? removeAllNamespaces(Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault()) : removeAllNamespaces(Req)),
                                                       removeAllNamespaces(response))));
            #endregion
            return CXpResponse;
        }
        #region Cancellation Policy Response
        public XElement cancellationPolicyResponse(XDocument Request, XElement travReq)
        {
            DateTime starttime = DateTime.Now;
            XElement cancelresponse = new XElement("HotelDetailwithcancellationResponse");
            string hid = travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value;
            XElement supplierResponse = LogXMLs(travReq.Descendants("TransID").FirstOrDefault().Value, 1, _SupplierID).Descendants("hotel")
                                .Where(x => x.Element("hotel.id").Value.Equals(hid))
                                .FirstOrDefault();
            string[] RatePlanCode = travReq.Descendants("Room").FirstOrDefault().Attribute("ID").Value.Split(new char[] { '_' });

            //  XElement room = supplierResponse.Descendants("roomtype").Descendants("room").Descendants("id").Where(x => x.Value.Equals(RatePlanCode[0])).FirstOrDefault().Parent;

            XElement room = (from r in supplierResponse.Descendants("roomtype").Descendants("room")
                             where r.Descendants("id").FirstOrDefault().Value == RatePlanCode[0]
                             select r).FirstOrDefault();


            // XElement room = supplierResponse.Descendants("roomtypes").FirstOrDefault().Descendants("roomtype").FirstOrDefault().Descendants("rooms").FirstOrDefault().Descendants("room").Where(x => x.Element("id").Value.Equals(RatePlanCode[0])).FirstOrDefault();
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
                log.SupplierID = _SupplierID;
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
                ex1.PageName = "SunHotelResponse";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            try
            {
                if (supplierResponse.Descendants("Error").Any())
                {
                    cancelresponse.Add(new XElement("ErrorTxt", supplierResponse.Descendants("Error").FirstOrDefault().Element("Message").Value));
                    return cancelresponse;
                }

                double totalRate = Convert.ToDouble(travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value);
                XElement cp = new XElement("CancellationPolicies");

                #region Cancellation Policy Tag
                foreach (XElement policy in room.Descendants("cancellation_policy"))
                {

                    DateTime dateFrom = DateTime.ParseExact(travReq.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    double rate = 0.0;

                    if (!policy.Descendants("deadline").FirstOrDefault().HasAttributes)
                    {
                        int hoursClx = Convert.ToInt16(policy.Element("deadline").Value);
                        double percent = Convert.ToDouble(policy.Element("percentage").Value);
                        DateTime clxdate = dateFrom.AddHours(-hoursClx);
                        rate = Math.Round(totalRate * percent / 100, 2);

                        cp.Add(new XElement("CancellationPolicy",
                                                          new XAttribute("LastCancellationDate", clxdate.ToString("dd/MM/yyyy")),
                                                          new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                          new XAttribute("NoShowPolicy", "0")));
                    }
                    else
                    {
                        double percent = Convert.ToDouble(policy.Element("percentage").Value);
                        rate = Math.Round(totalRate * percent / 100, 2);
                        cp.Add(new XElement("CancellationPolicy",
                                                          new XAttribute("LastCancellationDate", DateTime.Now.ToString("dd/MM/yyyy")),
                                                          new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                          new XAttribute("NoShowPolicy", "0")));

                    }
                }
                /* Region: To remove the lower cancellation price if two policies have the same date
                 * Added by: Aman Kaushik
                 * Added on: 31/12/2018                 */
                var cpGroups = cp.Descendants("CancellationPolicy").GroupBy(x => x.Attribute("LastCancellationDate").Value);
                foreach (var pol in cpGroups.Where(x => x.Count() > 1))
                {
                    decimal max = pol.Select(x => Convert.ToDecimal(x.Attribute("ApplicableAmount").Value)).Max();
                    cp.Descendants("CancellationPolicy").Where(x => x.Attribute("LastCancellationDate").Value.Equals(pol.Key) && !x.Attribute("ApplicableAmount").Value.Equals(max.ToString())).Remove();
                }
                /* Region ends here*/
                List<XElement> mergeinput = new List<XElement>();
                cp.Descendants("CancellationPolicy").Where(x => x.Attribute("ApplicableAmount").Value.Equals("0")).Remove();
                mergeinput.Add(cp);
                XElement finalcp = MergCxlPolicy(mergeinput);
                #endregion
                #region Response XML
                if (finalcp.Descendants("CancellationPolicy").Any() && finalcp.Descendants("CancellationPolicy").Last().HasAttributes)
                {
                    var cxp = new XElement("Hotels",
                             new XElement("Hotel",
                                 new XElement("HotelID", travReq.Descendants("HotelID").FirstOrDefault().Value),
                                 new XElement("HotelName", travReq.Descendants("HotelName").FirstOrDefault().Value),
                                 new XElement("HotelImgSmall"),
                                 new XElement("HotelImgLarge"),
                                 new XElement("MapLink"),
                                 new XElement("DMC", _SupplierName),
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
            //string cityID = Req.Descendants("CityID").FirstOrDefault().Value;
            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            //string trackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
            //string HotelID = Req.Descendants("HotelID").FirstOrDefault().Value;
            List<XElement> prebooklines = new List<XElement>();
            try
            {
                string strAdult = "", strChildAge = "", strInfant = "";

                var paxnumber = GetPaxNumber(Req);
                int adult = paxnumber.Item1, child = paxnumber.Item2, infant = paxnumber.Item3;
                strChildAge = paxnumber.Item4;

                string hid = Req.Descendants("HotelID").FirstOrDefault().Value;
                string[] splithid = hid.Split(new char[] { '_' });
                #region Request XML
                string[] splitID = Req.Descendants("Room").FirstOrDefault().Attribute("ID").Value.Split(new char[] { '_' });
                string[] RoomID = Req.Descendants("Room").FirstOrDefault().Attribute("ID").Value.Split(new char[] { '_' });
                string MealID = Req.Descendants("Room").FirstOrDefault().Attribute("MealPlanID").Value;
                // XElement room = supplierResponse.Descendants("roomtypes").FirstOrDefault().Descendants("roomtype").FirstOrDefault().Descendants("rooms").FirstOrDefault().Descendants("room").Where(x => x.Element("id").Value.Equals(RatePlanCode[0])).FirstOrDefault();

                XDocument PreeBookRequest = new XDocument(
                                  new XDeclaration("1.0", "utf-8", "yes"),
                                  new XElement(soap12 + "Envelope",
                                      new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                      new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                      new XAttribute(XNamespace.Xmlns + "soap12", soap12),
                                      new XElement(soap12 + "Body",
                                         new XElement(sunhotel + "PreBookV2",
                                           new XAttribute(XNamespace.None + "xmlns", sunhotel),
                                                            new XElement(sunhotel + "userName", _LoginID),
                                                            new XElement(sunhotel + "password", _Password),
                                                            new XElement(sunhotel + "language", "en"),
                                                            new XElement(sunhotel + "currency", "USD"),
                                                            new XElement(sunhotel + "checkInDate", JuniperDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                                            new XElement(sunhotel + "checkOutDate", JuniperDate(Req.Descendants("ToDate").FirstOrDefault().Value)),
                                                            new XElement(sunhotel + "rooms", Req.Descendants("RoomPax").Count()),
                                                             new XElement(sunhotel + "adults", adult.ToString()),
                                                             new XElement(sunhotel + "children", child.ToString()),
                                                             new XElement(sunhotel + "childrenAges", strChildAge),
                                                             new XElement(sunhotel + "infant", infant.ToString()),

                                                             new XElement(sunhotel + "mealId", MealID),
                                                             new XElement(sunhotel + "roomId", RoomID[0]),
                                                              new XElement(sunhotel + "showPriceBreakdown", "1"),
                                                             new XElement(sunhotel + "customerCountry", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                                             new XElement(sunhotel + "b2c", "0")))));


                #endregion
                response = PreBookingResponse(Req, PreeBookRequest, strChildAge);

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookingRequest";
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion

            }

            #region Response Format
            string oldprice = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value;
            string newprice = response.Descendants("RoomTypes").Any() ? response.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value : oldprice;
            XElement prebookingfinal = null;
            #region Price Change Condition
            double oprice = Convert.ToDouble(oldprice), nPrice = Convert.ToDouble(newprice);
            bool NoChangePrice = true;
            if (oprice != nPrice)
                NoChangePrice = false;
            if (NoChangePrice)
            {
                //response.Descendants("Hotel").FirstOrDefault().AddBeforeSelf(new XElement("NewPrice"));
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
                                                       new XElement(Req.Descendants("HotelPreBookingRequest").FirstOrDefault()),
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
                                                    new XElement(Req.Descendants("HotelPreBookingRequest").FirstOrDefault()),
                                                   new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "Amount has been changed"), new XElement("NewPrice", nPrice), response.Element("Hotels")))));
                return prebookingfinal;
            }



            #endregion
            #endregion


        }
        #region Response
        public XElement PreBookingResponse(XElement travReq, XDocument prebookreq, string childrenAges)
        {
            XElement resp = null;
            XElement response = new XElement("HotelPreBookingResponse");
            DateTime starttime = DateTime.Now;
            // string hostUrl="http://xml2.bookingengine.es/webservice/jp/operations/checktransactions.asmx";
            // string hostUrl = ConfigurationManager.AppSettings["JNPRHotPreBookURL"].ToString();

            // serverResponse = XDocument.Load("D:\\Projects\\XML Integration\\W2M-Juniper\\Juniper_Response_PreeBook.xml");
            int infant = travReq.Descendants("ChildAge").Where(x => x.Value.Equals("1")).Count();
            foreach (XElement inf in travReq.Descendants("ChildAge").Where(x => x.Value.Equals("1")).Take(infant - 1))
                inf.SetValue("2");
            serverResponse = serverRequest.SunHotelResponse(prebookreq, "PreBookV2", _HotPreBookURL, Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value), Convert.ToString(travReq.Descendants("TransID").FirstOrDefault().Value), _SupplierID);
            XElement responseelement = removeAllNamespaces(serverResponse.Root);
            XElement requestelement = removeAllNamespaces(prebookreq.Root);
            #region Log Save
            try
            {

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = _SupplierID;
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
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion

            if (responseelement.Descendants("Error").Any())
            {
                response.Add(new XElement("ErrorTxt", " Error Type- " + responseelement.Descendants("Error").FirstOrDefault().Element("ErrorType").Value + " : " + responseelement.Descendants("Error").FirstOrDefault().Element("Message").Value));
                return response;
            }


            string bookingamount = responseelement.Descendants("preBookResult").FirstOrDefault().Element("Price").Value;


            #region Comments


            List<XElement> Notes = responseelement.Descendants("Note").ToList();
            string conditions = null;

            foreach (XElement comnt in Notes)
            {
                string fromdate = TravayooDate(comnt.Attribute("start_date").Value);
                string todate = TravayooDate(comnt.Attribute("end_date").Value);
                string strvalue = "Note Start Date: " + fromdate + " Note End Date: " + todate + " " + comnt.Element("text").Value;
                conditions += "<ul><li> Note Start Date: " + fromdate + ", Note End Date: " + todate + " - " + comnt.Element("text").Value + "</li></ul><br>";

                //conditions += "<ul><li>" + comnt.Element("text").Value + "</li></ul><br>";
            }
            #endregion

            if (responseelement.Descendants("preBookResult").Any())
            {
                XElement tryRoom = PreBookRooms(responseelement.Descendants("preBookResult").FirstOrDefault(), travReq, childrenAges);
                #region Response Xml
                var preBookResponse = new XElement("Hotels",
                                                               new XElement("Hotel",
                                                                   new XElement("HotelID"),
                                                                   new XElement("HotelName"),
                                                                   new XElement("Status", "true"),
                                                                   new XElement("NewPrice"),
                                                                   new XElement("TermCondition", conditions),
                                                                   new XElement("HotelImgSmall"),
                                                                   new XElement("HotelImgLarge"),
                                                                   new XElement("MapLink"),
                                                                   new XElement("DMC", _SupplierName),
                                                                   new XElement("Currency", travReq.Descendants("CurrencyName").FirstOrDefault().Value),
                                                                    new XElement("Offers"),
                                                                          tryRoom));
                //XElement cp = cancellationPolicy(travReq).Descendants("CancellationPolicies").FirstOrDefault();
                XElement cp = PreBookPolicy(responseelement.Descendants("preBookResult").Any() ? responseelement.Descendants("preBookResult").FirstOrDefault() : null, travReq);
                preBookResponse.Descendants("Room").Last().AddAfterSelf(cp);
                #endregion
                response.Add(preBookResponse);
            }
            else
            {

                //if (responseelement.Descendants("Warning").Any() ? responseelement.Descendants("Warning").FirstOrDefault().Value.Equals("warnPriceChanged") : false)
                //    response.Add(new XElement("ErrorTxt", "Amount has been changed"));
                //else if (responseelement.Descendants("Warning").Any() ? responseelement.Descendants("Warning").FirstOrDefault().Value.Equals("warnStatusChanged") : false)
                //    response.Add(new XElement("ErrorTxt", "Status changed"));
                //else if (responseelement.Descendants("Warning").Any())
                //    response.Add(new XElement("ErrorTxt", "Supplier Warning: " + responseelement.Descendants("Warning").FirstOrDefault().Value));
                //else
                response.Add(new XElement("ErrorTxt", "Availability Status Changed"));
            }

            return response;
        }


        #region Pre-Booking Rooms
        public XElement PreBookRooms(XElement JunPRooms, XElement travReq, string childAges)
        {
            int count = 1;
            int index = 1;
            int cnt = 1;
            int totalroomcount = 0;
            foreach (XElement roompax in travReq.Descendants("RoomPax"))
            {
                roompax.Add(new XElement("id", count++));
                totalroomcount = totalroomcount + 1;
            }
            string child = null;
            #region Nights
            DateTime from = DateTime.ParseExact(travReq.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime toDate = DateTime.ParseExact(travReq.Descendants("ToDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            int nights = toDate.Subtract(from).Days;
            #endregion
            List<XElement> roomtypeList = new List<XElement>();
            XElement PriceList = JunPRooms.Descendants("PriceBreakdown").FirstOrDefault();

            //foreach (XElement room in JunPRooms.Descendants("HotelOption"))
            {
                List<XElement> roomlist = new List<XElement>();
                foreach (XElement roompax in travReq.Descendants("RoomPax"))
                {
                    int RoomSqlCounter = 1;

                    // string[] strSource ={"0"};// room.Descendants("HotelRoom").FirstOrDefault().Attribute("Source").Value.Split(new char[] { ',' });

                    // foreach (XElement hotelRoom in room.Descendants("HotelRoom").Where(x => x.Attribute("Source").Value.Equals(roompax.Element("id").Value)))
                    //foreach (XElement hotelRoom in room.Descendants("HotelRoom"))
                    {
                        //string rmlist = null; 
                        string[] strSrc = { "0" };// hotelRoom.Attribute("Source").Value.Split(new char[] { ',' });

                        //for (int i = 0; i < strSrc.Length; i++)
                        {
                            //if (roompax.Element("id").Value.Equals(strSrc[i]))
                            {

                                XElement pricedetail = PreBookPriceBackup(totalroomcount, nights, Convert.ToDateTime(JuniperDate((travReq.Descendants("FromDate").FirstOrDefault().Value))), roompax, ref PriceList);

                                double totalroomrate = 0;

                                foreach (XElement p1 in pricedetail.Descendants("Price"))
                                {
                                    totalroomrate = totalroomrate + Convert.ToDouble(p1.Attribute("PriceValue").Value);
                                }



                                roomlist.Add(new XElement("Room",
                                                               new XAttribute("ID", travReq.Descendants("RoomTypeID").FirstOrDefault().Value),
                                                                  new XAttribute("SuppliersID", _SupplierID.ToString()),
                                                                  new XAttribute("RoomSeq", RoomSqlCounter++),
                                                                  new XAttribute("SessionID", childAges),
                                                                  new XAttribute("RoomType", travReq.Descendants("RoomTypes").FirstOrDefault().Descendants("Room").FirstOrDefault().Attribute("RoomType").Value),
                                                                  new XAttribute("OccupancyID", strSrc[0]),
                                                                  new XAttribute("OccupancyName", ""),
                                                                  MealPlanDetails(travReq.Descendants("MealPlanID").FirstOrDefault().Value, travReq.Descendants("MealPlanID").FirstOrDefault().Value),
                                                                  new XAttribute("MealPlanPrice", ""),
                                                                  new XAttribute("PerNightRoomRate", Math.Round(totalroomrate / nights, 2)),
                                                                  new XAttribute("TotalRoomRate", Math.Round(totalroomrate, 2)),
                                                                  new XAttribute("CancellationDate", ""),
                                                                  new XAttribute("CancellationAmount", ""),
                                                                  new XAttribute("isAvailable", "true"),
                                                                  new XElement("RequestID", JunPRooms.Descendants("PreBookCode").FirstOrDefault().Value),
                                                                  new XElement("Offers"),
                                                                  new XElement("PromotionList", null),
                                    // RoomPromotion(travReq.Descendants("Concepts").FirstOrDefault(), travReq.Descendants("HotelOffers").Any() ? travReq.Descendants("HotelOffers").FirstOrDefault() : null, strSrc[0])),
                                                                  new XElement("CancellationPolicy"),
                                                                  new XElement("Amenities",
                                                                      new XElement("Amenity")),
                                                                  new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                  new XElement("Supplements", null),
                                    //RoomSupplement(room.Descendants("Concepts").FirstOrDefault(), room.Descendants("HotelOffers").Any() ? room.Descendants("HotelOffers").FirstOrDefault() : null, strSrc[i])),
                                                                   pricedetail,
                                                                  new XElement("AdultNum", roompax.Element("Adult").Value),
                                                                  new XElement("ChildNum", roompax.Element("Child").Value)));
                            }

                        }


                    }
                }
                try
                {
                    string i = JunPRooms.Descendants("Price").FirstOrDefault().Value;
                }
                catch (Exception ex)
                {

                }

                roomtypeList.Add(new XElement("RoomTypes",
                                    new XAttribute("Index", cnt++),
                                    new XAttribute("TotalRate", JunPRooms.Descendants("Price").FirstOrDefault().Value), roomlist));
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
                #region Request XML
                string strAdult = "", strChildAge = "", strInfant = "";

                var paxnumber = GetPaxNumber(Req);
                int adult = paxnumber.Item1, child = paxnumber.Item2, infant = paxnumber.Item3;
                strChildAge = paxnumber.Item4;

                string hid = Req.Descendants("HotelID").FirstOrDefault().Value;
                string[] splitid = hid.Split(new char[] { '_' });
                //XElement PrebookResp = LogXMLs(Req.Descendants("TransactionID").FirstOrDefault().Value, 4, _SupplierID).FirstOrDefault().Descendants("Response").FirstOrDefault().Descendants("preBookResult").FirstOrDefault();            
                string pbCode = Req.Descendants("Room").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value;
                string MealID = Req.Descendants("Room").FirstOrDefault().Attribute("MealPlanID").Value;
                string[] RoomID = Req.Descendants("Room").FirstOrDefault().Attribute("RoomTypeID").Value.Split(new char[] { '_' });
                XElement Paxes = bookingpax(Req.Descendants("PassengersDetail").FirstOrDefault());

                List<XElement> GuestDetails = Paxes.Elements().ToList();

                XDocument Bookingconfirmation = new XDocument(
                                  new XDeclaration("1.0", "utf-8", "yes"),
                                  new XElement(soap12 + "Envelope",
                                      new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                      new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                      new XAttribute(XNamespace.Xmlns + "soap12", soap12),
                                      new XElement(soap12 + "Body",
                                         new XElement(sunhotel + "BookV2",
                                           new XAttribute(XNamespace.None + "xmlns", sunhotel),
                                                            new XElement(sunhotel + "userName", _LoginID),
                                                            new XElement(sunhotel + "password", _Password),
                                                            new XElement(sunhotel + "currency", "USD"),
                                                            new XElement(sunhotel + "language", "en"),
                                                            new XElement(sunhotel + "email", _AgentEmailID),
                                                            new XElement(sunhotel + "checkInDate", JuniperDate(Req.Descendants("FromDate").FirstOrDefault().Value)),
                                                            new XElement(sunhotel + "checkOutDate", JuniperDate(Req.Descendants("ToDate").FirstOrDefault().Value)),
                                                            new XElement(sunhotel + "roomId", RoomID[0]),
                                                            new XElement(sunhotel + "rooms", Req.Descendants("RoomPax").Count()),
                                                            new XElement(sunhotel + "adults", adult.ToString()),
                                                            new XElement(sunhotel + "children", child.ToString()),
                                                            new XElement(sunhotel + "infant", infant.ToString()),
                                                            new XElement(sunhotel + "yourRef", Req.Descendants("TransID").FirstOrDefault().Value),
                                                            new XElement(sunhotel + "specialrequest", Req.Descendants("SpecialRemarks").FirstOrDefault().Value),
                                                            new XElement(sunhotel + "mealId", MealID),
                                                            GuestDetails,
                                                            new XElement(sunhotel + "paymentMethodId", "1"),
                                                            new XElement(sunhotel + "customerEmail", ""),
                                                            new XElement(sunhotel + "invoiceRef", ""),
                                                            new XElement(sunhotel + "customerCountry", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                                            new XElement(sunhotel + "b2c", "0"),
                                                            new XElement(sunhotel + "preBookCode", pbCode)
                                                            ))));

                #endregion
                response = bookingResponse(Bookingconfirmation, Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingRequest";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
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
                                                        new XElement(Req.Descendants("HotelBookingRequest").FirstOrDefault()),
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
                                                        new XElement(Req.Descendants("HotelBookingRequest").FirstOrDefault()),
                                                       response)));
            #region Price Change Condition
            if (condition)
                confirmedBooking.Descendants("Hotels").FirstOrDefault().AddBeforeSelf(
                     new XElement("ErrorTxt", "Amount has been changed"),
                   new XElement("NewPrice", Convert.ToString(newrate)));
            #endregion
            #endregion
            return confirmedBooking;
        }
        #region Response
        public XElement bookingResponse(XDocument RestelRequest, XElement travReq)
        {
            XElement response = null;
            DateTime starttime = DateTime.Now;
            serverResponse = serverRequest.SunHotelResponse(RestelRequest, "BookV2", _HotBookURL, Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value), Convert.ToString(travReq.Descendants("TransID").FirstOrDefault().Value), _SupplierID);
            XElement servrespnns = removeAllNamespaces(serverResponse.Root);
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement restreqnns = removeAllNamespaces(RestelRequest.Root);

                #endregion
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                log.SupplierID = _SupplierID;
                log.logrequestXML = restreqnns.ToString();
                log.logresponseXML = servrespnns.ToString();
                log.LogType = "Book";
                log.LogTypeID = 5;
                log.StartTime = starttime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingResponse";
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion

            try
            {
                int adult = 0;
                int child = 0;
                foreach (XElement ad in travReq.Descendants("RoomPax"))
                {
                    adult = adult + Convert.ToInt32(ad.Element("Adult").Value);
                    child = child + Convert.ToInt32(ad.Element("Child").Value);
                }

                if (servrespnns.Descendants("Error").Any())
                {
                    response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Error Type- " + servrespnns.Descendants("ErrorType").FirstOrDefault().Value + " : " + servrespnns.Descendants("Message").FirstOrDefault().Value));
                }
                else
                {
                    #region Response XML
                    var hbr =
                                      new XElement("Hotels",
                                                    new XElement("HotelID", travReq.Descendants("HotelID").FirstOrDefault().Value),
                                                    new XElement("HotelName", travReq.Descendants("HotelName").FirstOrDefault().Value),
                                                    new XElement("FromDate", travReq.Descendants("FromDate").FirstOrDefault().Value),
                                                    new XElement("ToDate", travReq.Descendants("ToDate").FirstOrDefault().Value),
                                                    new XElement("AdultPax", Convert.ToString(adult)),
                                                    new XElement("ChildPax", Convert.ToString(child)),
                                                    new XElement("TotalPrice", travReq.Descendants("TotalAmount").FirstOrDefault().Value),
                                                    new XElement("CurrencyID", travReq.Descendants("CurrencyID").FirstOrDefault().Value),
                                                    new XElement("CurrencyCode", travReq.Descendants("CurrencyCode").FirstOrDefault().Value),
                                                    new XElement("MarketID"),
                                                    new XElement("MarketName"),
                                                    new XElement("HotelImgSmall"),
                                                    new XElement("HotelImgLarge"),
                                                    new XElement("MapLink"),
                                                    new XElement("VoucherRemark"),
                                                    new XElement("TransID", travReq.Descendants("TransID").FirstOrDefault().Value),
                                                    new XElement("ConfirmationNumber", servrespnns.Descendants("bookingnumber").FirstOrDefault().Value),
                                                    new XElement("Status", "Success"),
                                                    Booking_Rooms(travReq));
                    #endregion
                    response = new XElement("HotelBookingResponse", hbr);

                }
            }
            catch (Exception ex)
            {
                response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", ex.Message));
            }

            return response;
        }

        #endregion
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


            try
            {
                XDocument CheckCXAmt = new XDocument(
                                  new XDeclaration("1.0", "utf-8", "yes"),
                                  new XElement(soap12 + "Envelope",
                                      new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                                      new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                                      new XAttribute(XNamespace.Xmlns + "soap12", soap12),
                                      new XElement(soap12 + "Body",
                                         new XElement(sunhotel + "CancelBooking",
                                           new XAttribute(XNamespace.None + "xmlns", sunhotel),
                                                            new XElement(sunhotel + "userName", _LoginID),
                                                            new XElement(sunhotel + "password", _Password),
                                                            new XElement(sunhotel + "bookingID", Req.Descendants("ConfirmationNumber").FirstOrDefault().Value),
                                                            new XElement(sunhotel + "language", "en")
                                                            ))));


                response = CancellationResponse(CheckCXAmt, Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "BookingCancel";
                ex1.PageName = "SunHotelsResponses";
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
                                                       new XElement(removeAllNamespaces(Req.Descendants("HotelCancellationRequest").FirstOrDefault())),
                                                      response)));
            return cancelledBooking;
            #endregion
        }
        #region Cancellation  Response
        public XElement CancellationResponse(XDocument BookingCancel, XElement Req)
        {
            DateTime starttime = DateTime.Now;
            serverResponse = serverRequest.SunHotelResponse(BookingCancel, "CancelBooking", _HotBookCanURL, Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value), Convert.ToString(Req.Descendants("TransID").FirstOrDefault().Value), _SupplierID);
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
                log.SupplierID = _SupplierID;
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
                ex1.PageName = "SunHotelsResponses";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            XElement response = new XElement("HotelCancellationResponse");
            string status = null;

            if (servrespnns.Descendants("Error").Any())
            {
                response.Add(new XElement("ErrorTxt", "Error Type- " + servrespnns.Descendants("ErrorType").FirstOrDefault().Value + " : " + servrespnns.Descendants("Message").FirstOrDefault().Value));
            }
            else
            {

                if (servrespnns.Descendants("Code").FirstOrDefault().Value.Equals("1"))
                {
                    status = "Success";
                    string cancelcost = "";
                    cancelcost = servrespnns.Descendants("cancellationfee").Where(x => x.Attribute("currency").Value.Equals("USD")).Any() ? servrespnns.Descendants("cancellationfee").Where(x => x.Attribute("currency").Value.Equals("USD")).FirstOrDefault().Value
                        : servrespnns.Descendants("cancellationfee").FirstOrDefault().Value;
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
                    if (servrespnns.Descendants("Code").FirstOrDefault().Value.Equals("-1"))
                    {
                        Error = "Cancel Code:" + servrespnns.Descendants("Code").FirstOrDefault().Value + "- Unsuccessful.";
                    }
                    else if (servrespnns.Descendants("Code").FirstOrDefault().Value.Equals("2"))
                    {
                        Error = "Cancel Code:" + servrespnns.Descendants("Code").FirstOrDefault().Value + "-Booking can’t be cancelled/booking don’t exists.";
                    }
                    response.Add(new XElement("ErrorTxt", Error));
                    #endregion
                }
            }
            return response;
        }
        #endregion
        #endregion


        #region Common Functions
        #region Rooms for Response

        public XElement groupedRooms(XElement JunpRooms, XElement travReq, DataTable staticdata)
        {
            int count = 1;
            int index = 1;
            int cnt = 1;
            int totalroomcount = 0;
            foreach (XElement roompax in travReq.Descendants("RoomPax"))
            {
                roompax.Add(new XElement("id", count++));
                totalroomcount = totalroomcount + 1;
            }
            string child = null;

            XElement response = new XElement("Rooms");
            List<XElement> roomtypeList = new List<XElement>();

            foreach (XElement room in JunpRooms.Descendants("roomtype"))
            {
                string strroomtype = room.Element("roomtype.ID").Value + "_RoomType";
                //List<XElement> roomlist = new List<XElement>();
                //foreach (XElement roompax in travReq.Descendants("RoomPax"))
                foreach (XElement hotelRoom in room.Descendants("room"))
                {
                    //List<XElement> roomlist = new List<XElement>();
                    //int RoomSqlCounter = 1;
                    double totalroomTyperate = 0;

                    foreach (XElement mealroom in hotelRoom.Descendants("meal"))
                    {
                        int remainingrooms = totalroomcount;
                        double totaladdedcost = 0;
                        roomtypeList = new List<XElement>();
                        List<XElement> roomlist = new List<XElement>();
                        int RoomSqlCounter = 1;
                        totalroomTyperate = Math.Round(Convert.ToDouble(mealroom.Descendants("prices").FirstOrDefault().Element("price").Value), 2);

                        foreach (XElement roompax in travReq.Descendants("RoomPax"))
                        {

                            XElement pricedetail = AveragePriceBackup(mealroom.Descendants("prices").FirstOrDefault(), totalroomcount, Convert.ToInt16(travReq.Descendants("Nights").FirstOrDefault().Value), Convert.ToDateTime(JuniperDate((travReq.Descendants("FromDate").FirstOrDefault().Value))));
                            double totalroomrate = 0;
                            foreach (XElement p1 in pricedetail.Descendants("Price"))
                            {
                                totalroomrate = Math.Round(totalroomrate + Convert.ToDouble(p1.Attribute("PriceValue").Value), 2);
                            }

                            /*****************************************
                             **** To Check the Exact Price Match *****
                             *****************************************/

                            totaladdedcost = totaladdedcost + totalroomrate;
                            if (remainingrooms == 1)
                            {
                                double roomratevariance = totalroomTyperate - totaladdedcost;
                                double pernightrate = Math.Round(roomratevariance / Convert.ToInt16(travReq.Descendants("Nights").FirstOrDefault().Value), 2);
                                totalroomrate = Math.Round(totalroomrate + roomratevariance, 2);
                                double projectedroomrate = Math.Round(Convert.ToDouble(pricedetail.Descendants("Price").First().Attribute("PriceValue").Value) + pernightrate, 2);
                                projectedroomrate = Convert.ToInt16(travReq.Descendants("Nights").FirstOrDefault().Value) * projectedroomrate;
                                double nightvariance = Math.Round(totalroomrate - projectedroomrate, 2);
                                int remainingnight = Convert.ToInt16(travReq.Descendants("Nights").FirstOrDefault().Value);
                                foreach (XElement p1 in pricedetail.Descendants("Price"))
                                {
                                    double newrate = Math.Round(Convert.ToDouble(p1.Attribute("PriceValue").Value) + pernightrate, 2);

                                    if (remainingnight == 1)
                                    {
                                        newrate = newrate + nightvariance;
                                    }
                                    p1.Attribute("PriceValue").Value = Convert.ToString(newrate);

                                    remainingnight = remainingnight - 1;
                                }
                            }
                            remainingrooms = remainingrooms - 1;

                            /*****************************************/

                            string str = room.Element("roomtype.ID").Value;
                            //var result = staticdata.AsEnumerable().Where(dt => dt.Field<string>("ID") == str);
                            //DataRow[] drow = result.ToArray();
                            string roomName = string.Empty;
                            if (staticdata.AsEnumerable().Where(x => x.Field<string>("ID") == str).Any())
                                roomName = staticdata.AsEnumerable().Where(x => x.Field<string>("ID") == str).FirstOrDefault().Field<string>("RoomtypeName");
                            else
                                roomName = string.Empty;
                            //str = string.Empty;
                            /************************************************
                             ** Check Room Detail avialable in Static Data **
                             ************************************************/

                            if (string.IsNullOrEmpty(roomName))
                            {
                                //if (result.ToArray().Length >= 1)
                                //{
                                //    str = "Yes";
                                //    strroomtype = drow[0].ItemArray[3].ToString();
                                //}
                                //else
                                //{
                                str = "No";
                                strroomtype = "RoomType" + "_" + hotelRoom.Element("id").Value;
                                //} 
                            }
                            else
                            {
                                str = "Yes";
                                strroomtype = roomName;
                            }

                            /********************************************/

                            roomlist.Add(new XElement("Room",
                                                                new XAttribute("ID", hotelRoom.Element("id").Value + "_" + mealroom.Element("id").Value),
                                                                new XAttribute("SuppliersID", _SupplierID.ToString()),
                                                                new XAttribute("RoomSeq", RoomSqlCounter++),
                                                                new XAttribute("SessionID", ""),
                                                                new XAttribute("RoomType", strroomtype),
                                                                new XAttribute("OccupancyID", 0),
                                                                new XAttribute("OccupancyName", ""),
                                                                MealPlanDetails(mealroom.Element("id").Value, ""),
                                                                new XAttribute("MealPlanPrice", ""),
                                                                new XAttribute("PerNightRoomRate", Math.Round(totalroomrate / Convert.ToInt16(travReq.Descendants("Nights").FirstOrDefault().Value), 2)),
                                                                new XAttribute("TotalRoomRate", Math.Round(totalroomrate, 2)),
                                                                new XAttribute("CancellationDate", ""),
                                                                new XAttribute("CancellationAmount", ""),
                                                                new XAttribute("isAvailable", "true"),
                                                                new XElement("RequestID", ""),
                                                                new XElement("Offers"),
                                                                new XElement("PromotionList",
                                                                RoomDiscount(mealroom.Descendants("discount").FirstOrDefault().HasElements ? mealroom.Descendants("discount").FirstOrDefault() : null, totalroomcount)),
                                                                new XElement("CancellationPolicy"),
                                                                new XElement("Amenities",
                                                                    new XElement("Amenity")),
                                                                new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                new XElement("Supplements", null),
                                                                pricedetail,
                                                                new XElement("AdultNum", roompax.Element("Adult").Value),
                                                                new XElement("ChildNum", roompax.Element("Child").Value)));

                        }
                        roomtypeList.Add(new XElement("RoomTypes",
                                  new XAttribute("Index", cnt++),
                                  new XAttribute("TotalRate", totalroomTyperate),
                                    new XAttribute("HtlCode", hotelcode),
                                    new XAttribute("CrncyCode", SupplierCurrency),
                                    new XAttribute("DMCType", dmc),
                                    new XAttribute("CUID", customerid),
                                    roomlist));
                        response.Add(roomtypeList);
                    }
                }

            }
            //XElement response = new XElement("Rooms");
            // response.Add(roomtypeList);
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
                int daysbefore = Convert.ToInt32(policy.Descendants("dias_antelacion").FirstOrDefault().Value);
                DateTime date = DateTime.ParseExact(policy.Attribute("fecha").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                date = date.AddDays(-daysbefore);
                string dt = date.ToString("dd/MM/yyyy");
                double nightsCharged = 0;
                if (!policy.Descendants("noches_gasto").FirstOrDefault().Value.Equals("0"))
                    nightsCharged = Convert.ToDouble(policy.Descendants("noches_gasto").FirstOrDefault().Value);
                else
                {
                    double per = Convert.ToDouble(policy.Descendants("estCom_gasto").FirstOrDefault().Value);
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
               GroupBy(r => new { r.Attribute("LastCancellationDate").Value, noshow = r.Attribute("NoShowPolicy").Value }).Select(y => y.FirstOrDefault()).
               OrderBy(p => p.Attribute("LastCancellationDate").Value);
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
                                datePrice += Convert.ToDecimal(rm.Attribute("ApplicableAmount").Value);
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
                        new XAttribute("LastCancellationDate", y.Key.Date.ToString("yyyy/MM/dd")),
                        new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                        new XAttribute("NoShowPolicy", y.Key.Value))).OrderBy(p => p.Attribute("LastCancellationDate").Value).ToList();

                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", Convert.ToDateTime(fItem.Attribute("LastCancellationDate").Value).AddDays(-1).Date.ToString("yyyy/MM/dd")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));

                }
            }

            XElement cxlItem = new XElement("CancellationPolicies", cxlList);
            return cxlItem;

        }
        public DateTime chnagetoTime(string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
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
                case "1":
                    mpName = "Room Only";
                    mpID = "1";
                    mpCode = "RO";
                    break;
                case "3":
                    mpName = "Bed & Breakfast";
                    mpID = "3";
                    mpCode = "BB";
                    break;
                case "4":
                    mpName = "Half Board";
                    mpID = "4";
                    mpCode = "HB";
                    break;
                case "5":
                    mpName = "Full Board";
                    mpID = "5";
                    mpCode = "FB";
                    break;
                case "6":
                    mpName = "All Inclusive";
                    mpID = "6";
                    mpCode = "AI";
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
            foreach (XElement room in rooms.Descendants("roomtype"))
            {
                double check = Convert.ToDouble(room.Descendants("prices").FirstOrDefault().Element("price").Value);
                if (check < minprice)
                    minprice = check;
            }
            return minprice.ToString();

        }

        #endregion

        #region Star Rating Condition
        public bool StarRating(string minRating, string MaxRating, string HotelStarRating)
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
        public List<string> HotelCodes(string CityCode)
        {

            DataTable hotelsList = jdata.GetHotelsList(CityCode);
            List<string> NewResponse = new List<string>();
            for (int i = 0; i < hotelsList.Rows.Count; i++)
            {
                NewResponse.Add(hotelsList.Rows[i]["ResortID"].ToString());
            }
            return NewResponse;
        }
        #endregion

        #region Facilities
        public XElement HotelFacilities(string CityID)
        {
            XElement response = new XElement("FacilitiesCityWise");
            List<XElement> Hotels = new List<XElement>();

            DataTable Facilities = jdata.GetW2MHotelFacility(CityID);
            if (Facilities != null && Facilities.Rows.Count != 0)
            {
                var facil = Facilities.Columns.Cast<DataColumn>()
                                                   .Select(x => x.ColumnName);
                string[] facilityArray = facil.ToArray();
                for (int j = 0; j < Facilities.Rows.Count; j++)
                {
                    DataRow fac = Facilities.Rows[j];
                    if (!String.IsNullOrEmpty(fac["Facilities"].ToString()))
                    {
                        XElement Hotel = new XElement("Hotel",
                                        new XAttribute("HotelID", fac["HotelID"]),
                                        new XAttribute("CityID", fac["CityID"]));

                        XElement Temp = XElement.Parse(fac["Facilities"].ToString());
                        List<XElement> TempFacilities = new List<XElement>();
                        List<XElement> TempData = Temp.Descendants("Feature").ToList();
                        foreach (XElement f in TempData)
                        {
                            TempFacilities.Add(new XElement("Facility", f.Value));
                        }

                        Hotel.Add(new XElement("Facilities", TempFacilities));
                        Hotels.Add(Hotel);
                    }
                }
            }
            //else
            //{
            //    response = new XElement("Facilities", new XElement("Facility", "No Facility Available"));
            //}
            response.Add(Hotels);
            return response;
        }
        #endregion



        #endregion
        #region PreBook Cancellation Policy
        public XElement PreBookPolicy(XElement HotelOption, XElement travReq)
        {
            double perNightRate = 0.0;
            double totalRate = Convert.ToDouble(travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value);
            double firstnight = 0.0;
            string ind = travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("Index").Value;
            //foreach (XElement room in pbList)
            //{

            //    firstnight += Convert.ToDouble(room.Descendants("Price").Where(x => x.Attribute("Night").Value.Equals("1")).FirstOrDefault().Attribute("PriceValue").Value);
            //    //perNightRate = perNightRate + Convert.ToDouble(room.Attribute("PerNightRoomRate").Value);
            //}

            XElement cp = new XElement("CancellationPolicies");

            #region Cancellation Policy Tag
            foreach (XElement policy in HotelOption.Descendants("CancellationPolicy"))
            {

                DateTime dateFrom = DateTime.ParseExact(travReq.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                double rate = 0.0;

                if (!policy.Descendants("deadline").FirstOrDefault().HasAttributes)
                {
                    int hoursClx = Convert.ToInt16(policy.Element("deadline").Value);
                    double percent = Convert.ToDouble(policy.Element("percentage").Value);
                    DateTime clxdate = dateFrom.AddHours(-hoursClx);
                    rate = Math.Round(totalRate * percent / 100, 2);

                    cp.Add(new XElement("CancellationPolicy",
                                                      new XAttribute("LastCancellationDate", clxdate.ToString("dd/MM/yyyy")),
                                                      new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                                      new XAttribute("NoShowPolicy", "0")));
                }
                else
                {
                    cp.Add(new XElement("CancellationPolicy",
                                                      new XAttribute("LastCancellationDate", DateTime.Now.ToString("dd/MM/yyyy")),
                                                      new XAttribute("ApplicableAmount", Convert.ToString(totalRate)),
                                                      new XAttribute("NoShowPolicy", "0")));

                }
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
        public XElement AveragePriceBackup(XElement roomratelist, int totalRoom, int totalNight, DateTime startdate)
        {
            XElement response = new XElement("PriceBreakups");
            double totaleprice = Convert.ToDouble(roomratelist.Element("price").Value);
            double roomrate = (totaleprice / totalNight) / totalRoom;
            double adjustment = 0;

            for (int j = 1; j <= totalNight; j++)
            {
                response.Add(new XElement("Price",
                                 new XAttribute("Night", j),
                                 new XAttribute("PriceValue", Convert.ToString(roomrate))));

            }
            return response;
        }


        public XElement PreBookPriceBackup(int totalRoom, int totalNight, DateTime startdate, XElement paxlist, ref XElement PriceList)
        {
            XElement response = new XElement("PriceBreakups");
            int adult = Convert.ToInt16(paxlist.Element("Adult").Value);
            int Children = Convert.ToInt16(paxlist.Element("Child").Value);
            double[] adultprice = new double[totalNight], childprice = new double[totalNight];
            double[] adultDisc = new double[totalNight], childDisc = new double[totalNight];
            int adultcount = 1;

            #region  Adult Price Breakup
            foreach (XElement guest in PriceList.Descendants("guest").Where(x => !x.Attributes("age").Any() && !x.Attributes("Read").Any()).ToList())
            {
                if (adultcount <= adult)
                {
                    List<XElement> PriceDetail = guest.Descendants("price").ToList();

                    foreach (XElement item in PriceDetail)
                    {
                        try
                        {
                            if (item.Attribute("type").Value.Equals("base"))
                            {
                                string[] nightprice = item.Attribute("breakdown").Value.Split(new char[] { '|' });
                                for (int j = 0; j < nightprice.Length; j++)
                                {
                                    adultprice[j] = adultprice[j] + Convert.ToDouble(nightprice[j]);
                                }
                            }
                            else
                            {
                                double disc = Convert.ToDouble(item.Attribute("value").Value) / totalNight;
                                for (int j = 0; j < totalNight; j++)
                                {
                                    adultDisc[j] = adultDisc[j] + disc;
                                }

                            }

                        }
                        catch (Exception ex)
                        { }

                    }
                    //string[] nightprice = guest.Descendants("price").FirstOrDefault().Attribute("breakdown").Value.Split(new char[] { '|' });
                    //for (int j = 0; j < nightprice.Length; j++)
                    //{
                    //    adultprice[j] = adultprice[j] + Convert.ToDouble(nightprice[j]);
                    //}
                    guest.Add(new XAttribute("Read", "T"));
                    adultcount += 1;
                }
                else { break; }
            }
            #endregion

            #region child Price Breakup
            foreach (XElement childage in paxlist.Descendants("ChildAge"))
            {
                if (Convert.ToInt16(childage.Value) > 1)
                {
                    XElement priceelement = PriceList.Descendants("guest").Where(x => x.Attributes("age").Any() && !x.Attributes("Read").Any() && x.Attribute("age").Value.Equals(childage.Value)).FirstOrDefault();
                    List<XElement> PriceDetail = priceelement.Descendants("price").ToList();

                    foreach (XElement item in PriceDetail)
                    {
                        if (item.Attribute("type").Value.Equals("base"))
                        {
                            string[] nightprice = item.Attribute("breakdown").Value.Split(new char[] { '|' });
                            for (int j = 0; j < nightprice.Length; j++)
                            {
                                childprice[j] = childprice[j] + Convert.ToDouble(nightprice[j]);
                            }
                        }
                        else
                        {
                            double disc = Convert.ToDouble(item.Attribute("value").Value) / totalNight;
                            for (int j = 0; j < totalNight; j++)
                            {
                                childDisc[j] = childDisc[j] + disc;
                            }

                        }
                    }

                    //string[] nightprice = priceelement.Descendants("price").FirstOrDefault().Attribute("breakdown").Value.Split(new char[] { '|' });
                    //for (int j = 0; j < nightprice.Length; j++)
                    //{
                    //    childprice[j] = childprice[j] + Convert.ToDouble(nightprice[j]);
                    //}
                    priceelement.Add(new XAttribute("Read", "T"));
                }
            }
            #endregion

            //double roomrate = (totaleprice / totalNight) / totalRoom;
            double roomrate = 0;

            for (int j = 1; j <= totalNight; j++)
            {
                roomrate = adultprice[j - 1] + childprice[j - 1] + adultDisc[j - 1] + childDisc[j - 1];
                response.Add(new XElement("Price",
                                 new XAttribute("Night", j),
                                 new XAttribute("PriceValue", Convert.ToString(roomrate))));
            }
            return response;
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
                    // string promocode = offer.Descendants("HotelOffer").FirstOrDefault().Attribute("Code").Value;
                    //List<XElement> supplements = SuplementList.Descendants("Concept").Where(x => x.Attribute("Type").Value.Equals("SUP")).ToList();
                    //foreach (XElement roomsoffer in supplements.Where(x => x.Attribute("RelationalCode").Value.Equals(offer.Attribute("Code").Value)).ToList())
                    {
                        //if( roomsoffer.Descendants("Item").Where( x=> x.Attribute("Source").Value.Equals(roomsource)).Count()>0)

                        promotion.Add(new XElement("Promotions", offer.Element("Name").Value));

                    }

                }
            }

            return promotion;
        }

        #region BindPromotions  - Discount

        public List<XElement> RoomDiscount(XElement offerslist, int roomcount)
        {
            List<XElement> promotion = new List<XElement>();
            if (offerslist != null)
            {
                string discountType = offerslist.Element("typeId").Value;

                foreach (XElement offer in offerslist.Descendants("amounts"))
                {
                    string disc = Convert.ToString(Math.Round(Convert.ToDouble(offer.Element("amount").Value) / roomcount, 2));
                    //promotion.Add(new XElement("Promotions", "Discount_" + discountType + "_" + disc) );
                    promotion.Add(new XElement("Promotions", "Discounted Price"));

                }

            }

            return promotion;
        }

        #endregion

        public List<XElement> RoomSupplement(XElement SuplementList, XElement offerslist, string roomsource)
        {
            List<XElement> Supplement = new List<XElement>();
            List<XElement> listsupplements = SuplementList.Descendants("Concept").Where(x => x.Attribute("Type").Value.Equals("SUP")).ToList();
            foreach (XElement roomsoffer in listsupplements)
            {
                //string str = roomsoffer.Attributes("RelationalCode").Any() ? roomsoffer.Attribute("RelationalCode").Value : null;

                Supplement.Add(new XElement("Supplement",
                          new XAttribute("suppId", roomsoffer.Attributes("RelationalCode").Any() ? roomsoffer.Attribute("RelationalCode").Value : "00"),
                          new XAttribute("suppName", roomsoffer.Attribute("Name").Value),
                          new XAttribute("supptType", roomsoffer.Attribute("Type").Value),
                          new XAttribute("suppIsMandatory", "FALSE"),
                          new XAttribute("suppChargeType", "Included"),
                          new XAttribute("suppPrice", "0"),
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

        private XElement bookingpax(XElement paxCount)
        {
            int adultnumber = 1, childnumber = 1;
            List<XElement> childTags = new List<XElement>();
            XElement Response = new XElement("GuestDetail");
            int infants = paxCount.Descendants("PaxInfo").Where(x => x.Element("Age").Value.Equals("1")).Count();
            foreach (XElement room in paxCount.Descendants("Room"))
            {
                foreach (XElement guest in room.Descendants("PaxInfo"))
                {
                    if (guest.Element("GuestType").Value.Equals("Adult"))
                    {
                        Response.Add(new XElement(sunhotel + "adultGuest" + adultnumber.ToString() + "FirstName", guest.Element("FirstName").Value));
                        Response.Add(new XElement(sunhotel + "adultGuest" + adultnumber.ToString() + "LastName", guest.Element("LastName").Value));
                        adultnumber += 1;
                    }
                    else
                    {
                        //if(Convert.ToInt16(guest.Element("Age").Value) >1)
                        //{

                        //    Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "FirstName", guest.Element("FirstName").Value ));
                        //    Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "LastName", guest.Element("LastName").Value ));
                        //    Response.Add(new XElement(sunhotel + "childrenGuestAge" + childnumber.ToString(),  guest.Element("Age").Value ));
                        //    childnumber += 1;                                
                        //}
                        //else if(infants>1 )
                        //{
                        //    Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "FirstName", guest.Element("FirstName").Value));
                        //    Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "LastName", guest.Element("LastName").Value));
                        //    Response.Add(new XElement(sunhotel + "childrenGuestAge" + childnumber.ToString(), "2"));
                        //    childnumber += 1;
                        //    infants--;
                        //}
                        if (Convert.ToInt16(guest.Element("Age").Value) > 1)
                        {
                            childTags.Add(new XElement("Child",
                            new XElement("childrenGuestFirstName", guest.Element("FirstName").Value),
                            new XElement("childrenGuestLastName", guest.Element("LastName").Value),
                            new XElement("childrenGuestAge", guest.Element("Age").Value)));
                        }
                        else if (infants > 1)
                        {
                            childTags.Add(new XElement("Child",
                            new XElement("childrenGuestFirstName", guest.Element("FirstName").Value),
                            new XElement("childrenGuestLastName", guest.Element("LastName").Value),
                            new XElement("childrenGuestAge", "2")));
                            infants--;
                        }
                    }
                }

            }
            if (childTags.Count > 0)
            {
                string[] childAges = paxCount.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value.Split(new char[] { ',' });
                for (int i = 0; i < childAges.Length; i++)
                {
                    XElement tag = childTags.Where(x => x.Elements().LastOrDefault().Value.Equals(childAges[i])).FirstOrDefault();
                    string fName = tag.Elements().FirstOrDefault().Value, lName = tag.Element("childrenGuestLastName").Value;
                    Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "FirstName", fName));
                    Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "LastName", fName));
                    Response.Add(new XElement(sunhotel + "childrenGuestAge" + childnumber.ToString(), childAges[i]));
                    childnumber++;
                    childTags.Remove(tag);
                }
            }
            //int infants = paxCount.Descendants("PaxInfo").Where(x => x.Element("Age").Value.Equals("1")).Count();
            //if(infants>1)
            //{
            //    for(int i=0;i<infants;i++)
            //    {
            //        Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "FirstName", guest.Element("FirstName").Value));
            //        Response.Add(new XElement(sunhotel + "childrenGuest" + childnumber.ToString() + "LastName", guest.Element("LastName").Value));
            //        Response.Add(new XElement(sunhotel + "childrenGuestAge" + childnumber.ToString(), "2"));
            //        childnumber += 1; 
            //    }
            //}
            return Response;
        }



        private void SupplierConfiguration(int suppID, int CustId)
        {
            // XElement SupplCredetials = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SupplierCredential/suppliercredentials.xml"));
            //XDocument SupplCredetials = XDocument.Load(Path.Combine(HttpRuntime.AppDomainAppPath, ConfigurationManager.AppSettings["SupplierCredetial"] + @"suppliercredentials.xml"));
            XElement SuplCred = supplier_Cred.getsupplier_credentials(Convert.ToString(CustId), Convert.ToString(suppID));
            // XElement SuplCred = SupplCredetials.Descendants("credential").Where(x => x.Attribute("customerid").Value.Equals(CustId.ToString()) && x.Attribute("supplierid").Value.Equals(suppID.ToString())).FirstOrDefault();
            _HostUrl = SuplCred.Element("HostUrl").Value;
            _LoginID = SuplCred.Element("Login").Value;
            _Password = SuplCred.Element("Password").Value;
            _AgentEmailID = SuplCred.Element("AgentEmail").Value;
            _SupplierName = "SunHotels";

        }

        #endregion
    }
}