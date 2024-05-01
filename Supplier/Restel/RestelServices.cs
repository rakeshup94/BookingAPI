using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
using TravillioXMLOutService.Models.Restel;
using System.IO;
using System.Configuration;
using TravillioXMLOutService.Models;
using System.Data;
using System.Threading;
using System.Text;

namespace TravillioXMLOutService.Supplier.Restel
{
    public class RestelServices : IDisposable
    {
        int sup_cutime = 100000;
        string dmc = string.Empty;
        string customerid = string.Empty;
        int supplierid, threadCount;
        string hotelid = string.Empty;
        RestelCredentials rc = new RestelCredentials();
        string XmlPath = ConfigurationManager.AppSettings["RestelPath"];
        RestelServerRequest serverRequest = new RestelServerRequest();
        RestelLogAccess rlac = new RestelLogAccess();
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        XDocument serverResponse;
        #region Hotel Search
        public List<XElement> ThreadedHotelSearch(XElement Req, string xtype, int supid,string custID,string custName)
        {
            try
            {
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                    threadCount = Convert.ToInt32(ConfigurationManager.AppSettings["RestelThreads"].ToString());
                }
                catch { }
                #endregion
                #region Add Occupancy ID
                int index = 1;
                foreach (XElement pax in Req.Descendants("RoomPax"))
                {
                    pax.Add(new XElement("paxes", pax.Element("Adult").Value + "-" + pax.Element("Child").Value));
                    pax.Add(new XElement("id", index++));
                }
                #endregion
                dmc = xtype;
                supplierid = supid;
                customerid = custID;
                XElement response = null;
                //string cityID = Req.Descendants("CityID").First().Value;
                //DataTable CityMapping = rlac.RestelCityMapping(cityID, "13");
                //if (CityMapping.Rows.Count == 0)
                //{
                //    #region Exception
                //    CustomException ex1 = new CustomException("City Not Found");
                //    ex1.MethodName = "ThreadedHotelSearch";
                //    ex1.PageName = "RestelServices";
                //    ex1.CustomerID = Req.Descendants("CustomerID").First().Value;
                //    ex1.TranID = Req.Descendants("TransID").First().Value;
                //    SaveAPILog saveex = new SaveAPILog();
                //    saveex.SendCustomExcepToDB(ex1);
                //    #endregion
                //    return null;
                //}
                List<string> hotelIDs = new List<string>();
                XElement Facilities = null;


                string HotelId = null;
                if (Req.Descendants("HotelID").First().Value != null && Req.Descendants("HotelID").First().Value != String.Empty)
                    HotelId = Req.Descendants("HotelID").First().Value;
                string HotelName = null;
                if (Req.Descendants("HotelName").First().Value != null && Req.Descendants("HotelName").First().Value != String.Empty)
                    HotelName = Req.Descendants("HotelName").First().Value;

                hotelIDs.AddRange(HotelCodes(Req.Descendants("CityID").First().Value, HotelId, HotelName));






                //hotelIDs.AddRange(HotelCodes(Req.Descendants("CityID").First().Value));
                //for (int i = 0; i < CityMapping.Rows.Count; i++)
                //{
                //    string RestelCity = CityMapping.Rows[i]["SupCityId"].ToString();
                //    //XElement Facilities = HotelFacilities(RestelCity);
                    
                //}
                if (hotelIDs.Count == 0)
                {
                    #region Exception
                    CustomException ex1 = new CustomException("Hotel Not Found");
                    ex1.MethodName = "ThreadedHotelSearch";
                    ex1.PageName = "RestelServices";
                    ex1.CustomerID = customerid;
                    ex1.TranID = Req.Descendants("TransID").First().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                    return null;
                }
                List<StringBuilder> HotelIDList = new List<StringBuilder>();
                StringBuilder HotelIdString = new StringBuilder();
                int countCheck = 0;
                foreach (string id in hotelIDs)
                {
                    countCheck++;
                    int length = HotelIdString.Length;
                    if (HotelIdString.Length < 1740)
                    {
                        //if (HotelIdString.Length == 0)
                        HotelIdString.Append(id);
                        //else
                        //    HotelIdString.Append(",#" + id);
                    }
                    else
                    {
                        HotelIDList.Add(HotelIdString);
                        HotelIdString = new StringBuilder();
                    }
                    if (countCheck == hotelIDs.Count)
                        HotelIDList.Add(HotelIdString);
                }
                //var chunklist = BreakIntoChunks(hotelIDs, 200);
                //int Number = chunklist.Count;
                if (HotelIDList.Count == 0)
                    HotelIDList.Add(HotelIdString);
                int Number = HotelIDList.Count;
                //int c = hotelIDs.Distinct().Count();
                List<XElement> tr1 = new List<XElement>();
                List<XElement> tr2 = new List<XElement>();
                List<XElement> tr3 = new List<XElement>();
                List<XElement> tr4 = new List<XElement>();
                List<XElement> tr5 = new List<XElement>();
                List<XElement> HotelList = new List<XElement>();
                if (true)
                {
                    try
                    {
                        int timeOut = sup_cutime, count = 1;
                        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
                        timer.Start();
                        for (int i = 0; i < Number; i += threadCount)
                        {
                            
                            List<Thread> threadedlist;
                            int rangecount = threadCount;
                            if (HotelIDList.Count - i < threadCount)
                                rangecount = HotelIDList.Count - i;
                            var chn = HotelIDList.GetRange(i, rangecount);
                            #region rangecount equals 1
                            if (rangecount == 1)
                            {
                                threadedlist = new List<Thread>
                       {   
                           new Thread(()=> tr1 = HotelSearch(Req, HotelIDList.ElementAt(i),Facilities, timeOut))                       
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join(timeOut));
                                threadedlist.ForEach(t => t.Abort());
                                #region Add to list
                                HotelList.AddRange(tr1);
                                //foreach (XElement hotel in tr1)
                                //    HotelList.Add(hotel);
                                #endregion

                            }
                            #endregion
                            #region rangecount equals 2
                            else if (rangecount == 2)
                            {
                                threadedlist = new List<Thread>
                       {   
                           new Thread(()=> tr1 = HotelSearch(Req, HotelIDList.ElementAt(i),Facilities, timeOut)),
                           new Thread(()=> tr2 = HotelSearch(Req, HotelIDList.ElementAt(i+1),Facilities, timeOut))                       
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join(timeOut));
                                threadedlist.ForEach(t => t.Abort());
                                #region Add to List
                                //foreach (XElement hotel in tr1)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr2)
                                //    HotelList.Add(hotel);
                                HotelList.AddRange(tr1);
                                HotelList.AddRange(tr2);
                                #endregion
                            }
                            #endregion
                            #region rangecount equals 3
                            else if (rangecount == 3)
                            {
                                threadedlist = new List<Thread>
                       {   
                           new Thread(()=> tr1 = HotelSearch(Req, HotelIDList.ElementAt(i),Facilities, timeOut)),
                           new Thread(()=> tr2 = HotelSearch(Req, HotelIDList.ElementAt(i+1),Facilities, timeOut)),
                           new Thread(()=> tr3 = HotelSearch(Req, HotelIDList.ElementAt(i+2),Facilities, timeOut))
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join(timeOut));
                                threadedlist.ForEach(t => t.Abort());
                                #region Add to List
                                //foreach (XElement hotel in tr1)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr2)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr3)
                                //    HotelList.Add(hotel);
                                HotelList.AddRange(tr1);
                                HotelList.AddRange(tr2);
                                HotelList.AddRange(tr3);
                                #endregion
                            }
                            #endregion
                            #region rangecount equals 4
                            else if (rangecount == 4)
                            {
                                threadedlist = new List<Thread>
                       {

                           new Thread(()=> tr1 = HotelSearch(Req, HotelIDList.ElementAt(i),Facilities, timeOut)),
                           new Thread(()=> tr2 = HotelSearch(Req, HotelIDList.ElementAt(i+1),Facilities, timeOut)),
                           new Thread(()=> tr3 = HotelSearch(Req, HotelIDList.ElementAt(i+2),Facilities, timeOut)),
                           new Thread(()=> tr4 = HotelSearch(Req, HotelIDList.ElementAt(i+3),Facilities, timeOut))

                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join(timeOut));
                                threadedlist.ForEach(t => t.Abort());
                                #region Add to List
                                //foreach (XElement hotel in tr1)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr2)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr3)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr4)
                                //HotelList.Add(hotel);
                                HotelList.AddRange(tr1);
                                HotelList.AddRange(tr2);
                                HotelList.AddRange(tr3);
                                HotelList.AddRange(tr4);
                                    
                                #endregion
                            }
                            #endregion
                            #region rangecount equals 5
                            else if (rangecount == 5)
                            {
                                threadedlist = new List<Thread>
                       {

                           new Thread(()=> tr1 = HotelSearch(Req, HotelIDList.ElementAt(i),Facilities, timeOut)),
                           new Thread(()=> tr2 = HotelSearch(Req, HotelIDList.ElementAt(i+1),Facilities, timeOut)),
                           new Thread(()=> tr3 = HotelSearch(Req, HotelIDList.ElementAt(i+2),Facilities, timeOut)),
                           new Thread(()=> tr4 = HotelSearch(Req, HotelIDList.ElementAt(i+3),Facilities, timeOut)),
                           new Thread(()=> tr5 = HotelSearch(Req, HotelIDList.ElementAt(i+4),Facilities, timeOut))
                       };
                                threadedlist.ForEach(t => t.Start());
                                threadedlist.ForEach(t => t.Join(timeOut));
                                threadedlist.ForEach(t => t.Abort());
                                #region Add to List
                                //foreach (XElement hotel in tr1)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr2)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr3)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr4)
                                //    HotelList.Add(hotel);
                                //foreach (XElement hotel in tr5)
                                //    HotelList.Add(hotel);
                                HotelList.AddRange(tr1);
                                HotelList.AddRange(tr2);
                                HotelList.AddRange(tr3);
                                HotelList.AddRange(tr4);
                                HotelList.AddRange(tr5);
                                #endregion
                            }
                            #endregion
                            timeOut = timeOut - Convert.ToInt32(timer.ElapsedMilliseconds);
                            count++;
                        }
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "ThreadedHotelSearch";
                        ex1.PageName = "RestelServices";
                        ex1.CustomerID = customerid;
                        ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                        APILog.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                }
                else
                {
                    #region Request XML for Test phase
                    XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "13");
                    string codusu = suppliercred.Descendants("codusu").FirstOrDefault().Value;
                    string affiliation = suppliercred.Descendants("affiliation").FirstOrDefault().Value;

                    XDocument HotelsearchRequest = new XDocument(
                         new XDeclaration("1.0", "ISO-8859-1", "yes"),
                         new XElement("peticion",
                             new XElement("tipo", "110"),
                             new XElement("nombre", "Hotel Search request"),
                             new XElement("agencia", "test agency"),
                             new XElement("parametros",
                                 new XElement("hotel"),
                                 new XElement("pais", "MV"),
                                 new XElement("provincia", "MVMOL"),
                                 new XElement("poblacion"),
                                 new XElement("categoria", Req.Descendants("MinStarRating").First().Value),
                                 new XElement("pais_cliente", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                 new XElement("radio", "9"),
                                 new XElement("fechaentrada", reformdate(Req.Descendants("FromDate").First().Value)),
                                 new XElement("fechasalida", reformdate(Req.Descendants("ToDate").First().Value)),
                                 new XElement("marca", ""),
                                 new XElement("afiliacion", affiliation),
                                 new XElement("usuario", codusu),
                                 SearchRooms(Req),
                                 new XElement("idioma", "2"),
                                 new XElement("duplicidad", "0"),
                                 new XElement("comprimido", "2"),
                                 new XElement("informacion_hotel", "1"))));
                    #endregion
                    response = HotelSearchResponse(HotelsearchRequest, Req, Facilities, 11000);
                    foreach (XElement hotel in response.Descendants("Hotel"))
                        HotelList.Add(hotel);
                }
                removetags(Req);
                //List<string> numberOFhotels = HotelList.Descendants("HotelID").Select(x => x.Value).ToList();
                //var query = numberOFhotels.GroupBy(x => x)
                //                            .Where(g => g.Count() > 1)
                //                            .Select(y => y.Key)
                //                            .ToList();
                //if (query.Count > 0)
                //{
                //    foreach (string id in query)
                //        HotelList.Where(x => x.Descendants("HotelID").FirstOrDefault().Value.Equals(id)).FirstOrDefault().Remove();
                //}
                return HotelList;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ThreadedHotelSearch";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
                return null; 
            }
        }
        public List<XElement> HotelSearch(XElement Req, StringBuilder HotelIDs, XElement Facilities, int timeout)//List<string> chunkthread, XElement Facilities)
        {
            try
            {
                string cityID = Req.Descendants("CityID").First().Value;
                List<XElement> ThreadResult = new List<XElement>();
                string listofhotels = null;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "13");
                string codusu = suppliercred.Descendants("codusu").FirstOrDefault().Value;
                string affiliation = suppliercred.Descendants("affiliation").FirstOrDefault().Value;

                try
                {
                    //foreach (var entry in chunkthread)
                    //    listofhotels = listofhotels + entry;
                    listofhotels = HotelIDs.ToString();
                    #region Request XML
                    XDocument HotelsearchRequest = new XDocument(
                         new XDeclaration("1.0", "ISO-8859-1", "yes"),
                         new XElement("peticion",
                             new XElement("tipo", "110"),
                             new XElement("nombre", "Hotel Search request"),
                        //new XElement("agencia", "test agency"),
                             new XElement("parametros",
                                 new XElement("hotel", listofhotels),
                                 new XElement("pais"),
                                 new XElement("provincia"),
                                 new XElement("poblacion"),
                                 new XElement("categoria", Req.Descendants("MinStarRating").First().Value),
                                 new XElement("pais_cliente", Req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                 new XElement("radio"),
                                 new XElement("fechaentrada", reformdate(Req.Descendants("FromDate").First().Value)),
                                 new XElement("fechasalida", reformdate(Req.Descendants("ToDate").First().Value)),
                                 new XElement("marca", ""),
                                 new XElement("afiliacion", affiliation),
                                 new XElement("usuario", codusu),
                                 SearchRooms(Req),
                                 new XElement("idioma", "2"),
                                 new XElement("duplicidad", "0"),
                                 new XElement("comprimido", "2"),
                                 new XElement("informacion_hotel", "1"),
                                 new XElement("tarifas_reembolsables","1")
                                 )));
                    #endregion
                    XElement response = HotelSearchResponse(HotelsearchRequest, Req, Facilities, timeout);
                    ThreadResult.AddRange(response.Descendants("Hotel"));
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelSearch";
                    ex1.PageName = "RestelServices";
                    ex1.CustomerID = customerid;
                    ex1.TranID = Req.Descendants("TransID").First().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                return ThreadResult;
            }
            catch(Exception ex) 
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearch";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
                return null; 
            }
        }

        #region Response
        public XElement HotelSearchResponse(XDocument RestelRequest, XElement travReq, XElement Facilities, int timeout)
        {
            DateTime starttime = DateTime.Now;
            XElement response = null;
            try
            {
                //LogModel model = new LogModel
                //{
                //    CustomerID = Convert.ToInt32(travReq.Descendants("CustomerID").FirstOrDefault().Value),
                //    Supl_Id = 13,
                //    Logtype = "Search",
                //    LogtypeID = 1,
                //    TrackNo = travReq.Descendants("TransID").FirstOrDefault().Value
                //};
                //serverResponse = serverRequest.RestelResponse(RestelRequest, travReq.Descendants("CustomerID").FirstOrDefault().Value, model);
                RestelServerRequest serverRequestsrc = new RestelServerRequest();
                XDocument serverResponsesrc = serverRequestsrc.RestelResponseSearch(RestelRequest, customerid, timeout, travReq.Descendants("TransID").FirstOrDefault().Value);
                #region Log Save
                XElement responseRNS = removecdata(serverResponsesrc.Root);
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(customerid);
                    log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                    log.SupplierID = 13;
                    log.logrequestXML = RestelRequest.ToString();
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
                    ex1.PageName = "RestelServices";
                    ex1.CustomerID = customerid;
                    ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                XElement Hotels = new XElement("Hotels");
                #region Response XML
                if (serverResponsesrc.Descendants("hot").Any())
                {
                    string xmlouttype = string.Empty;
                    try
                    {
                        if (dmc == "Restel")
                        {
                            xmlouttype = "false";
                        }
                        else
                        { xmlouttype = "true"; }
                    }
                    catch { }
                    foreach (XElement RstHotel in serverResponsesrc.Descendants("hot"))
                    {
                        string minstar = travReq.Descendants("MinStarRating").First().Value;
                        string maxstar = travReq.Descendants("MaxStarRating").First().Value;
                        string hotelstar = RstHotel.Descendants("cat").First().Value;
                        if (!RstHotel.Descendants("dir").First().Value.Equals(null) && StarRating(minstar, maxstar, hotelstar))
                        {
                            try
                            {
                                //XElement Fac = null;
                                //List<XElement> Facili = Facilities.Descendants("Facilities").Where(x => x.Attribute("HotelID").Value.Equals(RstHotel.Element("cod").Value)).Any() ? Facilities.Descendants("Facilities").Where(x => x.Attribute("HotelID").Value.Equals(RstHotel.Element("cod").Value)).First().Elements().ToList() : null;
                                //if (Facili != null && Facili.Count > 0)
                                //    Fac = new XElement("Facilities", Facili);
                                //else
                                //    Fac = new XElement("Facilities", new XElement("Facility", "No Facility Available"));

                                //    List<double> minRate = new List<double>();
                                //    foreach (XElement res in RstHotel.Descendants("reg"))
                                //        minRate.Add(Convert.ToDouble(res.Attribute("prr").Value));
                                Hotels.Add(new XElement("Hotel",
                                                        new XElement("HotelID", RstHotel.Element("cod").Value),
                                                        new XElement("HotelName", RstHotel.Element("nom").Value),
                                                        new XElement("PropertyTypeName", RstHotel.Descendants("tipo_establecimiento").First().Value),
                                                        new XElement("CountryID", travReq.Descendants("CountryID").First().Value),
                                                        new XElement("CountryName", travReq.Descendants("CountryName").First().Value),
                                                        new XElement("CountryCode", travReq.Descendants("CountryCode").First().Value),
                                                        new XElement("CityId", travReq.Descendants("CityID").First().Value),
                                                        new XElement("CityCode", travReq.Descendants("CityCode").First().Value),
                                                        new XElement("CityName", travReq.Descendants("CityName").First().Value),
                                                        new XElement("AreaId"),
                                                        new XElement("AreaName"),
                                                        new XElement("RequestID"),
                                                        new XElement("Address", RstHotel.Descendants("dir").First().Value),
                                                        new XElement("Location"),
                                                        new XElement("Description"),
                                                        new XElement("StarRating", RstHotel.Element("cat").Value),
                                                        new XElement("MinRate", MinRate(RstHotel.Descendants("res").First(), travReq)),
                                                        new XElement("HotelImgSmall", RstHotel.Descendants("thumbnail").First().Value),
                                                        new XElement("HotelImgLarge", RstHotel.Descendants("foto").First().Value),
                                                        new XElement("MapLink"),
                                                        new XElement("Longitude", RstHotel.Descendants("lon").First().Value),
                                                        new XElement("Latitude", RstHotel.Descendants("lat").First().Value),
                                                        new XElement("xmloutcustid", customerid),
                                                        new XElement("xmlouttype", xmlouttype),
                                                        new XElement("DMC", dmc),
                                                        new XElement("SupplierID", supplierid),
                                                        new XElement("Currency", currency(RstHotel.Descendants("reg").First().Attribute("div").Value)),
                                                        new XElement("Offers"),
                                                        new XElement("Facilities", null),
                                    //new XElement("Facilities", new XElement("Facility", "No Facility Available")),
                                                        new XElement("Rooms")));
                            }
                            catch { }
                            
                        }
                    }
                }
                #endregion
                response = new XElement("searchResponse", Hotels);
                //if (response.Descendants("HotelID").Any())
                //    response.Descendants("Hotel").Where(x => x.DescendantNodes().Count() == 0).Remove();
                //response.Descendants("Hotel").Where(x => x.Descendants("MinRate").First().Value.Equals("0.0")).Remove();
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelSearchResponse";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = customerid;
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
        public XElement RoomAvailability_restel(XElement Req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = Req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "13").ToList();
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
                        catch { custName = "HA"; }
                    }
                    else
                    {
                        try
                        {
                            customerid = htlele[i].Attribute("custID").Value;
                        }
                        catch { }
                        dmc = "Restel";
                    }
                    roomavailabilityresponse.Add(RoomAvailability(Req, dmc, htlid, 13));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        #endregion
        #region Room Availability
        public XElement RoomAvailability(XElement Req, string xtype, string htlid, int supid)
        {
            dmc = xtype;
            hotelid = htlid;
            supplierid = supid;
            DateTime starttime = DateTime.Now;
            XElement RoomList = roomlist(Req.Descendants("TransID").First().Value);
            XElement resp = RoomList.Descendants("hot")
                            .Where(x => x.Descendants("cod").First().Value
                                .Equals(hotelid)).First();
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
                log.SupplierID = 13;
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
                ex1.PageName = "RestelServices";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            if (resp != null)
            {
                #region Response XML
                var availableRooms = new XElement("Hotel",
                                               new XElement("HotelID", resp.Element("cod").Value),
                                               new XElement("HotelName", resp.Element("nom").Value),
                                               new XElement("PropertyTypeName", resp.Descendants("tipo_establecimiento").First().Value),
                                               new XElement("CountryID", Req.Descendants("CountryID").First().Value),
                                               //new XElement("CountryCode"),
                                               //new XElement("CountryName", Req.Descendants("CountryName").First().Value),
                                               //new XElement("CityId", Req.Descendants("CityID").First().Value),
                                               //new XElement("CityCode", Req.Descendants("CityCode").First().Value),
                                               //new XElement("CityName", Req.Descendants("CityName").First().Value),
                                               //new XElement("AreaName"),
                                               //new XElement("AreaId"),
                                               //new XElement("Address", resp.Descendants("dir").First().Value),
                                               //new XElement("Location"),
                                               //new XElement("Description"),
                                               //new XElement("StarRating", resp.Descendants("cat").First().Value),
                                               //new XElement("MinRate"),
                                               //new XElement("HotelImgSmall", resp.Descendants("thumbnail").First().Value),
                                               //new XElement("HotelImgLarge", resp.Descendants("foto").First().Value),
                                               //new XElement("MapLink"),
                                               //new XElement("Longitude", resp.Descendants("lon").First().Value),
                                               //new XElement("Latitude", resp.Descendants("lat").First().Value),
                                               new XElement("DMC", dmc),
                                               new XElement("SupplierID", supplierid),
                                               new XElement("Currency", currency(resp.Descendants("reg").First().Attribute("div").Value)),
                                              new XElement("Offers"),
                                               new XElement("Facilities", new XElement("Facility", "No Facility available")),
                                               groupedRooms(resp.Descendants("res").First(), Req, resp.Element("cod").Value, currency(resp.Descendants("reg").First().Attribute("div").Value), dmc));
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
                                                        new XElement(removeAllNamespaces(Req.Descendants("searchRequest").First())),
                                                       removeAllNamespaces(RoomResponse))));
            #endregion
            return AvailablilityResponse;

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

                XDocument restelreq = new XDocument(
                                        new XDeclaration("1.0", "utf-8", "yes"),
                                        new XElement("peticion",
                                            new XElement("tipo", "15"),
                                            new XElement("parametros",
                                                new XElement("codigo", req.Descendants("HotelID").First().Value),
                                                new XElement("idioma", "2"))));
                #endregion
                response = HotelDetailsResponse(restelreq, req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "hoteldetails";
                ex1.PageName = "RestelServices";
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
        public XElement HotelDetailsResponse(XDocument restelreq, XElement travayooReq)
        {
            DataTable Details = rlac.GetRestelHotelDetails(travayooReq.Descendants("HotelID").First().Value);
            DataRow dr = Details.Rows[0];
            XElement ima = XElement.Parse(dr["HotelXml"].ToString());
            XElement Tempfac = XElement.Parse(dr["Facilities"].ToString());
            List<XElement> Imag = new List<XElement>();
            if (ima.HasElements)
            {

                foreach (XElement pic in ima.Descendants("foto"))
                {
                    Imag.Add(new XElement("Image",
                                    new XAttribute("Path", pic.Value),
                                    new XAttribute("Caption", string.Empty)));
                }
            }
            List<XElement> Services = new List<XElement>();
            if (Tempfac.HasElements)
            {
                foreach (XElement ser in Tempfac.Descendants("servicio"))
                    Services.Add(new XElement("Facility", ser.Element("desc_serv").Value));
            }
            #region Response XML
            var hotels = new XElement("Hotels",
                              new XElement("Hotel",
                                  new XElement("HotelID", travayooReq.Descendants("HotelID").First().Value),
                                  new XElement("Description", dr["Description"].ToString()),
                                  new XElement("Images", Imag),
                                  new XElement("Facilities", Services),
                                  new XElement("ContactDetails",
                                      new XElement("Phone", dr["Telephone"].ToString()),
                                      new XElement("Fax")),
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
            List<XElement> lines = new List<XElement>();
            XElement lines1 = null;
            try
            {
                List<XElement> linefilter = new List<XElement>();
                foreach (XElement room in Req.Descendants("Room"))
                {
                    linefilter.Add(new XElement("lin", room.Attribute("ID").Value));
                }
                var linegrouping = from l in linefilter
                                   group l by l.Value;

                foreach (var linegroup in linegrouping)
                {
                    lines1 = getlinetags(linegroup.Key, Req.Descendants("TransID").First().Value, Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value);
                    foreach (XElement li in lines1.Descendants("lin"))
                    {
                        string newlinevalues = string.Empty;

                        lines.Add(li);
                    }
                }
                #region Request XML
                XDocument RestelRequest = new XDocument(
                             new XDeclaration("1.0", "ISO-8859-1", "yes"),
                             new XElement("peticion",
                                 new XElement("tipo", "144"),
                                 new XElement("nombre"),
                                 new XElement("agencia"),
                                 new XElement("parametros",
                                     new XElement("datos_reserva",
                                         //new XElement("hotel", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                             lines),
                                         new XElement("idioma", "1"))));
                #endregion
                response = cancellationPolicyResponse(RestelRequest, Req);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "cancellationPolicy";
                ex1.PageName = "RestelServices";
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
        #region Response
        public XElement cancellationPolicyResponse(XDocument RestelRequest, XElement travReq)
        {
            DateTime starttime = DateTime.Now;
            XElement cancelresponse = new XElement("HotelDetailwithcancellationResponse");
            //LogModel model = new LogModel
            //{
            //    CustomerID = Convert.ToInt32(travReq.Descendants("CustomerID").FirstOrDefault().Value),
            //    Supl_Id = 13,
            //    Logtype = "CXPolicy",
            //    LogtypeID = 3,
            //    TrackNo = travReq.Descendants("TransID").FirstOrDefault().Value
            //};
            serverResponse = serverRequest.RestelResponse(RestelRequest, travReq.Descendants("CustomerID").FirstOrDefault().Value, travReq.Descendants("TransID").FirstOrDefault().Value);
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement RestelRNS = removeAllNamespaces(RestelRequest.Root);
                XElement servresp = removecdata(serverResponse.Root);
                #endregion
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = 13;
                log.logrequestXML = RestelRNS.ToString();
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
                ex1.PageName = "RestelServices";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            try
            {
                if (serverResponse.Descendants("error").Any())
                {
                    cancelresponse.Add(new XElement("ErrorTxt", serverResponse.Descendants("error").First().Descendants("descripcion").First().Value));
                    return cancelresponse;
                }
                var lines = RestelRequest.Descendants("lin");
                double perNightRate = 0.0;
                double totalRate = 0.0;
                foreach (XElement room in travReq.Descendants("Room"))
                {
                    totalRate += Convert.ToDouble(room.Attribute("TotalRoomRate").Value);
                    perNightRate = perNightRate + Convert.ToDouble(room.Attribute("PerNightRoomRate").Value);
                }

                XElement cp = new XElement("CancellationPolicies");
                var exc = serverResponse.Descendants("politicaCanc");
                var groupresponse = from pol in serverResponse.Descendants("politicaCanc").ToList()
                                    group pol by new
                                    {
                                        a1 = pol.Attribute("fecha").Value,
                                        a2 = pol.Descendants("dias_antelacion").First().Value,
                                        a3 = pol.Descendants("noches_gasto").First().Value,
                                        a4 = pol.Descendants("estCom_gasto").First().Value
                                    };
                List<XElement> PoliciesAfterGrouping = new List<XElement>();
                foreach (var group in groupresponse)
                {
                    XElement test = new XElement("test");
                    List<double> price = new List<double>();
                    foreach (XElement policy in group)
                    {
                        double nights = 0;
                        double pricecheck = 0.0;
                        if (!policy.Descendants("noches_gasto").First().Value.Equals("0"))
                        {
                            nights = Convert.ToDouble(policy.Descendants("noches_gasto").First().Value);
                            pricecheck = nights * perNightRate;
                        }
                        else
                        {
                            double per = Convert.ToDouble(policy.Descendants("estCom_gasto").First().Value);
                            nights = per / 100;
                            pricecheck = nights * totalRate;
                        }
                        pricecheck = Math.Round(pricecheck, 2, MidpointRounding.AwayFromZero);
                        policy.Add(new XElement("priceCharged", pricecheck));
                        price.Add(pricecheck);
                        test.Add(policy);
                    }
                    string selectedprice = Convert.ToString(price.Max());
                    PoliciesAfterGrouping.Add(test.Descendants("politicaCanc")
                        .Where(x => x.Element("priceCharged").Value.Equals(selectedprice))
                        .First());
                }
                #region Cancellation Policy Tag
                foreach (XElement policy in PoliciesAfterGrouping)
                {
                    int daysbefore = Convert.ToInt32(policy.Descendants("dias_antelacion").First().Value);
                    // DateTime date = DateTime.ParseExact(policy.Attribute("fecha").Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    DateTime date = DateTime.ParseExact(travReq.Descendants("FromDate").FirstOrDefault().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    date = date.AddDays(-daysbefore);
                    string dt = date.ToString("dd/MM/yyyy");
                    dt = dt.Replace('-', '/');
                    double nightsCharged = 0;
                    double rate = 0.0;
                    if (!policy.Descendants("noches_gasto").First().Value.Equals("0"))
                    {
                        nightsCharged = Convert.ToDouble(policy.Descendants("noches_gasto").First().Value);
                        rate = nightsCharged * perNightRate;
                    }
                    else
                    {
                        double per = Convert.ToDouble(policy.Descendants("estCom_gasto").First().Value);
                        nightsCharged = per / 100;
                        rate = nightsCharged * totalRate;
                    }
                    cp.Add(new XElement("CancellationPolicy",
                                        new XAttribute("LastCancellationDate", dt),
                                        new XAttribute("ApplicableAmount", Convert.ToString(rate)),
                                        new XAttribute("NoShowPolicy", "0")));
                }
                //var cpgroup = from policies in cp.Descendants("CancellationPolicy")
                //              group policies by policies.Attribute("LastCancellationDate").Value;
                //XElement cxpolicies = new XElement("CancellationPolicies");
                //foreach(var group in cpgroup)
                //{
                //    double price = 0.0;
                //    foreach(XElement room in group.Descendants("Room"))
                //    {
                //        double check = Convert.ToDouble(room.Attribute("ApplicableAmount").Value);
                //        if (check > price)
                //            price = check;
                //    }
                //    cxpolicies.Add(new XElement("CancellationPolicy",
                //                        new XAttribute("LastCancellationDate", group.Key),
                //                        new XAttribute("ApplicableAmount", Convert.ToString(price)),
                //                        new XAttribute("NoShowPolicy", "0")));
                //}
                List<XElement> mergeinput = new List<XElement>();
                mergeinput.Add(cp);
                XElement finalcp = MergCxlPolicy(mergeinput);
                #endregion
                #region Response XML
                if (finalcp.Descendants("CancellationPolicy").Any() && finalcp.Descendants("CancellationPolicy").Last().HasAttributes)
                {
                    var cxp = new XElement("Hotels",
                             new XElement("Hotel",
                                 new XElement("HotelID", travReq.Descendants("HotelID").First().Value),
                                 new XElement("HotelName", travReq.Descendants("HotelName").First().Value),
                                 new XElement("HotelImgSmall"),
                                 new XElement("HotelImgLarge"),
                                 new XElement("MapLink"),
                                 new XElement("DMC", "Restel"),
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
        public XElement PreBookingRequest(XElement Req, int supid, string xtype)
        {
            supplierid = supid;
            dmc = xtype;
            XElement response = null;
            string cityID = Req.Descendants("CityID").First().Value;


            #region Credentials
            string username = Req.Descendants("UserName").Single().Value;
            string password = Req.Descendants("Password").Single().Value;
            string AgentID = Req.Descendants("AgentID").Single().Value;
            string ServiceType = Req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = Req.Descendants("ServiceVersion").Single().Value;
            #endregion
            #region Pre-Booking Restel Service-3
            string firstline = null;
            string trackNumber = Req.Descendants("TransID").First().Value;
            string HotelID = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value;
            List<XElement> prebooklines = new List<XElement>();
            try
            {

                foreach (XElement room in Req.Descendants("Room"))
                {
                    firstline = room.Attribute("ID").Value;
                    prebooklines.Add(new XElement("lin", firstline));
                }
                var linegrouping = from line in prebooklines
                                   group line by line.Value;
                List<XElement> finalprebookinglines = new List<XElement>();
                foreach (var linegroup in linegrouping)
                {
                    XElement linefirst = new XElement("lin", linegroup.Key);
                    List<XElement> allLines = new List<XElement>();
                    XElement linesforalldates = getlinetags(linefirst.Value, Req.Descendants("TransID").First().Value, Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value);
                    foreach (XElement abc in linesforalldates.Descendants("lin"))
                    {
                        string newlinevalues = string.Empty;

                        finalprebookinglines.Add(abc);
                    }
                }
                List<string> occupancyTypes = new List<string>();
                foreach (XElement x in Req.Descendants("paxes"))
                    occupancyTypes.Add(x.Value);
                int numberofoccupancy = occupancyTypes.Distinct().Count();

                #region Request XML
                XDocument PreBookReq = new XDocument(
                                                   new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                                   new XElement("peticion",
                                                       new XElement("nombre"),
                                                       new XElement("agencia"),
                                                       new XElement("tipo", "202"),
                                                       new XElement("parametros",
                                                           new XElement("codigo_hotel", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                           new XElement("nombre_cliente", "test"),
                                                           new XElement("observaciones", ""),
                                                           new XElement("num_mensaje", ""),
                                                           new XElement("forma_pago", "25"),
                                                           new XElement("res", finalprebookinglines))));
                #endregion
            #endregion
                response = PreBookingResponse(Req, PreBookReq);
                if (!response.Descendants("ErrorTxt").Any())
                {
                    #region PreBooking Cancellation
                    string prebookingID = response.Descendants("Room").First().Attribute("SessionID").Value;
                    XDocument PreBookCancellation = new XDocument(
                                                        new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                                        new XElement("peticion",
                                                            new XElement("tipo", "3"),
                                                            new XElement("parametros",
                                                                new XElement("localizador", prebookingID),
                                                                new XElement("accion", "AI"))));
                    //LogModel model = new LogModel
                    //{
                    //    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                    //    Supl_Id = 13,
                    //    Logtype = "PreBook(Cancel)",
                    //    LogtypeID = 4,
                    //    TrackNo = Req.Descendants("TransID").FirstOrDefault().Value
                    //};
                    XDocument Service202Cancel = serverRequest.RestelResponse(PreBookCancellation, Req.Descendants("CustomerID").FirstOrDefault().Value, Req.Descendants("TransID").FirstOrDefault().Value);
                }
                    #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookingRequest";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion

            }

            #region Response Format
            string oldprice = Req.Descendants("RoomTypes").First().Attribute("TotalRate").Value;
            string newprice = Req.Descendants("RoomTypes").First().Attribute("TotalRate").Value;
            XElement prebookingfinal = new XElement(
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
            #region Price Change Condition
            if (oldprice.Equals(newprice))
                return prebookingfinal;
            else
            {
                prebookingfinal.Descendants("HotelPreBookingResponse").Descendants("Hotels").First().AddBeforeSelf(
                   new XElement("ErrorTxt", "Amount has been changed"),
                   new XElement("NewPrice", newprice));
                return prebookingfinal;
            }
            #endregion
            #endregion
        }
        #region Response
        public XElement PreBookingResponse(XElement travReq, XDocument prebook2)
        {
            XElement resp = null;
            XElement response = new XElement("HotelPreBookingResponse");
            DateTime starttime = DateTime.Now;
            resp = roomlist(travReq.Descendants("TransID").First().Value).Descendants("hot")
                            .Where(x => x.Descendants("cod").First().Value.Equals(travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value))
                            .First();
            //Commented on 10 apr 2019 
            List<XElement> resp2 = LogXMLs(travReq.Descendants("TransID").First().Value, 2, 0);

            //XElement CurrentHotel = resp2.Descendants("Response")
            //                       .Where(x => x.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value.Equals(travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value))
            //                       .First();
            //Commented on 10 apr 2019 
            XElement CurrentHotel = resp2.Descendants("Response").
                                    Where(x => x.Descendants("RoomTypes").Where(y => y.Attribute("HtlCode").Value.Equals(travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value)).Any())
                                    .FirstOrDefault();
            //serverResponse = serverRequest.RestelResponse(prebook2, travReq.Descendants("CustomerID").FirstOrDefault().Value, travReq.Descendants("TransID").FirstOrDefault().Value);
            #region Log Save
            try
            {

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = supplierid;
                log.logrequestXML = null;
                log.logresponseXML = CurrentHotel.ToString();
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
                ex1.PageName = "RestelServices";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            string CurrentIndex = travReq.Descendants("RoomTypes").First().Attribute("Index").Value;

            XElement CurrentRoom = CurrentHotel.Descendants("RoomTypes")
                                    .Where(x => x.Attribute("Index").Value.Equals(CurrentIndex))
                                    .First();
            string bookingamount = CurrentRoom.Attribute("TotalRate").Value;

            #region Hotel Remarks
            XDocument Remark = new XDocument(new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                   new XElement("peticion",
                                       new XElement("nombre"),
                                       new XElement("agencia"),
                                       new XElement("tipo", "24"),
                                       new XElement("parametros",
                // new XElement("codigo_cobol", travReq.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                // new XElement("entrada", travReq.Descendants("FromDate").First().Value),
                // new XElement("salida", travReq.Descendants("ToDate").First().Value)
                                           prebook2.Descendants("lin"))));
            DateTime startime = DateTime.Now;
            //LogModel model = new LogModel
            //{
            //    CustomerID = Convert.ToInt32(travReq.Descendants("CustomerID").FirstOrDefault().Value),
            //    Supl_Id = 13,
            //    Logtype = "PreBook(Remarks)",
            //    LogtypeID = 4,
            //    TrackNo = travReq.Descendants("TransID").FirstOrDefault().Value
            //};
            XDocument RemarkResponse = serverRequest.RestelResponse(Remark, travReq.Descendants("CustomerID").FirstOrDefault().Value, travReq.Descendants("TransID").FirstOrDefault().Value);
            #region Log Save
            try
            {

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = supplierid;
                log.logrequestXML = Remark.ToString();
                log.logresponseXML = removecdata(RemarkResponse.Root).ToString();
                log.LogTypeID = 4;
                log.LogType = "PreBook(Remark)";
                log.StartTime = startime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookingResponse";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            List<string> Remarks = new List<string>();
            if (RemarkResponse.Descendants("obs_texto").Any())
            {
                string TnCLine = null;
                foreach (XElement obser in RemarkResponse.Descendants("observacion"))
                {
                    string from = null, todate = null;
                    if (obser.Descendants("obs_desde").Any() && !String.IsNullOrEmpty(obser.Descendants("obs_desde").FirstOrDefault().Value))
                    {
                        from = obser.Descendants("obs_desde").Any() ? obser.Descendants("obs_desde").FirstOrDefault().Value : string.Empty;
                        from = from.Insert(6, "/");
                        from = from.Insert(4, "/");
                    }
                    if (obser.Descendants("obs_hasta").Any() && !String.IsNullOrEmpty(obser.Descendants("obs_hasta").FirstOrDefault().Value))
                    {
                        todate = obser.Descendants("obs_hasta").Any() ? obser.Descendants("obs_hasta").FirstOrDefault().Value : string.Empty;
                        todate = todate.Insert(6, "/");
                        todate = todate.Insert(4, "/");
                    }
                    TnCLine = obser.Descendants("obs_texto").FirstOrDefault().Value;
                    if (from != null && todate != null)
                    {
                        TnCLine += "<br> <b>Applicable From</b> " + from + " <b>To</b> " + todate;
                    }
                    Remarks.Add(TnCLine);
                }

            }
            string conditions = null;
            int index = 0;
            foreach (string t in Remarks)
            {
                index++;
                conditions += "<ul><li>" + t + "</li></ul><br>";
                //if(index == 1)
                //    conditions += "<li>" + t + "</li><br>";
                //else if (index == Remarks.Count)
                //   conditions += "<li>" + t + "</li>";
                //else
                //   conditions += "<li> " + t +"</li><br>";
            }
            if (resp.Descendants("city_tax").Any())
                conditions += "<ul><li>" + resp.Descendants("city_tax").FirstOrDefault().Value + "</li></ul><br>";
            #endregion



            if (resp != null)
            {
                #region Response Xml
                var preBookResponse = new XElement("Hotels",
                                                               new XElement("Hotel",
                                                                   new XElement("HotelID"),
                                                                   new XElement("HotelName"),
                                                                   new XElement("Status", "true"),
                                                                   new XElement("TermCondition", conditions),
                                                                   new XElement("HotelImgSmall"),
                                                                   new XElement("HotelImgLarge"),
                                                                   new XElement("MapLink"),
                                                                   new XElement("DMC", dmc),
                                                                   new XElement("Currency", "EUR"),
                                                                    new XElement("Offers"),
                                                                  new XElement("Rooms",
                                                                      new XElement("RoomTypes",
                                                                          new XAttribute("Index", "1"),
                                                                          new XAttribute("TotalRate", bookingamount),
                                                                          prebookrooms(travReq)

                                                                          ))
                                                                   ));
                XElement cp = cancellationPolicy(travReq).Descendants("CancellationPolicies").First();
                preBookResponse.Descendants("Room").Last().AddAfterSelf(cp);
                #endregion
                response.Add(preBookResponse);
            }
            else
            {
                if (serverResponse.Descendants("datos").Any())
                    response.Add(new XElement("ErrorTxt", serverResponse.Descendants("datos").First().Value));
                else if (serverResponse.Descendants("error").Any())
                    response.Add(new XElement("ErrorTxt", serverResponse.Descendants("error").First().Descendants("descripcion").First().Value));
            }
            return response;
        }
        #region Pre-Booking Rooms
        public XElement PreRooms(XElement RestelRooms, XElement travReq)
        {
            int index = 0;
            XElement response = new XElement("Rooms");
            double totalRate = 0.0;
            List<XElement> Rooms = new List<XElement>();
            foreach (XElement room in RestelRooms.Descendants("pax"))
            {
                foreach (XElement Roomtag in room.Descendants("reg"))
                {
                    totalRate = totalRate + Convert.ToDouble(Roomtag.Attribute("prr").Value);

                    Rooms.Add(PBRoomTag(Roomtag, room.Attribute("cod").Value));
                }
            }
            var roomtypes = new XElement("RoomTypes",
                                new XAttribute("TotalRate", ""),
                                new XAttribute("Index", index++),
                                Rooms);
            response.Add(roomtypes);
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
            #region Pre-Booking Restel Service-3
            string firstline = null;
            string trackNumber = Req.Descendants("TransactionID").First().Value;
            string HotelID = Req.Descendants("HotelID").First().Value;
            List<XElement> prebooklines = new List<XElement>();
            foreach (XElement room in Req.Descendants("Room"))
            {
                firstline = room.Attribute("RoomTypeID").Value;
                XElement lineforroom = getlinetags(firstline, trackNumber, HotelID);
                foreach (XElement li in lineforroom.Descendants("lin"))
                    prebooklines.Add(li);
            }
            var leadpax = from pax in Req.Descendants("PaxInfo")
                          where pax.Element("IsLead").Value.Equals("true")
                          select pax.Element("FirstName").Value + " " + pax.Element("LastName").Value;
            try
            {
                List<XElement> prebooklines1 = new List<XElement>();
                foreach (XElement room in Req.Descendants("Room"))
                {
                    firstline = room.Attribute("RoomTypeID").Value;
                    prebooklines1.Add(new XElement("lin", firstline));
                }
                var linegrouping = from line in prebooklines1
                                   group line by line.Value;
                List<XElement> finalprebookinglines = new List<XElement>();
                foreach (var linegroup in linegrouping)
                {
                    XElement linefirst = new XElement("lin", linegroup.Key);
                    List<XElement> allLines = new List<XElement>();
                    XElement linesforalldates = getlinetags(linefirst.Value, Req.Descendants("TransactionID").First().Value, Req.Descendants("HotelID").First().Value);
                    foreach (XElement abc in linesforalldates.Descendants("lin"))
                    {
                        //string newlinevalues = string.Empty;
                        //if (linegroup.Count() == 1)
                        //{

                        //    string[] linvalues = getlinevalues(abc);
                        //    for (int i = 0; i < linvalues.Length; i++)
                        //    {
                        //        if (i == 1)
                        //            newlinevalues += "1#";
                        //        else
                        //            newlinevalues += linvalues[i] + "#";
                        //    }
                        //    finalprebookinglines.Add(new XElement("lin", newlinevalues));
                        //}
                        //else
                        finalprebookinglines.Add(abc);
                    }
                }
                List<string> occupancyTypes = new List<string>();
                foreach (XElement x in Req.Descendants("paxes"))
                    occupancyTypes.Add(x.Value);
                int numberofoccupancy = occupancyTypes.Distinct().Count();
                XDocument PreBookReq = new XDocument(
                                               new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                               new XElement("peticion",
                                                   new XElement("tipo", "202"),
                                                   new XElement("parametros",
                                                       new XElement("codigo_hotel", Req.Descendants("HotelID").First().Value),
                                                       new XElement("nombre_cliente", leadpax.First().ToString()),
                                                       new XElement("observaciones", Req.Descendants("SpecialRemarks").FirstOrDefault().Value),
                                                       new XElement("num_mensaje", ""),
                                                       new XElement("forma_pago", "25"),
                                                       new XElement("res", finalprebookinglines),
                                                       new XElement("paxes",
                                                            from guest in Req.Descendants("PaxInfo").ToList()
                                                            select new XElement("pax",
                                                                new XElement("titulo", guest.Descendants("Title").FirstOrDefault().Value),
                                                                 new XElement("nombrePax", guest.Descendants("FirstName").FirstOrDefault().Value),
                                                                  new XElement("apellidos", guest.Descendants("LastName").FirstOrDefault().Value),
                                                                   new XElement("edad", guest.Descendants("Age").FirstOrDefault().Value == "0" ? "30" : guest.Descendants("Age").FirstOrDefault().Value)
                                                             )))));
            #endregion
                DateTime starttime = DateTime.Now;
                //LogModel model = new LogModel
                //{
                //    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
                //    Supl_Id = 13,
                //    Logtype = "PreBook",
                //    LogtypeID = 4,
                //    TrackNo = Req.Descendants("TransactionID").FirstOrDefault().Value
                //};
                XDocument service202Response = serverRequest.RestelResponse(PreBookReq, Req.Descendants("CustomerID").FirstOrDefault().Value, Req.Descendants("TransactionID").FirstOrDefault().Value);
                #region Log Save
                try
                {
                    #region Removing Namespaces
                    XElement servresp = removecdata(service202Response.Root);
                    XElement RestRns = removeAllNamespaces(PreBookReq.Root);
                    #endregion
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = Req.Descendants("TransactionID").FirstOrDefault().Value;
                    log.SupplierID = 13;
                    log.logrequestXML = RestRns.ToString();
                    log.logresponseXML = servresp.ToString();
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
                    ex1.PageName = "RestelServices";
                    ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                string prebookingID = service202Response.Descendants("n_localizador").First().Value;
                if (!string.IsNullOrEmpty(prebookingID))
                {
                    #region Request XML
                    double oldrate = 0.0;
                    oldrate = Convert.ToDouble(Req.Descendants("TotalAmount").First().Value);
                    newrate = Convert.ToDouble(service202Response.Descendants("importe_total_reserva").First().Value);
                    if (oldrate != newrate)
                    {
                        condition = true;
                        XElement PriceChangeAtBooking = new XElement(
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
                                                                new XElement("Hotels"),
                                                                new XElement("ErrorTxt", "Amount has been changed"),
                                                                new XElement("NewPrice", Convert.ToString(newrate))))));
                        return PriceChangeAtBooking;
                    }
                    XDocument Bookingconfirmation = new XDocument(
                                                        new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                                        new XElement("peticion",
                                                            new XElement("tipo", "3"),
                                                            new XElement("parametros",
                                                                new XElement("localizador", prebookingID),
                                                                new XElement("accion", "AE"))));
                    #endregion
                    response = bookingResponse(Bookingconfirmation, Req, prebooklines);
                }
                else
                {
                    response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", service202Response.Descendants("datos").FirstOrDefault().Value));
                }
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
        #region Response
        public XElement bookingResponse(XDocument RestelRequest, XElement travReq, List<XElement> prebookinglines)
        {
            XElement response = null;
            DateTime starttime = DateTime.Now;
            //LogModel model = new LogModel
            //{
            //    CustomerID = Convert.ToInt32(travReq.Descendants("CustomerID").FirstOrDefault().Value),
            //    Supl_Id = 13,
            //    Logtype = "Book",
            //    LogtypeID = 5,
            //    TrackNo = travReq.Descendants("TransactionID").FirstOrDefault().Value
            //};
            serverResponse = serverRequest.RestelResponse(RestelRequest, travReq.Descendants("CustomerID").FirstOrDefault().Value, travReq.Descendants("TransactionID").FirstOrDefault().Value);
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement restreqnns = removeAllNamespaces(RestelRequest.Root);
                XElement servrespnns = removecdata(serverResponse.Root);
                #endregion
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                log.SupplierID = 13;
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
                ex1.MethodName = "HotelBookingResponse";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            if (!serverResponse.Descendants("estado").First().Value.Equals("00"))
            {
                #region PreBooking Cancellation
                string prebookingID = RestelRequest.Descendants("localizador").First().Value;
                XDocument PreBookCancellation = new XDocument(
                                                    new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                                    new XElement("peticion",
                                                        new XElement("tipo", "3"),
                                                        new XElement("parametros",
                                                            new XElement("localizador", prebookingID),
                                                            new XElement("accion", "AI"))));
                //LogModel model1 = new LogModel
                //{
                //    CustomerID = Convert.ToInt32(travReq.Descendants("CustomerID").FirstOrDefault().Value),
                //    Supl_Id = 13,
                //    Logtype = "PreBook(Cancel)",
                //    LogtypeID = 4,
                //    TrackNo = travReq.Descendants("TransactionID").FirstOrDefault().Value
                //};
                XDocument Service202Cancel = serverRequest.RestelResponse(PreBookCancellation, travReq.Descendants("CustomerID").FirstOrDefault().Value, travReq.Descendants("TransactionID").FirstOrDefault().Value);
                if (Service202Cancel.Descendants("estado").First().Value.Equals("00"))
                    response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking Failed, Pre-Booking cancelled successfully"));
                else
                    response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking Failed, Pre-Booking cancellation unsuccessful"));

                #region Log Save
                try
                {
                    #region Removing Namespace
                    XElement restcancelnns = removeAllNamespaces(PreBookCancellation.Root);
                    XElement servrespnns = removecdata(Service202Cancel.Root);
                    #endregion
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(travReq.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                    log.SupplierID = 13;
                    log.logrequestXML = restcancelnns.ToString();
                    log.logresponseXML = servrespnns.ToString();
                    log.LogTypeID = 5;
                    log.LogType = "PreBook(Cancel)";
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "bookingResponse";
                    ex1.PageName = "RestelServices";
                    ex1.CustomerID = travReq.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = travReq.Descendants("TransactionID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                return response;
                #endregion
            }
            try
            {
                int adult = 0;
                int child = 0;
                foreach (XElement ad in travReq.Descendants("RoomPax"))
                {
                    adult = adult + Convert.ToInt32(ad.Element("Adult").Value);
                    child = child + Convert.ToInt32(ad.Element("Child").Value);
                }
                string confID = RestelRequest.Descendants("localizador").First().Value + "#" + serverResponse.Descendants("localizador_corto").First().Value;
                if (serverResponse.Descendants("estado").First().Value.Equals("00"))
                {
                    #region Response XML
                    var hbr =
                                      new XElement("Hotels",
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
                                                    new XElement("VoucherRemark"),
                                                    new XElement("TransID", travReq.Descendants("TransID").First().Value),
                                                    new XElement("ConfirmationNumber", confID),
                                                    new XElement("Status", "Success"),
                                                    Booking_Rooms(travReq, prebookinglines));
                    #endregion
                    response = new XElement("HotelBookingResponse", hbr);
                }
                else
                {
                    response = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Your booking was unsuccessful"));
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
            List<XElement> xmls = LogXMLs(Req.Descendants("TransID").First().Value, 4, 0);
            XElement prbresp = xmls.Descendants("Response").Where(x => x.Descendants("CancellationPolicy").Last().HasAttributes).First();
            string[] id = Req.Descendants("ConfirmationNumber").First().Value.Split(new char[] { ',', ':', '#' });
            string cxdate = prbresp.Descendants("CancellationPolicy").Last().Attribute("LastCancellationDate").Value;
            cxdate = cxdate.Replace('-', '/');
            try
            {
                DateTime cancellationdate = DateTime.ParseExact(cxdate, "yyyy/MM/dd", CultureInfo.InvariantCulture);
            }
            catch { }
            int flag = 0;
            //-- To check Cancellation Amount
            #region Restel Service 142
            DateTime bookingdate = DateTime.ParseExact(prbresp.Descendants("FromDate").First().Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);

            XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "13");
            string codusu = suppliercred.Descendants("codusu").FirstOrDefault().Value;
            string affiliation = suppliercred.Descendants("affiliation").FirstOrDefault().Value;

            XDocument CheckCXAmt = new XDocument(
                                       new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                       new XElement("peticion",
                                           new XElement("tipo", "142"),
                                           new XElement("parametros",
                                               new XElement("usuario", codusu),
                                               new XElement("localizador", id[0]),
                                               new XElement("idioma", "2"))));
            DateTime starttime = DateTime.Now;
            //LogModel model = new LogModel
            //{
            //    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
            //    Supl_Id = 13,
            //    Logtype = "Cancel(Price Check)",
            //    LogtypeID = 6,
            //    TrackNo = Req.Descendants("TransactionID").FirstOrDefault().Value
            //};
            XDocument CancAmt = serverRequest.RestelResponse(CheckCXAmt, Req.Descendants("CustomerID").FirstOrDefault().Value, Req.Descendants("TransID").FirstOrDefault().Value);
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement RestelRNS = removeAllNamespaces(CheckCXAmt.Root);
                XElement servresp = removecdata(CancAmt.Root);
                #endregion
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = 13;
                log.logrequestXML = RestelRNS.ToString();
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
                ex1.MethodName = "BookingCancel";
                ex1.PageName = "RestelServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            if (CancAmt.Descendants("error").Any())
            {
                #region ErrorTxt
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
                                                          new XElement("HotelCancellationResponse",
                                                              new XElement("ErrorTxt", CancAmt.Descendants("error").First().Descendants("descripcion").First().Value)))));
                return cancelledBooking;
                #endregion
            }
            foreach (XElement policy in CancAmt.Descendants("politicaCanc"))
            {
                int daysbefore = Convert.ToInt32(policy.Descendants("dias_antelacion").First().Value);
                bookingdate.AddDays(-daysbefore);
                if (bookingdate < DateTime.Now)
                {
                    int nightsCharged = Convert.ToInt32(policy.Descendants("noches_gasto").First().Value);
                    int percentCharged = Convert.ToInt32(policy.Descendants("estCom_gasto").First().Value);
                    if (nightsCharged > 0 || percentCharged > 0)
                        flag++;
                }
            }
            #endregion

            if (flag == 0)
            {
                try
                {
                    #region RequestXML
                    XDocument BookingCancellation = new XDocument(
                                                        new XDeclaration("1.0", "ISO-8859-1", "yes"),
                                                        new XElement("peticion",
                                                            new XElement("tipo", "401"),
                                                            new XElement("parametros",
                                                                new XElement("localizador_largo", id[0]),
                                                                new XElement("localizador_corto", id[1]))));
                    #endregion
                    response = CancellationResponse(BookingCancellation, Req);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "BookingCancel";
                    ex1.PageName = "RestelServices";
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
            else
            {
                #region Cancellation with Charges
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
                                                           new XElement("HotelCancellationResponse",
                                                               new XElement("ErrorTxt", "Please contact admin for cancellation of bookings that incur cancellation charges")))));
                return cancelledBooking;
                #endregion
            }

        }
        #region Response
        public XElement CancellationResponse(XDocument BookingCancel, XElement Req)
        {
            DateTime starttime = DateTime.Now;
            //LogModel model = new LogModel
            //{
            //    CustomerID = Convert.ToInt32(Req.Descendants("CustomerID").FirstOrDefault().Value),
            //    Supl_Id = 13,
            //    Logtype = "Cancel",
            //    LogtypeID = 6,
            //    TrackNo = Req.Descendants("TransactionID").FirstOrDefault().Value
            //};
            serverResponse = serverRequest.RestelResponse(BookingCancel, Req.Descendants("CustomerID").FirstOrDefault().Value, Req.Descendants("TransID").FirstOrDefault().Value);
            #region Log Save
            try
            {
                #region Removing Namespace
                XElement restcancelnns = removeAllNamespaces(BookingCancel.Root);
                XElement servrespnns = removecdata(serverResponse.Root);
                #endregion
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = Req.Descendants("TransID").FirstOrDefault().Value;
                log.SupplierID = 13;
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
                ex1.PageName = "RestelServices";
                ex1.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = Req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            #endregion
            XElement response = new XElement("HotelCancellationResponse");
            string status = null;
            if (serverResponse.Descendants("estado").First().Value.Equals("00"))
            {
                status = "Success";
                var cancel = new XElement("Room",
                                     new XElement("Cancellation",
                                         new XElement("Amount", "0.00"),
                                         new XElement("Status", status)));
                response.Add(new XElement("Rooms", cancel));
            }
            else
            {
                status = "Failed";
                #region Error Text
                response.Add(new XElement("ErrorTxt", "The cancellation has not been done"));
                #endregion
            }
            return response;
        }
        #endregion
        #endregion



        #region Extra Restel Services
        #region Service 12: Voucher Details
        //public XElement voucherService(XDocument req)
        //{
        //    XDocument requestVoucher = new XDocument(
        //                                    new XDeclaration("1.0", "ISO-8859-1", "yes"),
        //                                    new XElement("peticion",
        //                                        new XElement("tipo", "12"),
        //                                        new XElement("nombre", "Voucher request"),
        //                                        new XElement("agencia", "HOTUSA"),
        //                                        new XElement("parametros",
        //                                            new XElement("idioma", "2"),
        //                                            new XElement("localizador", req.Descendants("localizador").First().Value))));
        //    XDocument Voucher = serverRequest.RestelResponse(requestVoucher, req.Descendants("CustomerID").FirstOrDefault().Value);
        //    return Voucher.Root;
        //}
        #endregion
        #endregion



        #region Common Functions
        #region Rooms for Response
        public XElement Rooms(XElement RestelRooms, XElement travReq)
        {
            int count = 1;
            int cnt = 1;
            string child = null;
            foreach (XElement pax in travReq.Descendants("RoomPax"))
            {

                if (pax.Descendants("ChildAge").Any())
                {
                    int counter = 0;
                    foreach (XElement ages in pax.Descendants("ChildAge"))
                    {
                        if (Convert.ToInt32(ages.Value) > 2)
                            counter++;
                    }
                    child = Convert.ToString(counter);
                    if (Convert.ToInt32(pax.Element("Child").Value) != counter)
                        pax.Add(new XElement("NewChildCount", Convert.ToString(counter)));
                }
                else
                    child = pax.Descendants("Child").First().Value;
            }
            XElement response = new XElement("Rooms");
            var occupancy = travReq.Descendants("RoomPax");
            foreach (var pax in occupancy)
            {
                string occ = pax.Element("Adult").Value + "-" + pax.Element("Child").Value;
                pax.Add(new XElement("paxes", occ));
            }
            foreach (XElement oc in RestelRooms.Descendants("pax"))
            {
                string paxes = oc.Attribute("cod").Value;
                foreach (var pax in occupancy.Where(x => x.Element("paxes").Value.Equals(paxes)))
                {

                    var roomtypes = from room in RestelRooms.Descendants("hab")
                                    select
                                    new XElement("RoomTypes",
                                        new XAttribute("TotalRate", ""),
                                        new XAttribute("Index", count++),
                                        RoomTag(room, pax));
                    response.Add(roomtypes);
                }
            }
            return response;
        }
        public XElement groupedRooms(XElement RestelRooms, XElement travReq, string Hotelcode, string currency, string dmcval)
        {
            int count = 1;
            int index = 1;
            string child = null;
            foreach (XElement pax in travReq.Descendants("RoomPax"))
            {
                child = pax.Descendants("Child").First().Value;
            }
            XElement response = new XElement("Rooms");

            XElement retRooms = new XElement("Rooms");
            int roomcount = travReq.Descendants("RoomPax").Count();
            #region Room Count = 1
            if (roomcount == 1)
            {
                var occupancy = travReq.Descendants("RoomPax");

                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("1"));

                foreach (var rm1 in query1)
                {

                    if (rm1.HasElements)
                    {

                        response.Add(new XElement("RoomTypes",
                                              new XAttribute("TotalRate", rm1.Attribute("TotalRoomRate").Value),
                                              new XAttribute("Index", Convert.ToString(index++)), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmcval), new XAttribute("CUID", customerid), rm1));
                    }

                }


                #endregion
            }
            #endregion
            #region Room Count = 2
            else if (roomcount == 2)
            {
                var occupancy = travReq.Descendants("RoomPax");
                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        List<XElement> mpc = new List<XElement>();

                        mpc.Add(rm1);
                        mpc.Add(rm2);


                        int numberofrooms = 0;
                        foreach (XElement x in mpc)
                        {
                            int ind = x.Attribute("ID").Value.IndexOf('#');
                            numberofrooms = numberofrooms + Convert.ToInt32(x.Attribute("ID").Value.Substring(ind + 1, 1));
                        }
                        var splitrooms = from room in mpc
                                         group room by room.Attribute("MealPlanCode").Value;

                        foreach (var group1 in splitrooms)
                        {
                            if (group1.Count() == roomcount)
                            {
                                var groupGrouping = from grp in group1
                                                    group grp by grp.Attribute("ID").Value;
                                List<XElement> groupedRoomList = new List<XElement>();
                                double total = 0.0;
                                int grouproomcount = 0;
                                foreach (var group2 in groupGrouping)
                                {
                                    int ind = group2.Key.IndexOf('#');
                                    grouproomcount += Convert.ToInt32(group2.Key.Substring(ind + 1, 1));
                                }
                                foreach (var room in group1)
                                {
                                    if (room.HasElements)
                                    {
                                        total = total + Convert.ToDouble(room.Attribute("TotalRoomRate").Value);
                                        groupedRoomList.Add(room);
                                    }
                                }
                                if (groupedRoomList.Count == roomcount && grouproomcount == roomcount)
                                {
                                    response.Add(new XElement("RoomTypes",
                                                      new XAttribute("TotalRate", Convert.ToString(total)),
                                                      new XAttribute("Index", Convert.ToString(index++)), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmcval), new XAttribute("CUID", customerid), groupedRoomList));
                                }
                            }
                        }

                    }
                }
                #endregion
            }
            #endregion
            #region Room Count = 3
            else if (roomcount == 3)
            {
                var occupancy = travReq.Descendants("RoomPax");
                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        var query3 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("3"));
                        foreach (var rm3 in query3)
                        {
                            List<XElement> mpc = new List<XElement>();

                            mpc.Add(rm1);
                            mpc.Add(rm2);
                            mpc.Add(rm3);
                            int numberofrooms = 0;
                            List<string> roomcategories = new List<string>();
                            var splitrooms = from room in mpc
                                             group room by room.Attribute("MealPlanCode").Value;
                            foreach (var group1 in splitrooms)
                            {
                                if (group1.Count() == roomcount)
                                {
                                    var groupGrouping = from grp in group1
                                                        group grp by grp.Attribute("ID").Value;

                                    List<XElement> groupedRoomList = new List<XElement>();
                                    double total = 0.0;
                                    int grouproomcount = 0;
                                    foreach (var group2 in groupGrouping)
                                    {
                                        int ind = group2.Key.IndexOf('#');
                                        grouproomcount = grouproomcount + Convert.ToInt32(group2.Key.Substring(ind + 1, 1));
                                    }
                                    foreach (var room in group1)
                                    {

                                        if (room.HasElements)
                                        {
                                            total = total + Convert.ToDouble(room.Attribute("TotalRoomRate").Value);
                                            groupedRoomList.Add(room);
                                        }
                                    }

                                    if (groupedRoomList.Count == roomcount && grouproomcount == roomcount)
                                    {
                                        response.Add(new XElement("RoomTypes",
                                                          new XAttribute("TotalRate", Convert.ToString(total)),
                                                          new XAttribute("Index", Convert.ToString(index++)), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmcval), new XAttribute("CUID", customerid), groupedRoomList));
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion
            #region Room Count = 4
            else if (roomcount == 4)
            {
                var occupancy = travReq.Descendants("RoomPax");
                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        var query3 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("3"));
                        foreach (var rm3 in query3)
                        {
                            var query4 = retRooms.Descendants("Room").Where(x => x.Attribute("OccupancyID").Value.Equals("4"));
                            foreach (var rm4 in query4)
                            {
                                List<XElement> mpc = new List<XElement>();
                                mpc.Add(rm1);
                                mpc.Add(rm2);
                                mpc.Add(rm3);
                                mpc.Add(rm4);
                                List<string> roomtypecheck = new List<string>();
                                List<string> occupancyTypes = new List<string>();
                                foreach (XElement p in travReq.Descendants("RoomPax"))
                                {
                                    occupancyTypes.Add(p.Element("paxes").Value);
                                }
                                foreach (XElement r in mpc)
                                {
                                    roomtypecheck.Add(r.Attribute("ID").Value.Substring(0, 2));
                                }
                                int numberofrooms = roomtypecheck.Distinct().Count();
                                int numberofoccupancies = occupancyTypes.Distinct().Count();
                                //foreach(XElement x in mpc)
                                //{
                                //    int ind = x.Attribute("ID").Value.IndexOf('#');
                                //    numberofrooms = numberofrooms +Convert.ToInt32(x.Attribute("ID").Value.Substring(ind+1, 1));
                                //}
                                var splitrooms = from room in mpc
                                                 group room by room.Attribute("MealPlanCode").Value;
                                if (splitrooms.Count() < roomcount)
                                {
                                    foreach (var group1 in splitrooms)
                                    {
                                        var groupGrouping = from grp in group1
                                                            group grp by grp.Attribute("ID").Value;
                                        List<XElement> groupedRoomList = new List<XElement>();
                                        double total = 0.0;
                                        int grouproomcount = 0;
                                        foreach (var group2 in groupGrouping)
                                        {
                                            int ind = group2.Key.IndexOf('#');
                                            grouproomcount += Convert.ToInt32(group2.Key.Substring(ind + 1, 1));
                                        }
                                        foreach (var room in group1)
                                        {
                                            if (room.HasElements)
                                            {
                                                total = total + Convert.ToDouble(room.Attribute("TotalRoomRate").Value);
                                                groupedRoomList.Add(room);
                                            }
                                        }
                                        if (groupedRoomList.Count == roomcount && grouproomcount == roomcount)
                                        {
                                            response.Add(new XElement("RoomTypes",
                                                              new XAttribute("TotalRate", Convert.ToString(total)),
                                                              new XAttribute("Index", Convert.ToString(index++)), new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), new XAttribute("DMCType", dmcval), new XAttribute("CUID", customerid), groupedRoomList));
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

            foreach (XElement r in response.Descendants("RoomTypes"))
            {
                int roomseq = 1;
                foreach (XElement Rm in r.Descendants("Room"))
                    Rm.Attribute("RoomSeq").SetValue(Convert.ToString(roomseq++));
            }
            return response;
        }
        #region Room Tag
        public List<XElement> RoomTag(XElement RestelRoom, XElement occupancy)
        {
            int count = 1;
            List<XElement> roomlist = new List<XElement>();
            foreach (var hab in RestelRoom.Descendants("hab"))
            {
                string RoomType = hab.Attribute("desc").Value;
                foreach (XElement room in hab.Descendants("reg"))
                {
                    try
                    {
                        string[] linvalues = getlinevalues(room.Descendants("lin").First());
                        var lines = room.Descendants("lin");
                        string forBooking = string.Empty;
                        //for (int a = 0; a < linvalues.Length;a++ )
                        //{
                        //    if (a == 1)
                        //        forBooking = forBooking + "1#";
                        //    else
                        //        forBooking = forBooking + linvalues[a] + "#";
                        //}
                        string Roomprice = null;
                        int number = Convert.ToInt32(linvalues[1]);
                        if (number > 0)
                        {
                            double rp = Convert.ToDouble(linvalues[3]);
                            Roomprice = Convert.ToString(rp / number);
                        }
                        else
                        {
                            Roomprice = linvalues[3];
                        }
                        string totalroomrate = Convert.ToString(Convert.ToDouble(room.Attribute("prr").Value) / number);
                        roomlist.Add(new XElement("Room",
                                           new XAttribute("ID", room.Descendants("lin").First().Value),
                                              new XAttribute("SuppliersID", supplierid),
                                              new XAttribute("RoomSeq", ""),
                                              new XAttribute("SessionID", ""),
                                              new XAttribute("RoomType", RoomType),
                                              new XAttribute("OccupancyID", occupancy.Descendants("id").First().Value),
                                              new XAttribute("OccupancyName", ""),
                                              MealPlanDetails(room.Attribute("cod").Value),
                                              new XAttribute("MealPlanPrice", ""),
                                              new XAttribute("PerNightRoomRate", Roomprice),
                                              new XAttribute("TotalRoomRate", totalroomrate),                     //room.Attribute("prr").Value
                                              new XAttribute("CancellationDate", ""),
                                              new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", room.Attribute("esr").Value.Equals("OK") ? "true" : "false"),
                                              new XElement("RequestID"),
                                              new XElement("Offers"),
                                              new XElement("PromotionList",
                                              new XElement("Promotions")),
                                              new XElement("CancellationPolicy"),
                                              new XElement("Amenities",
                                                  new XElement("Amenity")),
                                              new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                              new XElement("Supplements"),
                                              pb(room),
                                              new XElement("AdultNum", occupancy.Element("Adult").Value),
                                              new XElement("ChildNum", occupancy.Element("Child").Value)));
                    }
                    catch { }

                }
            }

            return roomlist;
        }
        #endregion
        #region Price breakup
        public XElement pb(XElement reg)
        {
            XElement response = new XElement("PriceBreakups");
            int count = 1;
            foreach (XElement lin in reg.Descendants("lin"))
            {
                string[] linvalues = getlinevalues(lin);
                int number = Convert.ToInt32(linvalues[1]);
                double price = Convert.ToDouble(linvalues[3]) / number;
                response.Add(new XElement("Price",
                                 new XAttribute("Night", count++),
                                 new XAttribute("PriceValue", Convert.ToString(price))));
            }
            return response;
        }
        #endregion
        #endregion
        #region Rooms for HotelSearch
        public List<XElement> SearchRooms(XElement travReq)
        {
            int RC = travReq.Descendants("RoomPax").Count();
            List<XElement> rooms = new List<XElement>();
            List<XElement> occupancy = travReq.Descendants("RoomPax").ToList();

            foreach (XElement pax in occupancy)
            {
                string child = null;
                string adult = null;
                child = pax.Descendants("Child").First().Value;
                pax.Add(new XElement("paxes", pax.Descendants("Adult").First().Value + "-" + child));
            }
            var paxgroup = from pax in occupancy
                           group pax by pax.Descendants("paxes").First().Value;
            int count = 1;
            foreach (var group in paxgroup)
            {
                //List<XElement> childages = new List<XElement>();
                string childrenage = null;
                foreach (var ca in group.Descendants("ChildAge"))
                {
                    if (string.IsNullOrEmpty(childrenage))
                        childrenage = ca.Value;
                    else
                        childrenage = childrenage + "," + ca.Value;
                }

                rooms.Add(new XElement("numhab" + Convert.ToString(count), Convert.ToString(group.Count())));
                rooms.Add(new XElement("paxes" + Convert.ToString(count), group.Key.ToString()));
                rooms.Add(new XElement("edades" + Convert.ToString(count), Convert.ToString(childrenage)));
                count++;

            }

            return rooms;
        }
        #endregion
        #region Rooms for Booking Response
        public XElement Booking_Rooms(XElement travyobcr, List<XElement> RestelLines)
        {
            int count = 0;
            XElement guests = new XElement("GuestDetails");
            List<string> linesvalues = new List<string>();
            foreach (XElement lin in RestelLines)
                linesvalues.Add(lin.Value);
            XElement bookedrooms = new XElement("PassengerDetail");
            foreach (XElement Room in travyobcr.Descendants("Room"))
            {

                string[] linvalues = getlinevalues(new XElement("lin", Room.Attribute("RoomTypeID").Value));


                var RoomsGettingbooked =
                                                    new XElement("Room",
                                                        new XAttribute("ID", count++),
                                                        new XAttribute("RoomType", Room.Attribute("RoomType").Value),
                                                        new XAttribute("ServiceID", ""),
                                                        new XAttribute("MealPlanID", Room.Attribute("MealPlanID").Value),
                                                        new XAttribute("MealPlanName", ""),
                                                        new XAttribute("MealPlanCode", ""),
                                                        new XAttribute("MealPlanPrice", ""),
                                                        new XAttribute("PerNightRoomRate", linvalues[3]),
                                                        new XAttribute("RoomStatus", linvalues[6].Equals("OK") ? "true" : "false"),
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
        #region Rooms for pre-booking response
        public List<XElement> prebookrooms(XElement travReq)
        {
            List<XElement> RoomTags = new List<XElement>();
            XElement response = new XElement("RoomTypes",
                                    new XAttribute("TotalRate", travReq.Descendants("RoomTypes").First().Attribute("TotalRate").Value),
                                    new XAttribute("Index", "1"));
            XElement hotels = roomlist(travReq.Descendants("TransID").First().Value);
            XElement hotel = hotels.Descendants("hot")
                            .Where(x => x.Element("cod").Value.Equals(travReq.Descendants("HotelID").First().Value))
                            .First();
            foreach (XElement room in travReq.Descendants("Room"))
            {
                string[] splitforpax = room.Attribute("ID").Value.Split(new char[] { '-' });
                XElement reg = hotel.Descendants("reg")
                               .Where(x => x.Descendants("lin").First().Value.Equals(room.Attribute("ID").Value))
                               .First();
                //XElement query = room;
                //query.Descendants("RequestID").First().AddAfterSelf(new XElement("Offers"));
                //query.Descendants("Offers").First().AddAfterSelf(new XElement("PromotionList", new XElement("Promotions")));
                //query.Descendants("PromotionList").First().AddAfterSelf(new XElement("CancellationPolicy"));
                //query.Descendants("CancellationPolicy").First().AddAfterSelf(new XElement("Amenities", new XElement("Amenity")));
                //query.Descendants("Amenities").First().AddAfterSelf(new XElement("Images", new XElement("Image", new XAttribute("Path", ""))));
                //query.Descendants("Supplements").First().AddAfterSelf(pb(reg));
                //query.Descendants("PriceBreakups").First().AddAfterSelf(new XElement("AdultNum", splitforpax[0].ElementAt(splitforpax[0].Length - 1)));
                //query.Descendants("AdultNum").First().AddAfterSelf(new XElement("ChildNum", splitforpax[1].ElementAt(0)));
                XElement entry = new XElement("Room",
                                     new XAttribute("ID", room.Attribute("ID").Value),
                                     new XAttribute("SuppliersID", room.Attribute("SuppliersID").Value),
                                     new XAttribute("RoomSeq", room.Attribute("RoomSeq").Value),
                                     new XAttribute("SessionID", ""),
                                     new XAttribute("RoomType", room.Attribute("RoomType").Value),
                                     new XAttribute("OccupancyID", room.Attribute("OccupancyID").Value),
                                     new XAttribute("OccupancyName", room.Attribute("OccupancyName").Value),
                                     new XAttribute("MealPlanID", room.Attribute("MealPlanID").Value),
                                     new XAttribute("MealPlanName", room.Attribute("MealPlanName").Value),
                                     new XAttribute("MealPlanCode", room.Attribute("MealPlanCode").Value),
                                     new XAttribute("MealPlanPrice", room.Attribute("MealPlanPrice").Value),
                                     new XAttribute("PerNightRoomRate", room.Attribute("PerNightRoomRate").Value),
                                     new XAttribute("TotalRoomRate", room.Attribute("TotalRoomRate").Value),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", room.Attribute("isavailable").Value),
                                     new XElement("RequestID"),
                                     new XElement("Offers"),
                                     new XElement("PromotionList",
                                         new XElement("Promotions")),
                                     new XElement("CancellationPolicy"),
                                     new XElement("Amenities",
                                         new XElement("Amenity")),
                                     new XElement("Images",
                                         new XElement("Image",
                                             new XAttribute("Path", ""))),
                                     new XElement("Supplements"),
                                     pb(reg),
                                     new XElement("AdultNum"),
                                     new XElement("ChildNum"));

                RoomTags.Add(entry);
            }
            return RoomTags;
        }

        #region Fetch <reg> from the database

        #endregion
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
        #region Pre-Booking Room Tag
        public XElement PBRoomTag(XElement reg, string occupancy)
        {
            string[] linvalues = getlinevalues(reg.Descendants("lin").First());

            XElement response = new XElement("Room",
                               new XAttribute("ID", ""),
                                  new XAttribute("SuppliersID", ""),
                                  new XAttribute("RoomSeq", ""),
                                  new XAttribute("SessionID", ""),
                                  new XAttribute("RoomType", ""),
                                  new XAttribute("OccupancyID", ""),
                                  new XAttribute("OccupancyName", ""),
                                  new XAttribute("MealPlanID", ""),
                                  new XAttribute("MealPlanName", ""),
                                  new XAttribute("MealPlanCode", ""),
                                  new XAttribute("MealPlanPrice", ""),
                                  new XAttribute("PerNightRoomRate", ""),
                                  new XAttribute("TotalRoomRate", ""),
                                  new XAttribute("CancellationDate", ""),
                                  new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", "true"),
                                  new XElement("RequestID", ""),
                                  new XElement("Offers"),
                                  new XElement("PromotionList",
                                  new XElement("Promotions")),
                                  new XElement("CancellationPolicy"),
                                  new XElement("Amenities",
                                      new XElement("Amenity")),
                                  new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                  new XElement("Supplements"),
                                  pb(reg),
                                  new XElement("AdultNum", occupancy.Substring(0, 1)),
                                  new XElement("ChildNum", occupancy.Substring(2, 1)));
            return response;
        }
        #endregion
        #region Break hotel list into chunks: 200
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
        #region Adult Count
        public string adultsNumber(XElement voucher)
        {
            int adults = 0;
            foreach (XElement adcount in voucher.Descendants("lin_adultos"))
                adults = adults + Convert.ToInt32(adcount.Value);
            return Convert.ToString(adults);
        }
        #endregion
        #region Child Count
        public string ChildNumber(XElement voucher)
        {
            int child = 0;
            foreach (XElement cdcount in voucher.Descendants("lin_ninos"))
                child = child + Convert.ToInt32(cdcount.Value);
            return Convert.ToString(child);
        }
        #endregion
        #region Meal Plan Name and Code and ID
        public List<XAttribute> MealPlanDetails(string reg)
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
                case "OB":
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
                case "BB":
                    mpName = "Bed & Breakfast";
                    mpID = "2";
                    mpCode = "BB";
                    break;
                case "FB":
                    mpName = "Full Board";
                    mpID = "4";
                    mpCode = "FB";
                    break;
                case "HB":
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
                default:
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
        #region Reform date to MM/dd/yyyy
        public string reformdate(string date)
        {
            DateTime dt = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
            string dd = dt.ToString("MM/dd/yyyy");
            dd = dd.Replace('-', '/');
            return dd;
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
        #region RoomList
        public XElement roomlist(string trackNumber)
        {
            DataTable dt = rlac.GetMiki_RoomList(trackNumber);
            XElement finalresponse = new XElement("test");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (!string.IsNullOrEmpty(dt.Rows[i]["logresponseXML"].ToString()))
                {
                    XElement response = XElement.Parse(dt.Rows[i]["logresponseXML"].ToString());
                    finalresponse.Add(response);
                }
            }
            return finalresponse;

        }
        #endregion
        #region Minimum Rate
        public string MinRate(XElement rooms, XElement Req)
        {

            double minprice = MinPriceGrouping(rooms, Req);
            string rate = null;
            if (minprice == double.MaxValue)
                rate = "0.0";
            else
                rate = Convert.ToString(minprice);
            return rate;

        }
        #region Min Price Calculation
        public double MinPriceGrouping(XElement RestelRooms, XElement travReq)
        {
            int count = 1;


            double response = 0.0;

            XElement retRooms = new XElement("Rooms");
            int roomcount = travReq.Descendants("RoomPax").Count();
            #region Room Count = 1
            if (roomcount == 1)
            {
                var occupancy = travReq.Descendants("RoomPax");
                string child = null;
                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag1(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                double minimum = double.MaxValue;
                foreach (var rm1 in query1)
                {
                    double check = Convert.ToDouble(rm1.Attribute("RoomPrice").Value);
                    if (check < minimum)
                        minimum = check;
                }
                #endregion
                response = minimum;
                removetags(travReq);
            }
            #endregion
            #region Room Count = 2
            else if (roomcount == 2)
            {
                var occupancy = travReq.Descendants("RoomPax");
                string child = null;
                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag1(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                double minimum = double.MaxValue;
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        List<XElement> mpc = new List<XElement>();

                        mpc.Add(rm1);
                        mpc.Add(rm2);



                        var splitrooms = from room in mpc
                                         group room by room.Attribute("MealPlanCode").Value;
                        foreach (var group in splitrooms)
                        {
                            List<XElement> groupedRoomList = new List<XElement>();
                            double total = 0.0;
                            foreach (var room in group)
                            {
                                if (room.HasAttributes)
                                {
                                    total = total + Convert.ToDouble(room.Attribute("RoomPrice").Value);
                                    groupedRoomList.Add(room);
                                }
                            }
                            if (groupedRoomList.Count == roomcount)
                            {
                                if (total < minimum)
                                    minimum = total;
                            }
                        }



                    }
                }
                #endregion
                response = minimum;
                removetags(travReq);
            }
            #endregion
            #region Room Count = 3
            else if (roomcount == 3)
            {
                var occupancy = travReq.Descendants("RoomPax");
                string child = null;
                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag1(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                double minimum = double.MaxValue;
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        var query3 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("3"));
                        foreach (var rm3 in query3)
                        {
                            List<XElement> mpc = new List<XElement>();

                            mpc.Add(rm1);
                            mpc.Add(rm2);
                            mpc.Add(rm3);

                            var splitrooms = from room in mpc
                                             group room by room.Attribute("MealPlanCode").Value;
                            foreach (var group in splitrooms)
                            {
                                List<XElement> groupedRoomList = new List<XElement>();
                                double total = 0.0;
                                foreach (var room in group)
                                {

                                    if (room.HasAttributes)
                                    {
                                        total = total + Convert.ToDouble(room.Attribute("RoomPrice").Value);
                                        groupedRoomList.Add(room);
                                    }
                                }
                                if (groupedRoomList.Count == roomcount)
                                {
                                    if (total < minimum)
                                        minimum = total;
                                }
                            }

                        }
                    }
                }
                #endregion
                response = minimum;
                removetags(travReq);
            }
            #endregion
            #region Room Count = 4
            else if (roomcount == 4)
            {
                var occupancy = travReq.Descendants("RoomPax");
                string child = null;
                foreach (var pax in occupancy)
                {
                    if (pax.Descendants("NewChildCount").Any())
                        child = pax.Descendants("NewChildCount").First().Value;
                    else
                        child = pax.Descendants("Child").First().Value;
                    string occ = pax.Element("Adult").Value + "-" + child;
                    pax.Add(new XElement("paxes", occ));
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    retRooms.Add(RoomTag1(RestelRooms.Descendants("pax").Where(x => x.Attribute("cod").Value.Equals(occ)).First(), pax));
                }
                #region RoomGrouping
                double minimum = double.MaxValue;
                var query1 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("1"));
                foreach (var rm1 in query1)
                {
                    var query2 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("2"));
                    foreach (var rm2 in query2)
                    {
                        var query3 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("3"));
                        foreach (var rm3 in query3)
                        {
                            var query4 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("4"));
                            foreach (var rm4 in query4)
                            {
                                List<XElement> mpc = new List<XElement>();
                                mpc.Add(rm1);
                                mpc.Add(rm2);
                                mpc.Add(rm3);
                                mpc.Add(rm4);
                                var splitrooms = from room in mpc
                                                 group room by room.Attribute("MealPlanCode").Value;
                                foreach (var group in splitrooms)
                                {
                                    List<XElement> groupedRoomList = new List<XElement>();
                                    double total = 0.0;
                                    foreach (var room in group)
                                    {
                                        if (room.HasAttributes)
                                        {
                                            total = total + Convert.ToDouble(room.Attribute("RoomPrice").Value);
                                            groupedRoomList.Add(room);
                                        }
                                    }
                                    if (groupedRoomList.Count == roomcount)
                                    {
                                        if (total < minimum)
                                            minimum = total;
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion
                response = minimum;
                removetags(travReq);
            }
            #endregion

            return response;
        }
        public List<XElement> RoomTag1(XElement RestelRoom, XElement occupancy)
        {
            List<XElement> roomlist = new List<XElement>();
            try
            {
                foreach (var hab in RestelRoom.Descendants("hab"))
                {
                    string RoomType = hab.Attribute("desc").Value;
                    foreach (XElement room in hab.Descendants("reg"))
                    {
                        string[] splitline = getlinevalues(room.Descendants("lin").FirstOrDefault());
                        double roomPrice = Convert.ToDouble(room.Attribute("prr").Value) / Convert.ToInt32(splitline[1]);
                        roomlist.Add(new XElement("Room",
                                          new XAttribute("Occupancy", occupancy.Descendants("id").First().Value),
                                          MealPlanDetails(room.Attribute("cod").Value),
                                          new XAttribute("RoomPrice", roomPrice.ToString())));
                    }
                }
            }
            catch { }
            return roomlist;
        }
        #endregion
        #endregion
        #region Line Tags
        public XElement getlinetags(string firstlin, string tracknumber, string hotelID)
        {
            DataTable dt = rlac.GetMiki_RoomList(tracknumber);
            XElement finalresponse = new XElement("test");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (!string.IsNullOrEmpty(dt.Rows[i]["logresponseXML"].ToString()))
                {
                    XElement response = XElement.Parse(dt.Rows[i]["logresponseXML"].ToString());
                    finalresponse.Add(response);
                }
            }
            XElement hotel = finalresponse.Descendants("hot")
                             .Where(x => x.Element("cod").Value.Equals(hotelID))
                             .First();
            //var test = hotel.Descendants("lin");
            //foreach(XElement lin in hotel.Descendants("lin"))
            //{
            //    string newlinevalue=string.Empty;
            //    string[] linvalues = getlinevalues(lin);
            //    for(int i=0; i<linvalues.Length;i++)
            //    {
            //        if (i == 1)
            //            newlinevalue = newlinevalue + "1#";
            //        else
            //            newlinevalue = newlinevalue + linvalues[i] + "#";
            //    }
            //    lin.SetValue(newlinevalue);
            //}
            var lines = from reg in hotel.Descendants("reg")
                        where reg.Descendants("lin").First().Value.Equals(firstlin)
                        select reg;

            return lines.First();
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
            LogTable = rlac.GetLog(trackID, logtypeID, SupplierID);
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
        public List<string> HotelCodes(string CityCode,string HotelId, string HotelName)
        { 
            DataTable hotelsList = rlac.GetHotelsList(CityCode, HotelId,HotelName);
            List<string> NewResponse = new List<string>();
            for (int i = 0; i < hotelsList.Rows.Count; i++)
            {
                NewResponse.Add(hotelsList.Rows[i]["HotelID"].ToString() + "#");
            }
            return NewResponse.Distinct().ToList();
        }
        #endregion
        #region Facilities
        public XElement HotelFacilities(string CityID)
        {
            XElement response = new XElement("Test");

            DataTable Facilities = rlac.GetRestelFacilities(CityID);
            if (Facilities != null && Facilities.Rows.Count != 0)
            {
                var facil = Facilities.Columns.Cast<DataColumn>()
                                                   .Select(x => x.ColumnName);
                string[] facilityArray = facil.ToArray();
                for (int j = 0; j < Facilities.Rows.Count; j++)
                {
                    List<XElement> TempData = new List<XElement>();
                    DataRow fac = Facilities.Rows[j];
                    int count = fac.ItemArray.Count();
                    for (int i = 1; i < count - 1; i++)
                        TempData.Add(new XElement(facilityArray[i], fac[i].ToString()));
                    List<XElement> TempFacilities = new List<XElement>();
                    foreach (XElement service in TempData)
                    {
                        if (service.Value.Equals("true"))
                            TempFacilities.Add(new XElement("Facility", service.Name));
                    }
                    response.Add(new XElement("Facilities", new XAttribute("HotelID", fac[0]), TempFacilities));
                }
            }
            //else
            //{
            //    response = new XElement("Facilities", new XElement("Facility", "No Facility Available"));
            //}
            return response;
        }
        #endregion
        #region Country Mapping
        public string CountryMapping(string TravCountryID)
        {
            //XElement mapElement = citymapping.Descendants("d0").Where(x => x.Descendants("CountryID").First().Value.Equals(TravCountryID)).First();
            //string RestelCountryID = mapElement.Descendants("SupplierCountryID").First().Value;
            //return RestelCountryID;
            return null;
        }
        #endregion

        public string[] getlinevalues(XElement abc)
        {
            //var lines = abc.Descendants("res").Elements("lin");
            //foreach(var lin in lines)
            //{ 
            string line = abc.Value;
            string[] linevalues = line.Split(new char[] { '#' });
            return linevalues;

        }
        #endregion
        public static void WriteToLogFile(string logtxt)
        {
            string _filePath = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
            //string path = Path.Combine(_filePath, @"log.txt");
            string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(logtxt);
                writer.Close();
            }

        }
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