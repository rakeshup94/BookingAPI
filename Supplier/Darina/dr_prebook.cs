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
    public class dr_prebook
    {
        XElement reqTravillio;
        string dmc = string.Empty;
        string requestid = string.Empty;
        #region PreBooking Darina
        public XElement prebookdarina(XElement req, string xmlout)
        {
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;

            IEnumerable<XElement> request = req.Descendants("HotelPreBookingRequest");
            reqTravillio = req;
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            List<XElement> htlst = req.Descendants("GiataHotelList").ToList();

            supplierid = htlst[0].Attribute("GSupID").Value;
            dmc = xmlout;

            //DarinaCredentials _credential = new DarinaCredentials();
            //var _url = _credential.APIURL_PreBook_Book;
            //var _action = "http://travelcontrol.softexsw.us/CheckAvailabilityWithCancellation_NoCache_LiveCalculation";
            XElement suppliercred = supplier_Cred.getsupplier_credentials(reqTravillio.Descendants("CustomerID").FirstOrDefault().Value, "1");
            var _url = suppliercred.Descendants("APIURL_PreBook_Book").FirstOrDefault().Value;
            var _action = suppliercred.Descendants("APIURL_NoCacheaction").FirstOrDefault().Value;
            string avail = CallWebService(req, _url, _action,1);
            if (avail != null)
            {
                //WriteToFile(avail.ToString());
                string termcondition = string.Empty;
                XElement doc = XElement.Parse(avail);

                int error = doc.Descendants("Hotels").Count();
                if (error > 0)
                {
                    #region Darina Out XML                    
                    List<XElement> htdetails = doc.Descendants("Hotels").ToList();

                    List<XElement> htalldetail = doc.Descendants("D").ToList();
                    bool status;
                    string availabilityvalue = string.Empty;
                    try
                    {
                        requestid = reqTravillio.Descendants("HotelPreBookingRequest").Descendants("RoomTypes").FirstOrDefault().Descendants("RequestID").FirstOrDefault().Value;
                        XElement doff = htalldetail.Descendants("AccRates").Where(x => x.Descendants("RequestID").FirstOrDefault().Value == requestid).FirstOrDefault();
                        string availval = doff.Descendants("Availability").FirstOrDefault().Value;
                        if(availval=="1")
                        {
                            availabilityvalue = "1";
                        }
                        else
                        {
                            availabilityvalue = htalldetail[0].Descendants("Availability").FirstOrDefault().Value;
                        }
                    }
                    catch { availabilityvalue = htalldetail[0].Descendants("Availability").FirstOrDefault().Value; }

                    if (availabilityvalue == "1")
                    {
                        status = true;                        
                    }
                    else
                    {
                        status = false;
                    }
                    try
                    {
                        _url = suppliercred.Descendants("APIURL").FirstOrDefault().Value;
                        _action = suppliercred.Descendants("propertyadditionaldetail").FirstOrDefault().Value;
                        string extradetails = CallWebService(req, _url, _action, 2);
                        XElement extradoc = XElement.Parse(extradetails);
                        XElement resck = extradoc.Descendants("d").Where(x => x.Descendants("fieldname").FirstOrDefault().Value == "REMARKS").FirstOrDefault();
                        termcondition = resck.Descendants("Value").FirstOrDefault().Value;
                    }
                    catch { }
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
                                new XElement("NewPrice", null), // said by manisha
                               new XElement("Hotels",
                                   new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(htdetails.Descendants("HotelID").Single().Value)),
                                           new XElement("HotelName", Convert.ToString(htdetails.Descendants("Name").Single().Value)),
                                           new XElement("Status", status),
                                           new XElement("TermCondition", termcondition),
                                           new XElement("HotelImgSmall", Convert.ToString(htdetails.Descendants("SmallImgLink").Single().Value)),
                                           new XElement("HotelImgLarge", Convert.ToString(htdetails.Descendants("LargeImgLink").Single().Value)),
                                           new XElement("MapLink", ""),
                                           new XElement("DMC", dmc),
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
                    #region Server not responding
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
                               new XElement("ErrorTxt", "Server is not responding")
                                       )
                                   )
                  ));
                    return searchdoc;
                    #endregion
                }
            }
            else
            {
                #region Server Not Responding
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
                           new XElement("ErrorTxt", "Server is not responding")
                                   )
                               )


              ));
                return searchdoc;
                #endregion
            }
        }
        #endregion

        #region Hotel's Rooms (Darina Holidays)
        private IEnumerable<XElement> GetHotelRooms(List<XElement> htlist, string CurrencyName)
        {
            #region Hotel's Rooms (Darina Holidays)
            List<XElement> rmlst = new List<XElement>();
            List<XElement> roomlist = null;
            try
            {
                roomlist = htlist.Descendants("AccRates").Where(x => x.Descendants("RequestID").FirstOrDefault().Value == requestid).ToList();
            }
            catch { roomlist = htlist.Descendants("AccRates").ToList(); }
            List<XElement> RoomTypes = htlist.Descendants("RoomTypes").ToList();
            List<XElement> roomlistatt = htlist.Descendants("Cancellation").ToList();
            DateTime fromDate = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            DateTime toDate = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            int nights = (int)(toDate - fromDate).TotalDays;

            XElement docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
            XElement dococcupancy = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\Occupancy.xml"));
            // Parallel.For(0,roomlist.Count(), i => 
            for (int i = 0; i < roomlist.Count(); i++)
            {
                string mealplanname = string.Empty;
                string mealplancode = string.Empty;
                string occupanyname = string.Empty;
                try
                {
                    IEnumerable<XElement> docmealplandet = docmealplan.Descendants("d0").Where(x => x.Descendants("MealPlanID").Single().Value == roomlist[i].Descendants("MealPlan").FirstOrDefault().Value);
                    mealplanname = Convert.ToString(docmealplandet.Descendants("MealPlanName").FirstOrDefault().Value);
                    mealplancode = Convert.ToString(docmealplandet.Descendants("MealPlanCode").FirstOrDefault().Value);

                    IEnumerable<XElement> dococcupancydet = dococcupancy.Descendants("d0").Where(x => x.Descendants("OccupancyID").Single().Value == roomlist[i].Descendants("Occupancy").FirstOrDefault().Value);
                    occupanyname = Convert.ToString(dococcupancydet.Descendants("OccupancyName").FirstOrDefault().Value);
                }
                catch { }
                string lastcancellationdate = Convert.ToString(roomlistatt.Descendants("ToDate").LastOrDefault().Value);
                if (lastcancellationdate == null || lastcancellationdate == "")
                {
                    lastcancellationdate = "";
                }
                rmlst.Add(new XElement("RoomTypes", new XAttribute("Index", 1), new XAttribute("TotalRate", roomlist[i].Descendants("RatePerStay").FirstOrDefault().Value),

                    new XElement("Room",
                             new XAttribute("ID", Convert.ToString(RoomTypes[i].Descendants("RoomTypeID").FirstOrDefault().Value)),
                             new XAttribute("SuppliersID", "1"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomlist[i].Descendants("Serial").FirstOrDefault().Value)),
                             new XAttribute("RoomType", Convert.ToString(RoomTypes[i].Descendants("RoomTypeName").FirstOrDefault().Value)),
                             new XAttribute("OccupancyID", Convert.ToString(roomlist[i].Descendants("Occupancy").FirstOrDefault().Value)),
                             new XAttribute("OccupancyName", Convert.ToString(occupanyname)),
                             new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Descendants("MealPlan").FirstOrDefault().Value)),
                             new XAttribute("MealPlanName", Convert.ToString(mealplanname)),
                             new XAttribute("MealPlanCode", mealplancode),
                             new XAttribute("MealPlanPrice", ""),
                             new XAttribute("PerNightRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerNight").FirstOrDefault().Value)),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerStay").FirstOrDefault().Value)),
                             new XAttribute("CancellationDate", lastcancellationdate),
                             new XAttribute("CancellationAmount", ""),
                             new XAttribute("isAvailable", "true"),
                             new XElement("RequestID", Convert.ToString(roomlist[i].Descendants("RequestID").FirstOrDefault().Value)),
                             new XElement("Offers", ""),
                             new XElement("PromotionList",
                             new XElement("Promotions", Convert.ToString(""))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements", null),
                                new XElement("PriceBreakups",
                                GetRoomsPriceBreakupDarina(nights, Convert.ToString(roomlist[i].Descendants("RatePerNight").FirstOrDefault().Value))),
                                 new XElement("AdultNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value)),
                                 new XElement("ChildNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value))
                             ),


                    new XElement("CancellationPolicies",
                         GetRoomCancellationPolicy(roomlistatt, Convert.ToString(roomlist[i].Descendants("RatePerNight").FirstOrDefault().Value), Convert.ToString(roomlist[i].Descendants("RatePerStay").FirstOrDefault().Value), CurrencyName)
                         ))
                         );
            }
            return rmlst;
            #endregion
        }
        #endregion
        #region Room's Cancellation Policies (Darina Holidays)
        private IEnumerable<XElement> GetRoomCancellationPolicy(List<XElement> cancellationpolicy, string pernightcost, string totalamount, string CurrencyName)
        {
            #region Room's Cancellation Policies (Darina Holidays)
            List<XElement> rmplc = new List<XElement>();
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
                    rmplc.Add(new XElement("CancellationPolicy", "Cancellation done by or before " + Convert.ToString(roomlist[i].Descendants("ToDate").Single().Value) + "  will apply " + CurrencyName + " " + AmountApplicable + "  Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(roomlist[i].Descendants("ToDate").Single().Value))
                        , new XAttribute("ApplicableAmount", AmountApplicable)
                        , new XAttribute("NoShowPolicy", "0")));
                }
                else
                {
                    rmplc.Add(new XElement("CancellationPolicy", "Cancellation done by or before " + Convert.ToString(roomlist[i].Descendants("FromDate").Single().Value) + "  will apply " + CurrencyName + " " + AmountApplicable + "  Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(roomlist[i].Descendants("FromDate").Single().Value))
                        , new XAttribute("ApplicableAmount", AmountApplicable)
                        , new XAttribute("NoShowPolicy", "0")));
                }
            }
            if (freecxl == 0)
            {
                List<XElement> grhtrm = new List<XElement>();
                grhtrm.Add(new XElement("Room", rmplc));
                XElement cxlfinal = MergCxlPolicy(grhtrm, CurrencyName);
                List<XElement> cxlfinalres = cxlfinal.Descendants("CancellationPolicy").ToList();
                return cxlfinalres;
            }
            else
            {
                return rmplc;
            }
            //return rmplc;
            #endregion
        }
        #endregion
        #region Darina PriceBreakups
        private IEnumerable<XElement> GetRoomsPriceBreakupDarina(int nights, string pernightprice)
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
        public string CallWebService(XElement req, string url, string action,int index)
        {
            try
            {
                var _url = url;
                var _action = action;
                XDocument soapEnvelopeXml = new XDocument();
                int logtypeid = 4;
                string logtype = "PreBook";
                if (index == 2)
                {
                    logtypeid = 52;
                    logtype = "ExtraDetail";
                    soapEnvelopeXml = CreateSoapEnvelopehotelextradetail(req);
                }
                else
                {
                    soapEnvelopeXml = CreateSoapEnvelopehotelprebooking(req);
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

                    try
                    {
                        XElement availresponse = XElement.Parse(soapResult.ToString());
                        XElement doc = RemoveAllNamespaces(availresponse);                        
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravillio.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                        log.LogTypeID = logtypeid;
                        log.LogType = logtype;
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
                        ex1.PageName = "dr_prebook";
                        ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                        ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
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
                    CustomException ex1 = new CustomException(webex);
                    ex1.MethodName = "CallWebService";
                    ex1.PageName = "dr_prebook";
                    ex1.CustomerID = reqTravillio.Descendants("CustomerID").Single().Value;
                    ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
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
        private static XDocument CreateSoapEnvelopehotelprebooking(XElement req)
        {
            int totalchild = Convert.ToInt32(req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value);
            string mainchildrenages = "";
            string childrenages = "";
            #region City ID / Country ID / Pax Nationality Country ID
            //string cityid = "";
            //string countryid = "";
            string paxcountryid = "";
            //XElement doccity = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\cities.xml"));
            XElement docccountry = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\country.xml"));
            //IEnumerable<XElement> doccityd = doccity.Descendants("d0").Where(x => x.Descendants("LetterCode").Single().Value == req.Descendants("CityCode").Single().Value);
            //countryid = doccityd.Descendants("CountryID").SingleOrDefault().Value;
            //cityid = doccityd.Descendants("Serial").SingleOrDefault().Value;
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
                              //"<RoomType>" + req.Descendants("RoomTypeID").Single().Value + "</RoomType>" +
                              //"<MealPlan>" + req.Descendants("MealPlanID").Single().Value + "</MealPlan>" +
                              //"<OccupancyID>" + req.Descendants("OccupancyID").Single().Value + "</OccupancyID>" +
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
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static XDocument CreateSoapEnvelopehotelextradetail(XElement req)
        {
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
            #endregion
            string ss = "<soap:Envelope xmlns:soap='http://www.w3.org/2003/05/soap-envelope' xmlns:trav='http://travelcontrol.softexsw.us/'>"+
                           "<soap:Header/>"+
                           "<soap:Body>"+
                              "<trav:GetProperty_AdditionalFields>"+ 
                                 "<trav:SecStr>"+Secret+"</trav:SecStr>"+ 
                                 "<trav:AccountName>"+AccountName+"</trav:AccountName>"+ 
                                 "<trav:UserName>"+UserName+"</trav:UserName>"+ 
                                 "<trav:Password>"+Password+"</trav:Password>"+
                                 "<trav:AgentID>"+AgentID+"</trav:AgentID>"+
                                 "<trav:HotelID>"+req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value+"</trav:HotelID>"+
                              "</trav:GetProperty_AdditionalFields>"+
                           "</soap:Body>"+
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
    }


}