using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelBeds;

namespace TravillioXMLOutService.App_Code
{
    public class CancellationPolicyHotelBeds
    {
        XElement reqTravillio;
        #region Credentails of HotelBeds
        string apiKey = string.Empty;
        string Secret = string.Empty;
        #endregion
        #region HotelCXLPolicyHotelBeds of HotelBeds (XML OUT for Travayoo)
        public XElement HotelCXLPolicyHotelBeds(XElement req)
        {
            string reqstrcxl = string.Empty;
            reqTravillio = req;
            XElement hotelprebookresponse = null;
            XElement hotelpreBooking = null;
            IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest").ToList();
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";

            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;

            try
            {
                //HotelBedsCredential _credential = new HotelBedsCredential();
                //apiKey = _credential.apiKey;
                //Secret = _credential.Secret;
                //string endpoint = "https://api.test.hotelbeds.com/hotel-api/1.0/checkrates";
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "4");
                apiKey = suppliercred.Descendants("apiKey").FirstOrDefault().Value;
                Secret = suppliercred.Descendants("Secret").FirstOrDefault().Value;
                string endpoint = suppliercred.Descendants("checkrateendpoint").FirstOrDefault().Value;
                #endregion
                List<XElement> getroom = reqTravillio.Descendants("Room").ToList();
                string hotel = string.Empty;
                XElement occupancyrequest = new XElement(
                        new XElement("rooms", getroomkey(getroom)));
                string reqstr = "<checkRateRQ xmlns='http://www.hotelbeds.com/schemas/messages' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' upselling='false' language='ENG'>" +
                                   occupancyrequest +
                                "</checkRateRQ>";
                reqstrcxl = reqstr;
                string signature;
                using (var sha = SHA256.Create())
                {
                    long ts = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                    Console.WriteLine("Timestamp: " + ts);
                    var computedHash = sha.ComputeHash(Encoding.UTF8.GetBytes(apiKey + Secret + ts));
                    signature = BitConverter.ToString(computedHash).Replace("-", "");
                }

                string response = string.Empty;
                using (var client = new WebClient())
                {

                    client.Headers.Add("X-Signature", signature);
                    client.Headers.Add("Api-Key", apiKey);
                    client.Headers.Add("Accept", "application/xml");
                    client.Headers.Add("Content-Type", "application/xml");
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
                    response = client.UploadString(endpoint, reqstr);


                    XElement availresponse = XElement.Parse(response.ToString());

                    XElement doc = RemoveAllNamespaces(availresponse);

                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransID").Single().Value;
                    log.LogTypeID = 3;
                    log.LogType = "CXLPolicy";
                    log.SupplierID = 4;
                    log.logrequestXML = reqstr.ToString();
                    log.logresponseXML = doc.ToString();
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(log);

                    XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
                    List<XElement> hotelavailabilityres = doc.Descendants("hotel").ToList();

                    hotelprebookresponse = GetHotelListHotelBeds(hotelavailabilityres).FirstOrDefault();

                    #region PreBooking Response
                    

                    #region XML OUT

                    hotelpreBooking = new XElement(
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
                                                       hotelprebookresponse
                                      )))));

                    #endregion
                    #endregion

                }
                return hotelpreBooking;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelCXLPolicyHotelBeds";
                ex1.PageName = "CancellationPolicyHotelBeds";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log.TrackNumber = req.Descendants("TransID").Single().Value;
                    log.LogTypeID = 3;
                    log.LogType = "CXLPolicy";
                    log.SupplierID = 4;
                    log.logrequestXML = reqstrcxl.ToString();
                    log.logresponseXML = ex.Message.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
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
                                   new XElement("HotelDetailwithcancellationResponse",
                                       new XElement("ErrorTxt", "No Policy Found")
                                               )
                                           )
                          ));
                return searchdoc;
            }
        }
        #endregion

        #region HotelBeds
        #region HotelBeds Hotel Listing
        private IEnumerable<XElement> GetHotelListHotelBeds(List<XElement> htlist)
        {
            #region HotelBeds
            List<XElement> hotellst = new List<XElement>();
            Int32 length = htlist.Count();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    hotellst.Add(new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(htlist[i].Attribute("code").Value)),
                                                       new XElement("HotelName", Convert.ToString(htlist[i].Attribute("name").Value)),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", "HotelBeds"),
                                                       new XElement("Currency", ""),
                                                       new XElement("Offers", "")
                                                       , new XElement("Rooms",
                                                    GetHotelRoomListingHotelBeds(htlist[i].Descendants("room").ToList(), htlist)
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

        #region HotelBeds Hotel's Room Listing
        private IEnumerable<XElement> GetHotelRoomListingHotelBeds(List<XElement> roomlist, List<XElement> htlist)
        {

            XNamespace ns = "http://www.hotelbeds.com/schemas/messages";
            List<XElement> str = new List<XElement>();
            #region CXL Policy
            str.Add(new XElement("Room",
                 new XAttribute("ID", Convert.ToString("")),
                 new XAttribute("RoomType", Convert.ToString("")),
                 new XAttribute("MealPlanPrice", ""),
                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                 new XAttribute("TotalRoomRate", Convert.ToString("")),
                 new XAttribute("CancellationDate", ""),
                 new XElement("CancellationPolicies",
             GetRoomCancellationPolicyHotelBeds(htlist[0].Descendants("cancellationPolicies").ToList(), htlist[0].Attribute("currency").Value))
                 ));
            #endregion
            return str;

        }
        #endregion        
        
        #region Room's Cancellation Policies from HotelBeds
        private IEnumerable<XElement> GetRoomCancellationPolicyHotelBeds(List<XElement> troom, string currencycode)
        {
            #region Room's Cancellation Policies from HotelBeds
            List<XElement> grhtrm = new List<XElement>();
            for (int i = 0; i < troom.Count(); i++)
            {
                List<XElement> cxlplc = troom[i].Descendants("cancellationPolicy").ToList();

                string chformat = cxlplc[0].Attribute("from").Value;
                string output = chformat.Substring(chformat.Length - 1, 1);
                string dtlastcxldate2 = string.Empty;
                if (output == "Z")
                {
                    dtlastcxldate2 = ((DateTimeOffset.ParseExact(cxlplc[0].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture).UtcDateTime).AddDays(-1).ToString("dd/MM/yyyy"));
                }
                else
                {
                    dtlastcxldate2 = ((DateTimeOffset.ParseExact(cxlplc[0].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture).UtcDateTime).AddDays(-1).ToString("dd/MM/yyyy"));
                }                

                List<XElement> htrm = new List<XElement>();
                htrm.Add(
                       new XElement("CancellationPolicy", ""
                       , new XAttribute("LastCancellationDate", Convert.ToString(dtlastcxldate2))
                       , new XAttribute("ApplicableAmount", "0.00")
                       , new XAttribute("NoShowPolicy", "0")));

                for (int j = 0; j < cxlplc.Count(); j++)
                {
                    string date2 = string.Empty;
                    if(output=="Z")
                    {
                        date2 = ((DateTimeOffset.ParseExact(cxlplc[j].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture).UtcDateTime).ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        date2 = ((DateTimeOffset.ParseExact(cxlplc[j].Attribute("from").Value, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture).UtcDateTime).ToString("dd/MM/yyyy"));
                    }
                   
                    string totamt = cxlplc[j].Attribute("amount").Value;

                    htrm.Add(
                        new XElement("CancellationPolicy", "Cancellation done on after " + date2 + "  will apply " + currencycode + " " + Convert.ToDouble(totamt) + "  Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(date2))
                        , new XAttribute("ApplicableAmount", Convert.ToDouble(totamt))
                        , new XAttribute("NoShowPolicy", "0")));
                }
                grhtrm.Add(new XElement("Room", htrm));
            }
            XElement cxlfinal = MergCxlPolicy(grhtrm, currencycode);
            List<XElement> cxlfinalres = cxlfinal.Descendants("CancellationPolicy").ToList();
            return cxlfinalres;
            #endregion
        }
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
            var lastCxlDate = rooms.Descendants("CancellationPolicy").Where(pq => (pq.Attribute("ApplicableAmount").Value == "0.00" && pq.Attribute("NoShowPolicy").Value == "0")).Min(y => y.Attribute("LastCancellationDate").Value.HotelsDate().Date);
            policyList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", lastCxlDate.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0"), "Cancellation done on before " + lastCxlDate.ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"));


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
        #region Old policy
        private IEnumerable<XElement> GetRoomCancellationPolicyHotelBedsOLD(List<XElement> cancellationpolicy, string currencycode)
        {
            #region Room's Cancellation Policies from HotelBeds
            List<XElement> htrm = new List<XElement>();

            var distinctcp = cancellationpolicy.GroupBy(x => x.Attribute("from").Value).ToList();

            #region Last CXL Date Policy
            XElement lastcxldate = cancellationpolicy.OrderBy(x => x.Attribute("from").Value).FirstOrDefault();
            string slastcxldate = lastcxldate.Attribute("from").Value;
            var dddtt = DateTimeOffset.ParseExact(slastcxldate, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            DateTime ddlastcxldate = dddtt.UtcDateTime;
            //DateTime ddlastcxldate = DateTime.ParseExact(slastcxldate, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
            ddlastcxldate = ddlastcxldate.AddDays(-1);
            string dtlastcxldate = ddlastcxldate.ToString("dd/MM/yyyy");
            htrm.Add(new XElement("CancellationPolicy", "Cancellation done on before " + dtlastcxldate + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"
                   , new XAttribute("LastCancellationDate", Convert.ToString(dtlastcxldate))
                   , new XAttribute("ApplicableAmount", "0")
                   , new XAttribute("NoShowPolicy", "0")));
            #endregion

            for (int i = 0; i < distinctcp.Count(); i++)
            {
                List<XElement> ff = distinctcp[i].ToList();
                decimal totamt = 0;
                for (int j = 0; j < ff.Count(); j++)
                {
                    decimal at = Convert.ToDecimal(ff[j].Attribute("amount").Value);
                    totamt = totamt + at;
                }
                var dtt = DateTimeOffset.ParseExact(distinctcp[i].Key, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                DateTime fdate = dtt.UtcDateTime;
                //DateTime fdate = DateTime.ParseExact(distinctcp[i].Key, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                //DateTime fdate = DateTime.ParseExact(distinctcp[i].Key, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
                string date2 = fdate.ToString("dd/MM/yyyy");


                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + date2 + "  will apply " + currencycode + " " + totamt + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(date2))
                    , new XAttribute("ApplicableAmount", totamt)
                    , new XAttribute("NoShowPolicy", "0")));

            }
            
            return htrm;
            #endregion
        }
        #endregion
        #endregion

        #endregion

        #region Prebook Request
        private List<XElement> getroomkey(List<XElement> room)
        {
            #region Bind Room keys
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("room",
                       new XAttribute("rateKey", Convert.ToString(room[i].Attribute("SessionID").Value)))
                );
            }
            return str;
            #endregion
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