using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Darina;

namespace TravillioXMLOutService.Supplier.Darina
{
    public class dr_CXLpolicy
    {
        public XElement getCXLpolicy(XElement req)
        {
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest");
            #region Darina supplier            
            //DarinaCredentials _credential = new DarinaCredentials();
            //var _url = _credential.APIURL;
            //var _action = "http://travelcontrol.softexsw.us/CheckAvailabilityWithCancellation_NoCache_LiveCalculation";
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            var _url = suppliercred.Descendants("APIURL").FirstOrDefault().Value;
            var _action = suppliercred.Descendants("APIURL_NoCacheaction").FirstOrDefault().Value;
            string avail = CallWebService(req, _url, _action);
            if (avail != null)
            {
                #region XML OUT
                XElement doc = XElement.Parse(avail);
                int error = doc.Descendants("Hotels").Count();
                if (error > 0)
                {
                    #region XML OUT from Darina Holidays
                    List<XElement> htdetails = doc.Descendants("Hotels").ToList();
                    List<XElement> htalldetail = doc.Descendants("D").ToList();
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
                           new XElement("HotelDetailwithcancellationResponse",
                               new XElement("Hotels",
                                   new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(htdetails.Descendants("HotelID").Single().Value)),
                                           new XElement("HotelName", Convert.ToString(htdetails.Descendants("Name").Single().Value))
                                           , new XElement("HotelImgSmall", Convert.ToString(htdetails.Descendants("SmallImgLink").Single().Value)),
                                           new XElement("HotelImgLarge", Convert.ToString(htdetails.Descendants("LargeImgLink").Single().Value)),
                                           new XElement("MapLink", ""),
                                           new XElement("DMC", "Darina"),
                                           new XElement("Currency", ""),
                                           new XElement("Offers", "")
                                           , new XElement("Rooms",
                                               GetHotelRooms(htalldetail, Convert.ToString(req.Descendants("CurrencyName").Single().Value))
                                               )
                    ))
                  ))));
                    return searchdoc;
                    #endregion
                }
                else
                {
                    #region No Policy Found
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
                           new XElement("HotelDetailwithcancellationResponse",
                               new XElement("ErrorTxt", "No policy Found")
                                       )
                                   )
                  ));
                    return searchdoc;
                    #endregion
                }
                #endregion
            }
            else
            {
                #region No Policy Found
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
                       new XElement("HotelDetailwithcancellationResponse",
                           new XElement("ErrorTxt", "No policy Found")
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }
            #endregion
        }
        #region Room's Details from Darina Holidays
        private IEnumerable<XElement> GetHotelRooms(List<XElement> htlist, string CurrencyName)
        {
            #region Room's Details from Darina Holidays
            List<XElement> htrm = new List<XElement>();
            List<XElement> roomlist = htlist.Descendants("AccRates").ToList();
            List<XElement> RoomTypes = htlist.Descendants("RoomTypes").ToList();
            List<XElement> roomlistatt = htlist.Descendants("Cancellation").ToList();
            Parallel.For(0, roomlist.Count(), i =>
            {
                string lastcancellationdate = Convert.ToString(roomlistatt.Descendants("ToDate").LastOrDefault().Value);

                if (lastcancellationdate == null || lastcancellationdate == "")
                {
                    lastcancellationdate = "";
                }
                htrm.Add(new XElement("Room",
                    new XAttribute("ID", Convert.ToString(RoomTypes[0].Descendants("RoomTypeID").Single().Value)),
                    new XAttribute("RoomType", Convert.ToString(RoomTypes[0].Descendants("RoomTypeName").Single().Value)),
                    new XAttribute("PerNightRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value)),
                    new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerStay").Single().Value)),
                    new XAttribute("LastCancellationDate", lastcancellationdate),
                    new XElement("CancellationPolicies",
                         GetRoomCancellationPolicy(roomlistatt, Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value), Convert.ToString(roomlist[i].Descendants("RatePerStay").Single().Value), CurrencyName))

                    ));
            });
            return htrm;
            #endregion
        }
        #endregion
        #region Room's Cancellation Policies from Darina Holidays
        private IEnumerable<XElement> GetRoomCancellationPolicy(List<XElement> cancellationpolicy, string pernightcost, string totalamount, string CurrencyName)
        {
            #region Room's Cancellation Policies from Darina Holidays
            List<XElement> htrm = new List<XElement>();
            List<XElement> roomlist = cancellationpolicy;
            decimal TotalRoomRate = Convert.ToDecimal(totalamount);
            decimal RoomRate = Convert.ToDecimal(pernightcost);
            int freecxl = 0;
            for (int i = 0; i < roomlist.Count(); i++)
            {
                string amounttype = roomlist[i].Descendants("AmountType").Single().Value;
                string desc = roomlist[i].Descendants("Description").Single().Value;
                decimal AmountApplicable = 0;
                if (amounttype == "")
                {
                    freecxl = 1;
                    AmountApplicable = (decimal)0.0;
                }
                else if (amounttype == "Night")
                {
                    AmountApplicable = (Convert.ToDecimal(roomlist[i].Descendants("Amount").Single().Value)) * RoomRate;
                }
                else
                {
                    AmountApplicable = (TotalRoomRate * Convert.ToDecimal(roomlist[i].Descendants("Amount").Single().Value)) / 100;
                }
                if (amounttype == "")
                {
                    htrm.Add(new XElement("CancellationPolicy", "Cancellation done by or before " + Convert.ToString(roomlist[i].Descendants("ToDate").Single().Value) + "  will apply " + CurrencyName + " " + AmountApplicable + "  Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(roomlist[i].Descendants("ToDate").Single().Value))
                        , new XAttribute("ApplicableAmount", AmountApplicable)
                        , new XAttribute("NoShowPolicy", "0")));
                }
                else
                {
                    htrm.Add(new XElement("CancellationPolicy", "Cancellation done by or before " + Convert.ToString(roomlist[i].Descendants("ToDate").Single().Value) + "  will apply " + CurrencyName + " " + AmountApplicable + "  Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(roomlist[i].Descendants("FromDate").Single().Value))
                        , new XAttribute("ApplicableAmount", AmountApplicable)
                        , new XAttribute("NoShowPolicy", "0")));
                }
            }
            if (freecxl == 0)
            {
                List<XElement> grhtrm = new List<XElement>();
                grhtrm.Add(new XElement("Room", htrm));
                XElement cxlfinal = MergCxlPolicy(grhtrm, CurrencyName);
                List<XElement> cxlfinalres = cxlfinal.Descendants("CancellationPolicy").ToList();
                return cxlfinalres;
            }
            else
            {
                return htrm;
            }

            #endregion
        }
        #endregion
        #region Methods for Darina Holidays
        public string CallWebService(XElement req, string url, string action)
        {
            XDocument soapEnvelopeXml = new XDocument();
            try
            {
                soapEnvelopeXml = CreateSoapEnvelopehoteldetail(req);
            }
            catch { }
            try
            {
                var _url = url;
                var _action = action;  
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
                    try
                    {
                        XElement availresponse = XElement.Parse(soapResult.ToString());
                        XElement doc = RemoveAllNamespaces(availresponse);
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 3;
                        log.LogType = "CXLPolicy";
                        log.SupplierID = 1;
                        log.logrequestXML = soapEnvelopeXml.ToString();
                        log.logresponseXML = doc.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CallWebService";
                        ex1.PageName = "TrvHotelDetailsWithCancellation";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
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
                    try
                    {
                        XElement availresponse = XElement.Parse(text.ToString());
                        XElement doc = RemoveAllNamespaces(availresponse);
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 3;
                        log.LogType = "CXLPolicy";
                        log.SupplierID = 1;
                        log.logrequestXML = soapEnvelopeXml.ToString();
                        log.logresponseXML = doc.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CallWebService";
                        ex1.PageName = "TrvHotelDetailsWithCancellation";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
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
        private static XDocument CreateSoapEnvelopehoteldetail(XElement req)
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
                mainchildrenages = "<ChildrenAges></ChildrenAges>";
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

            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
            currencyid = suppliercred.Descendants("currencyid").FirstOrDefault().Value;
            
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                         "<soap:Body>" +
                           "<CheckAvailabilityWithCancellation_NoCache_LiveCalculation xmlns='http://travelcontrol.softexsw.us/'>" +
                              "<SecStr>" + Secret + "</SecStr>" +
                            "<AccountName>" + AccountName + "</AccountName>" +
                            "<UserName>" + UserName + "</UserName>" +
                            "<Password>" + Password + "</Password>" +
                            "<AgentID>" + AgentID + "</AgentID>" +
                              "<FromDate>" + req.Descendants("FromDate").Single().Value + "</FromDate>" +
                              "<ToDate>" + req.Descendants("ToDate").Single().Value + "</ToDate>" +
                             "<HotelID>" + req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value + "</HotelID>" +
                             "<RoomType>" + req.Descendants("Room").Attributes("ID").FirstOrDefault().Value + "</RoomType>" +
                             "<MealPlan>" + req.Descendants("Room").Attributes("MealPlanID").FirstOrDefault().Value + "</MealPlan>" +
                             "<OccupancyID>" + req.Descendants("Room").Attributes("OccupancyID").FirstOrDefault().Value + "</OccupancyID>" +
                             "<AdultPax>" + req.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value + "</AdultPax>" +
                             "<ChildPax>" + req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value + "</ChildPax>" +
                             mainchildrenages +
                             "<ExtraBedAdult>false</ExtraBedAdult>" +
                             "<ExtraBedChild>false</ExtraBedChild>" +
                             "<Nationality_CountryID>" + paxcountryid + "</Nationality_CountryID>" + //320
                             "<CurrencyID>" + currencyid + "</CurrencyID>" +
                             "<Availability>ShowAvailableOnly</Availability>" +
                             "<GetHotelImageLink>true</GetHotelImageLink>" +
                             "<GetHotelMapLink>true</GetHotelMapLink>" +
                             "<Source>0</Source>" +
                           "</CheckAvailabilityWithCancellation_NoCache_LiveCalculation>" +
                         "</soap:Body>" +
                       "</soap:Envelope>";
            //string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
            //         "<soap:Body>" +
            //           "<CheckAvailabilityWithCancellation_NoCache_LiveCalculationResult xmlns='http://travelcontrol.softexsw.us/'>" +
            //              "<SecStr>" + Secret + "</SecStr>" +
            //                 "<AccountName>" + AccountName + "</AccountName>" +
            //                 "<UserName>" + UserName + "</UserName>" +
            //                 "<Password>" + Password + "</Password>" +
            //                 "<AgentID>" + AgentID + "</AgentID>" +
            //             "<FromDate>" + req.Descendants("FromDate").Single().Value + "</FromDate>" +
            //             "<ToDate>" + req.Descendants("ToDate").Single().Value + "</ToDate>" +
            //             "<CountryID>" + countryid + "</CountryID>" +
            //             "<CityID>" + cityid + "</CityID>" +
            //             "<MinLandCategory>" + req.Descendants("MinStarRating").Single().Value + "</MinLandCategory>" +
            //             "<MaxLandCategory>" + req.Descendants("MaxStarRating").Single().Value + "</MaxLandCategory>" +
            //             "<AreaID>0</AreaID>" +
            //             "<AreaName></AreaName>" +
            //             "<PropertyType>0</PropertyType>" +
            //             "<HotelName>" + req.Descendants("HotelName").Single().Value + "</HotelName>" +
            //             "<OccupancyID>" + req.Descendants("OccupancyID").Single().Value + "</OccupancyID>" +
            //             "<AdultPax>" + req.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value + "</AdultPax>" +
            //             "<ChildPax>" + req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value + "</ChildPax>" +
            //              mainchildrenages +
            //             "<ExtraBedAdult>false</ExtraBedAdult>" +
            //             "<ExtraBedChild>false</ExtraBedChild>" +
            //             "<Nationality_CountryID>" + paxcountryid + "</Nationality_CountryID>" +
            //             "<CurrencyID>" + req.Descendants("CurrencyID").Single().Value + "</CurrencyID>" +
            //             "<MaxOverallPrice>0</MaxOverallPrice>" +
            //             "<Availability>ShowAvailableOnly</Availability>" +
            //             "<RoomType>" + req.Descendants("RoomID").Single().Value + "</RoomType>" +  //RoomID
            //             "<MealPlan>" + req.Descendants("MealPlanID").Single().Value + "</MealPlan>" + //MealPlanID 1
            //             "<GetHotelImageLink>true</GetHotelImageLink>" +
            //             "<GetHotelMapLink>true</GetHotelMapLink>" +
            //             "<Source>0</Source>" +
            //             "<LimitRoomTypesInResult>0</LimitRoomTypesInResult>" +
            //           "</CheckAvailabilityWithCancellation_NoCache_LiveCalculationResult>" +
            //         "</soap:Body>" +
            //        "</soap:Envelope>";
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
        #region Merge CXL Policy
        public XElement MergCxlPolicy(List<XElement> rooms, string currencycode)
        {
            List<XElement> policyList = new List<XElement>();



            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
                Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0")).
                GroupBy(r => new { r.Attribute("LastCancellationDate").Value }).Select(y => y.First()).
                OrderBy(p => p.Attribute("LastCancellationDate").Value);
            if (dateLst.Count() > 0)
            {

                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCancellationDate").Value;

                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms)
                    {
                        var prItem = rm.Descendants("CancellationPolicy").
                            Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value == date)).
                            FirstOrDefault();
                        if (prItem != null)
                        {
                            var price = prItem.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            DateTime oDate = date.HotelsDate();

                            var lastItem = rm.Descendants("CancellationPolicy").
                                Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value.HotelsDate() < oDate));

                            if (lastItem.Count() > 0)
                            {
                                var lastDate = lastItem.Max(y => y.Attribute("LastCancellationDate").Value);
                                var lastprice = rm.Descendants("CancellationPolicy").
                        Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value == lastDate)).
                        FirstOrDefault().Attribute("ApplicableAmount").Value;
                                datePrice += Convert.ToDecimal(lastprice);
                            }
                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", date), new XAttribute("ApplicableAmount", datePrice), new XAttribute("NoShowPolicy", "0"), "");
                    policyList.Add(pItem);

                }



                policyList = policyList.GroupBy(x => new { x.Attribute("LastCancellationDate").Value.HotelsDate().Date }).
                    Select(y => new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))), new XAttribute("NoShowPolicy", "0"), "")).ToList();

            }
            var lastCxlDate = rooms.Descendants("CancellationPolicy").Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0")).Min(y => y.Attribute("LastCancellationDate").Value.HotelsDate().Date);
            policyList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", lastCxlDate.AddDays(-1).ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0"), "Cancellation done on before " + lastCxlDate.ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"));


            var NoShow = rooms.Descendants("CancellationPolicy").Where(pq => pq.Attribute("NoShowPolicy").Value == "1").GroupBy(r => new { r.Attribute("NoShowPolicy").Value }).Select(x => new { date = x.Min(y => y.Attribute("LastCancellationDate").Value.HotelsDate().Date), price = x.Sum(y => Convert.ToDecimal(y.Attribute("ApplicableAmount").Value)) });

            if (NoShow.Count() > 0)
            {
                var showItem = NoShow.FirstOrDefault();
                policyList.Add(new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", showItem.date.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", showItem.price), new XAttribute("NoShowPolicy", "1"), ""));

            }
            XElement cxlItem = new XElement("CancellationPolicies", policyList);
            return cxlItem;
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
    }
}