using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Darina;

namespace TravillioXMLOutService.Supplier.Darina
{
    public class dr_Hotelsearch : IDisposable
    {
        int sup_cutime = 100000;
        string availDarina = string.Empty;
        string hoteldetailDarina = string.Empty;
        string dmc = string.Empty;
        string customerid = string.Empty;
        XElement reqTravillio;
        XElement doccity = null;
        XElement docccountry = null;
        #region darina availability
        public List<XElement> darinaavailcombined(XElement req, XElement doccurrency, XElement docmealplan, XElement dococcupancy, string custID, string custName)
        {
            dmc = custName;
            customerid = custID;
            List<XElement> results = null;
            try
            {
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                }
                catch { }
                #endregion
                reqTravillio = req;
                doccity = dr_staticdata.drn_doccity(); 
                docccountry = dr_staticdata.drn_doccountry();
                IEnumerable<XElement> doccityd = doccity.Descendants("d0").Where(x => x.Descendants("LetterCode").FirstOrDefault().Value == req.Descendants("CityCode").FirstOrDefault().Value);
                if (doccityd.FirstOrDefault() != null)
                {
                    Thread dth = new Thread(new ThreadStart(gethotelavaillistDarina));
                    Thread dth1 = new Thread(new ThreadStart(gethoteldetailDarina));
                    dth.Start();
                    dth1.Start();
                    dth.Join(sup_cutime);
                    dth1.Join(sup_cutime);
                    dth.Abort();
                    dth1.Abort();
                    if (availDarina != "" && hoteldetailDarina != "")
                    {
                        try
                        {
                            XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "1");
                            string currencyid = suppliercred.Descendants("currencyid").FirstOrDefault().Value;
                            XElement doc = XElement.Parse(availDarina);
                            List<XElement> htlist = doc.Descendants("D").ToList();
                            XElement dochtd = XElement.Parse(hoteldetailDarina);
                            IEnumerable<XElement> htlistdetail = dochtd.Descendants("D").ToList();
                            IEnumerable<XElement> currencycode = doccurrency.Descendants("d0").Where(x => x.Descendants("CurrencyID").Single().Value == currencyid);
                            results = GetHotelList(htlist, htlistdetail, docmealplan, dococcupancy, currencycode);
                            return results;
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    //try
                    //{
                    //    APILogDetail log = new APILogDetail();
                    //    log.customerID = Convert.ToInt64(customerid);
                    //    log.TrackNumber = req.Descendants("TransID").Single().Value;
                    //    log.SupplierID = 1;
                    //    log.LogTypeID = 1;
                    //    log.LogType = "Search";
                    //    log.logrequestXML = req.ToString();
                    //    log.logresponseXML = "<xml>No city available</xml>";
                    //    SaveAPILog savelog = new SaveAPILog();
                    //    savelog.SaveAPILogs(log);
                    //    return null;
                    //}
                    //catch { return null; }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        #endregion
        #region Hotel Availability Methods
        public void gethotelavaillistDarina()
        {
            #region Darina Hotel Availability
            try
            {
                string flag = "htlist";
                //DarinaCredentials _credential = new DarinaCredentials();
                //var _url = _credential.APIURL;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "1");
                var _url = suppliercred.Descendants("APIURL").FirstOrDefault().Value;
                var _action = suppliercred.Descendants("searchActionURL").FirstOrDefault().Value;
                //var _action = "http://travelcontrol.softexsw.us/CheckAvailability";
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
                //DarinaCredentials _credential = new DarinaCredentials();
                //string _url = _credential.APIURL;
                //string _action = "http://travelcontrol.softexsw.us/GetBasicData_Properties_WithFullDetails";
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "1");
                var _url = suppliercred.Descendants("APIURL").FirstOrDefault().Value;
                var _action = suppliercred.Descendants("propertyActionURL").FirstOrDefault().Value;
                hoteldetailDarina = CallWebService(reqTravillio, _url, _action, flag);
            }
            catch (Exception ex)
            {
                hoteldetailDarina = "";
            }
            #endregion
        }
        #endregion
        #region Darina Holidays
        #region Darina's Hotel Listing
        private List<XElement> GetHotelList(List<XElement> htlist, IEnumerable<XElement> htlistdetail, XElement docmealplan, XElement dococcupancy, IEnumerable<XElement> currencycode)
        {
            #region Darina Hotel List
            List<XElement> hotellst = new List<XElement>();
            string xmlouttype = string.Empty;
            try
            {
                if (dmc == "Darina")
                {
                    xmlouttype = "false";
                }
                else
                { xmlouttype = "true"; }
            }
            catch { }
            try
            {
                Int32 length = htlist.Descendants("Hotels").Count();
                List<XElement> hotellist = htlist.Descendants("Hotels").ToList();
                try
                {
                    //Parallel.For(0, length, i =>
                    for (int i = 0; i < length; i++)
                    {
                        try
                        {
                            IEnumerable<XElement> hoteldetails = htlistdetail.Descendants("d0").Where(x => x.Descendants("HotelID").Single().Value == hotellist[i].Element("HotelID").Value);
                            List<XElement> hotelfacilities = htlistdetail.Descendants("HFac").Where(x => x.Descendants("HotelID").Single().Value == hotellist[i].Element("HotelID").Value).ToList();
                            List<XElement> roomlist = htlist.Descendants("AccRates").Where(x => x.Descendants("Hotel").Single().Value == hotellist[i].Element("HotelID").Value).ToList();
                            string roomrate = string.Empty;
                            try
                            {
                                roomrate = Convert.ToString(Convert.ToDecimal(roomlist.Descendants("RatePerStay").Min(x => Convert.ToDecimal(x.Value))));
                            }
                            catch
                            {
                                roomrate = roomlist.Descendants("RatePerStay").FirstOrDefault().Value;
                            }
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
                                                   new XElement("MinRate", Convert.ToString(roomrate))
                                                   , new XElement("HotelImgSmall", Convert.ToString(hotellist[i].Element("SmallImgLink").Value)),
                                                   new XElement("HotelImgLarge", Convert.ToString(hotellist[i].Element("LargeImgLink").Value)),
                                                   new XElement("MapLink", map),
                                                   new XElement("Longitude", null),
                                                   new XElement("Latitude", null),
                                                    new XElement("xmloutcustid", customerid),
                                                       new XElement("xmlouttype", xmlouttype),
                                                   new XElement("DMC", dmc),
                                                   new XElement("SupplierID", "1"),
                                                   new XElement("Currency", Convert.ToString(currencycode.Descendants("CurrencyName").Single().Value)),
                                                   new XElement("Offers", "")
                                                   , new XElement("Facilities", null)
                                //GetHotelFacilities(hotelfacilities))
                                                   , new XElement("Rooms", ""
                                //GetHotelRooms(htlist, roomlist, docmealplan, dococcupancy)
                                                       )
                            ));
                        }
                        catch { }
                    }
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
        public List<XElement> GetHotelRooms(List<XElement> htlist, List<XElement> roomlist, XElement docmealplan, XElement dococcupancy)
        {
            #region Darina Hotel's Room List
            List<XElement> str = new List<XElement>();
            List<XElement> roomtypes = htlist;
            DateTime fromDate = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            DateTime toDate = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            int nights = (int)(toDate - fromDate).TotalDays;
            Parallel.For(0, roomlist.Count(), i =>
            {

                List<XElement> docmealplandet = docmealplan.Descendants("d0").Where(x => x.Descendants("MealPlanID").Single().Value == roomlist[i].Descendants("MealPlan").Single().Value).ToList();
                List<XElement> dococcupancydet = dococcupancy.Descendants("d0").Where(x => x.Descendants("OccupancyID").Single().Value == roomlist[i].Descendants("Occupancy").Single().Value).ToList();
                List<XElement> room = roomtypes.Descendants("RoomTypes").Where(x => x.Descendants("RoomTypeID").Single().Value == roomlist[i].Descendants("RoomType").Single().Value).ToList();
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
                         new XAttribute("MealPlanCode", Convert.ToString(docmealplandet.Descendants("MealPlanCode").Single().Value)),
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
        private List<XElement> GetHotelFacilities(List<XElement> hotelfacilities)
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
        private List<XElement> GetRoomsPriceBreakupDarina(int nights, string pernightprice)
        {
            #region Darina Room's Price Breakups
            List<XElement> str = new List<XElement>();
            try
            {
                Parallel.For(0, nights, i =>
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pernightprice)))
                    );
                });
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }
            #endregion
        }
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

                            log.customerID = Convert.ToInt64(customerid);
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
                            ex1.CustomerID = customerid;
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
                    CustomException ex1 = new CustomException(webex);
                    ex1.MethodName = "CallWebService";
                    ex1.PageName = "TrvHotelSearch";
                    ex1.CustomerID = customerid;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return "";
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
        public XDocument CreateSoapEnvelopehtlist(XElement req)
        {
            int totalchild = Convert.ToInt32(req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value);
            string mainchildrenages = "";
            string childrenages = "";
            #region City ID / Country ID / Pax Nationality Country ID
            string cityid = "";
            string countryid = "";
            string paxcountryid = "";
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
            string currencyid = string.Empty;
            //DarinaCredentials _credential = new DarinaCredentials();
            //AccountName = _credential.AccountName;
            //UserName = _credential.UserName;
            //Password = _credential.Password;
            //AgentID = _credential.AgentID;
            //Secret = _credential.Secret;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
            currencyid = suppliercred.Descendants("currencyid").FirstOrDefault().Value;
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
                             //"<MinLandCategory>" + req.Descendants("MinStarRating").Single().Value + "</MinLandCategory>" +
                             "<MinLandCategory>0</MinLandCategory>" +
                             "<MaxLandCategory>" + req.Descendants("MaxStarRating").Single().Value + "</MaxLandCategory>" +
                             "<PropertyType>0</PropertyType>" +
                             //"<HotelName></HotelName>" +
                             "<HotelName>" + req.Descendants("HotelName").FirstOrDefault().Value.Trim() + "</HotelName>" + 
                             "<OccupancyID>0</OccupancyID>" +
                             "<AdultPax>" + req.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value + "</AdultPax>" +
                             "<ChildPax>" + req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value + "</ChildPax>" +
                                    mainchildrenages +
                             "<ExtraBedAdult>false</ExtraBedAdult>" +
                             "<ExtraBedChild>false</ExtraBedChild>" +
                             "<Nationality_CountryID>" + paxcountryid + "</Nationality_CountryID>" +
                             "<CurrencyID>" + currencyid + "</CurrencyID>" +
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
        public XDocument CreateSoapEnvelopehtdetail(XElement req)
        {
            #region City ID / Country ID / Pax Nationality Country ID
            string cityid = "";
            string countryid = "";
            string paxcountryid = "";
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
            //DarinaCredentials _credential = new DarinaCredentials();
            //AccountName = _credential.AccountName;
            //UserName = _credential.UserName;
            //Password = _credential.Password;
            //AgentID = _credential.AgentID;
            //Secret = _credential.Secret;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
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
                             //"<HotelName></HotelName>" +
                             "<HotelName>" + req.Descendants("HotelName").FirstOrDefault().Value.Trim() + "</HotelName>" + 
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