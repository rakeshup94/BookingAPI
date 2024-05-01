
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models.DotW;
using TravillioXMLOutService.Common.DotW;
using System.IO;
using TravillioXMLOutService.Models;
using System.Threading;
using System.Threading.Tasks;
using TravillioXMLOutService.Common;
using TravillioXMLOutService.Models.Common;
using System.Data;

namespace TravillioXMLOutService.Supplier.DotW
{
    public class DotwService : IDisposable
    {

        DotWCredentials _credetials;
        DotwRepository _DotwRepo;
        XElement travyoReq;
        string customerid = string.Empty;
        string dmc = string.Empty;
        string HtlCode = string.Empty;
        string CrncyCode = string.Empty;
        string XmlPath = ConfigurationManager.AppSettings["DotWPath"];
        decimal price = 0.0m;
        public DotwService(string _customerId)
        {
            #region Credentials
            XElement suppliercred = supplier_Cred.getsupplier_credentials(_customerId, "5");
            try
            {
                customerid = _customerId;
                string serviceHost = suppliercred.Element("ServiceHost").Value;
                string userName = suppliercred.Element("UserName").Value;
                string password = suppliercred.Element("Password").Value;
                string id = suppliercred.Element("Id").Value;
                int currency = Convert.ToInt32(suppliercred.Element("Currency").Value);
                _credetials = new DotWCredentials(serviceHost, userName, password, id, currency);
                _DotwRepo = new DotwRepository(serviceHost, userName, password, id, currency);
            }
            catch { }
            #endregion
        }
        public DotwService()
        {
        }


        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region HotelSearch

        public XElement SearchByHotel(XElement req)
        {
            XElement resp = null;
            try
            {
                this.travyoReq = req;
                XDocument dotwReq = CreateSearchReq(req);
                resp = HtlSearchResp(dotwReq);
            }
            catch (Exception ex)
            {
                resp = new XElement("searchResponse", new XElement("Hotels", null),
                     new XElement("ErrorText", ex.Message));

                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "SearchByHotel";
                ex1.PageName = "DotwService";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion




            }
            return resp;
        }

        XDocument CreateSearchReq(XElement req)
        {
            XElement htlItem = null;

            if (!string.IsNullOrEmpty(req.Descendants("HotelID").FirstOrDefault().Value))
            {
                var model = new SqlModel()
                {
                    flag = 2,
                    SupplierId = 5,
                    HotelCode = req.Descendants("HotelID").FirstOrDefault().Value
                };
                DataTable htlList = TravayooRepository.GetData(model);




                if (htlList.Rows.Count > 0)
                {
                    var result = htlList.AsEnumerable().Select(y => new XElement("fieldValue", y.Field<string>("HotelID")));
                    htlItem = new XElement("fieldValues", result);
                }
                else
                {
                    throw new Exception("There is no hotel available in database");
                }









        
            }
            else
            {
                string CityId = req.Descendants("CityID").FirstOrDefault().Value.DotWCity();
                string strPath = ConfigurationManager.AppSettings["DotWNPath"] + @"Hotels\City-" + CityId + ".xml";
                string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
                XDocument htlList = XDocument.Load(FilePath);
                var result = htlList.Descendants("hotel").
                    Where(x => x.Element("hotelName").Value.ToUpper().Contains(req.Descendants("HotelName").FirstOrDefault().Value.ToUpper())).
                    Select(y => new XElement("fieldValue", y.Attribute("hotelid").Value));

                if (result.Count() > 0)
                {
                    htlItem = new XElement("fieldValues", result);
                }
                else
                {
                    throw new Exception("There is no hotel available in database");
                }
            }

            int MinRating = int.Parse(req.Descendants("MinStarRating").FirstOrDefault().Value);
            int MaxRating = int.Parse(req.Descendants("MaxStarRating").FirstOrDefault().Value);
            XNamespace complx = "http://us.dotwconnect.com/xsd/complexCondition";
            XNamespace atomic = "http://us.dotwconnect.com/xsd/atomicCondition";
            XDocument dowReq = new XDocument(
  new XDeclaration("1.0", "utf-8", "yes"),
  new XElement("customer",
     new XElement("username", _credetials.UserName),
       new XElement("password", _credetials.Password),
         new XElement("id", _credetials.Id),
           new XElement("source", _credetials.Source),
                 new XElement("product", _credetials.Service),
     new XElement("request",
        new XAttribute("command", "searchhotels"),
           new XElement("bookingDetails",
               new XElement("fromDate", req.Descendants("FromDate").FirstOrDefault().DotWDate()),
               new XElement("toDate", req.Descendants("ToDate").FirstOrDefault().DotWDate()),
               new XElement("currency", _credetials.Currency),
               HtlSeaReqRoomTag(req.Descendants("RoomPax").ToList())),
           new XElement("return",
               new XElement("filters",
                   new XAttribute(XNamespace.Xmlns + "a", atomic),
                   new XAttribute(XNamespace.Xmlns + "c", complx),
                   new XElement(complx + "condition",
                       new XElement(atomic + "condition",
                           new XElement("fieldName", "hotelId"),
                           new XElement("fieldTest", "in"),
                           htlItem)))))));
            return dowReq;
        }
                
        public List<XElement> HtlSearchReq(XElement Req, string custID, string xtype)
        {
            customerid = custID;
            dmc = xtype;
            List<XElement> dotwresp = null;
            try
            {
                string HtId = Req.Descendants("HotelID").FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(HtId))
                {
                    var list = SearchByHotel(Req);
                    dotwresp = list.Descendants("Hotel").ToList();
                }
                else
                {
                    travyoReq = Req;
                    XDocument dowReq = new XDocument(
                       new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement("customer",
                          new XElement("username", _credetials.UserName),
                            new XElement("password", _credetials.Password),
                              new XElement("id", _credetials.Id),
                                new XElement("source", _credetials.Source),
                                      new XElement("product", _credetials.Service),
                          new XElement("request",
                             new XAttribute("command", "searchhotels"),
                                new XElement("bookingDetails",
                                    new XElement("fromDate", Req.Descendants("FromDate").FirstOrDefault().DotWDate()),
                                    new XElement("toDate", Req.Descendants("ToDate").FirstOrDefault().DotWDate()),
                                    new XElement("currency", _credetials.Currency),
                                    HtlSeaReqRoomTag(Req.Descendants("RoomPax").ToList())),
                                new XElement("return", HtlSeaRating()))));
                    XElement dowResp = HtlSearchResp(dowReq);
                    dotwresp = dowResp.Descendants("Hotel").ToList();
                }
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "HtlSearchReq";
                custEx.PageName = "DotwService";
                custEx.CustomerID = customerid;
                custEx.TranID = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
            }
            return dotwresp;
        }
        
        
        
        public XElement HtlSeaReqRoomTag(List<XElement> rmlst)
        {
            int counter = 0;
            XElement rmItem;
            if (rmlst != null)
            {
                var result = from room in rmlst
                             select new XElement("room", new XAttribute("runno", counter++),
                                         new XElement("adultsCode", room.Element("Adult").Value),
                                       HtlSeaReqChildTag(room.DescendantsOrEmpty("ChildAge")),
                                         new XElement("rateBasis", "-1"),
                                         new XElement("passengerNationality", travyoReq.Descendants("PaxNationality_CountryID").FirstOrDefault().Value.DotWCountry()),
                                           new XElement("passengerCountryOfResidence", travyoReq.Descendants("PaxResidenceID").FirstOrDefault().Value.DotWCountry()));
                rmItem = new XElement("rooms", new XAttribute("no", rmlst.Count), result);
            }
            else
            {
                rmItem = new XElement("rooms", new XAttribute("no", 0));
            }
            return rmItem;
        }
        public XElement HtlSeaReqChildTag(IEnumerable<XElement> chldlst)
        {
            int counter = 0;
            XElement chlItem;
            if (!chldlst.IsNullOrEmpty())
            {
                var result = from chld in chldlst
                             let counItem = 0
                             select new XElement("child", new XAttribute("runno", counter++), chld.Value);
                chlItem = new XElement("children", new XAttribute("no", chldlst.Count()), result);
            }
            else
            {
                chlItem = new XElement("children", new XAttribute("no", 0));
            }
            return chlItem;

        }

        public XElement HtlSeaRating()
        {
            XNamespace complx = "http://us.dotwconnect.com/xsd/complexCondition";
            XNamespace atomic = "http://us.dotwconnect.com/xsd/atomicCondition";
            XElement RatingFilter;

            int minRate = Convert.ToInt16(travyoReq.Descendants("MinStarRating").FirstOrDefault().Value);
            int maxRate = Convert.ToInt16(travyoReq.Descendants("MaxStarRating").FirstOrDefault().Value);
            List<XElement> FilterList = new List<XElement>();
            if (minRate == maxRate)
            {

                FilterList.Add(new XElement("fieldValue", minRate.ToString().ToDotWRating()));
                FilterList.Add(new XElement("fieldValue", 48055));
            }
            else
            {
                List<int> list = Enumerable.Range(minRate, maxRate - minRate + 1).ToList();

                FilterList = list.Select(x => new XElement("fieldValue", x.ToString().ToDotWRating())).ToList();
                FilterList.Add(new XElement("fieldValue", 48055));
            }

            RatingFilter = new XElement("filters",
                                     new XAttribute(XNamespace.Xmlns + "a", atomic),
                                     new XAttribute(XNamespace.Xmlns + "c", complx),
                                     new XElement("city", travyoReq.Descendants("CityID").FirstOrDefault().Value.DotWCity()),
                                     new XElement(complx + "condition",
                                         new XElement(atomic + "condition",
                                             new XElement("fieldName", "rating"),
                                             new XElement("fieldTest", "in"),
                                             new XElement("fieldValues", FilterList))));






            //if ((travyoReq.Descendants("MinStarRating").FirstOrDefault().Value != "0") && (travyoReq.Descendants("MinStarRating").FirstOrDefault().Value != travyoReq.Descendants("MaxStarRating").FirstOrDefault().Value))
            //{
            //    RatingFilter = new XElement("filters",
            //                              new XAttribute(XNamespace.Xmlns + "a", atomic),
            //                              new XAttribute(XNamespace.Xmlns + "c", complx),
            //                              new XElement("city", travyoReq.Descendants("CityID").FirstOrDefault().Value.DotWCity()),
            //                              new XElement(complx + "condition",
            //                                  new XElement(atomic + "condition",
            //                                      new XElement("fieldName", "rating"),
            //                                      new XElement("fieldTest", "between"),
            //                                      new XElement("fieldValues",
            //                                          new XElement("fieldValue", travyoReq.Descendants("MinStarRating").FirstOrDefault().Value.ToDotWRating()),
            //                                          new XElement("fieldValue", travyoReq.Descendants("MaxStarRating").FirstOrDefault().Value.ToDotWRating())))));
            //}
            //else if (travyoReq.Descendants("MinStarRating").FirstOrDefault().Value != travyoReq.Descendants("MaxStarRating").FirstOrDefault().Value)
            //{
            //    RatingFilter = new XElement("filters",
            //                           new XAttribute(XNamespace.Xmlns + "a", atomic),
            //                           new XAttribute(XNamespace.Xmlns + "c", complx),
            //                           new XElement("city", travyoReq.Descendants("CityID").FirstOrDefault().Value.DotWCity()),
            //                           new XElement(complx + "condition",
            //                               new XElement(atomic + "condition",
            //                                   new XElement("fieldName", "rating"),
            //                                   new XElement("fieldTest", "between"),
            //                                   new XElement("fieldValues", new XElement("fieldValue", "1".ToDotWRating()),
            //                                       new XElement("fieldValue", travyoReq.Descendants("MaxStarRating").FirstOrDefault().Value.ToDotWRating()))),
            //                                       new XElement("operator", "OR"),
            //                                       new XElement(atomic + "condition",
            //                                           new XElement("fieldName", "rating"),
            //                                           new XElement("fieldTest", "equals"),
            //                                           new XElement("fieldValues", new XElement("fieldValue", "0".ToDotWRating()))),
            //                                           new XElement("operator", "OR"),
            //                                           new XElement(atomic + "condition",
            //                                           new XElement("fieldName", "rating"),
            //                                           new XElement("fieldTest", "equals"),
            //                                           new XElement("fieldValues", new XElement("fieldValue", 48055)))));
            //}
            //else
            //{
            //    RatingFilter = new XElement("filters",
            //                           new XAttribute(XNamespace.Xmlns + "a", atomic),
            //                           new XAttribute(XNamespace.Xmlns + "c", complx),
            //                           new XElement("city", travyoReq.Descendants("CityID").FirstOrDefault().Value.DotWCity()),
            //                           new XElement(complx + "condition",
            //                               new XElement(atomic + "condition",
            //                                   new XElement("fieldName", "rating"),
            //                                   new XElement("fieldTest", "equals"),
            //                                   new XElement("fieldValues", new XElement("fieldValue",
            //                                       travyoReq.Descendants("MinStarRating").FirstOrDefault().Value.ToDotWRating()))

            //                                       )));
            //}







            return RatingFilter;
        }
        //////////////////////////////////HotelSearchResponse////////////////////
        public XElement HtlSearchResp(XDocument _dotWReq)
        {
            XElement _travyooResp;
            int noRoom = _dotWReq.Descendants("room").Count();
            Client model = new Client();
            model.Customer = Convert.ToInt64(customerid);
            model.ActionId = 1;
            model.Action = "Search";
            model.TrackNo = travyoReq.Descendants("TransID").FirstOrDefault().Value;
            XDocument response = _DotwRepo.GetResponse(_dotWReq, model);
            string xmlouttype = string.Empty;
            try
            {
                if (dmc == "DOTW")
                {
                    xmlouttype = "false";
                }
                else
                { xmlouttype = "true"; }
            }
            catch { }
            bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);
            if (result)
            {
                //string CityId = _dotWReq.Descendants("city").FirstOrDefault().Value;
                string CityId = travyoReq.Descendants("CityID").FirstOrDefault().Value.DotWCity();


                //  string FilePath = HttpContext.Current.Server.MapPath(XmlPath + "Hotels/City-" + CityId + ".xml");
                string strPath = ConfigurationManager.AppSettings["DotWNPath"] + @"Hotels\City-" + CityId + ".xml";
                string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
                XDocument htlList = XDocument.Load(FilePath);
                var hotelResult = from htl in response.Descendants("hotel")
                                  join htldesc in htlList.Descendants("hotel")
                                  on htl.Attribute("hotelid").Value equals htldesc.Attribute("hotelid").Value
                                  select new XElement("Hotel",
                                              new XElement("HotelID", htl.Attribute("hotelid").Value),
                                              new XElement("HotelName", htldesc.Element("hotelName").Value),
                                              new XElement("PropertyTypeName", htldesc.Element("rating").Value == "48055" ? "Apartment" : "Hotel"),
                                              new XElement("CountryID", htldesc.Element("countryCode").Value),
                                              new XElement("CountryCode", htldesc.Element("countryCode").Value),
                                              new XElement("CountryName", htldesc.Element("countryName").Value),
                                              new XElement("CityId", htldesc.Element("cityCode").Value),
                                              new XElement("CityCode", htldesc.Element("cityCode").Value),
                                              new XElement("CityName", htldesc.Element("cityName").Value),
                                              new XElement("AreaId", htldesc.Element("locationId").Value),
                                              new XElement("AreaName", htldesc.Element("location").Value),
                                              new XElement("RequestID", null),
                                              new XElement("Address", HotelAddress(htldesc)),
                                              new XElement("Location", htldesc.Element("location").Value),
                                              new XElement("Description", htldesc.Element("description1").Element("language").Value + "\n" + htldesc.Element("description2").Element("language").Value),
                                              new XElement("StarRating", htldesc.Element("rating").Value.DotWRating()),
                                              new XElement("MinRate", Convert.ToDecimal(htl.Element("from").Element("formatted").Value) * noRoom),
                                              new XElement("HotelImgSmall", htldesc.Descendants("thumb").Count() != 0 ? htldesc.Descendants("thumb").FirstOrDefault().Value : ""),
                                              new XElement("HotelImgLarge", htldesc.Descendants("image").Count() != 0 ? htldesc.Descendants("image").FirstOrDefault().Element("url").Value : ""),
                                              new XElement("MapLink", ""),
                                              new XElement("Longitude", htldesc.Element("geoPoint").Element("lng").Value),
                                              new XElement("Latitude", htldesc.Element("geoPoint").Element("lat").Value),
                                               new XElement("xmloutcustid", customerid),
                                               new XElement("xmlouttype", xmlouttype),
                                              new XElement("DMC", dmc),
                                              new XElement("SupplierID", _credetials.Supplier),
                                              new XElement("Currency", response.Descendants("currencyShort").FirstOrDefault().Value),
                                              new XElement("Offers", ""),
                                              new XElement("Facilities", null)
                                      //FacilityTag(htldesc.DescendantsOrEmpty("leisureItem")), FacilityTag(htldesc.DescendantsOrEmpty("amenitieItem")), FacilityTag(htldesc.DescendantsOrEmpty("businessItem")))
                                              , new XElement("Rooms", ""));
                //RmSeaRespTag(htl.DescendantsOrEmpty("room"))
                _travyooResp = new XElement("searchResponse", new XElement("Hotels", hotelResult));
            }
            else
            {
                _travyooResp = new XElement("searchResponse", new XElement("Hotels", null));
            }

            return _travyooResp;

        }

        #endregion
        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region HotelDetail
        public XElement HtlDesReq(XElement Req)
        {
            try
            {
                travyoReq = Req;
                string CityId = Req.Descendants("CityID").FirstOrDefault().Value.DotWCity();
                string hotelId = Req.Descendants("HotelID").FirstOrDefault().Value;

                if (!string.IsNullOrEmpty(hotelId))
                {
                    string FilePath = HttpContext.Current.Server.MapPath(XmlPath + "Hotels/City-" + CityId + ".xml");
                    XDocument htlList = XDocument.Load(FilePath);
                    var htldesc = htlList.Descendants("hotel").Where(x => x.Attribute("hotelid").Value == hotelId).FirstOrDefault();
                    XElement _travyoResp;
                    _travyoResp = new XElement("hoteldescResponse",
                        new XElement("Hotels",
                            new XElement("Hotel",
                                new XElement("HotelID", hotelId),
                                new XElement("Address", HotelAddress(htldesc)),
                                new XElement("Description", htldesc.Element("description1").Element("language").Value + "\n" + htldesc.Element("description2").Element("language").Value),
                                HtlDesReqImageTag(htldesc.Element("images").DescendantsOrEmpty("image")),
                                new XElement("ContactDetails", new XElement("Phone", htldesc.Element("hotelPhone").Value), new XElement("Fax", "")),
                                new XElement("CheckinTime", htldesc.Element("hotelCheckIn").Value),
                                new XElement("CheckoutTime", htldesc.Element("hotelCheckOut").Value),
                                new XElement("Facilities", FacilityTag(htldesc.DescendantsOrEmpty("leisureItem")), FacilityTag(htldesc.DescendantsOrEmpty("amenitieItem")), FacilityTag(htldesc.DescendantsOrEmpty("businessItem"))))));

                    XElement SearReq = Req.Descendants("hoteldescRequest").FirstOrDefault();
                    SearReq.AddAfterSelf(_travyoResp);

                }
                else
                {
                    XElement SearReq = Req.Descendants("hoteldescRequest").FirstOrDefault();
                    SearReq.AddAfterSelf(new XElement("hoteldescResponse", new XElement("ErrorTxt", "No detail found")));
                }

            }
            catch (Exception ex)
            {

                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "HtlDesReq";
                custEx.PageName = "DotwService";
                //custEx.CustomerID = travyoReq.Descendants("CustomerID").FirstOrDefault().Value;
                //custEx.TranID = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);

                XElement SearReq = Req.Descendants("hoteldescRequest").FirstOrDefault();
                SearReq.AddAfterSelf(new XElement("hoteldescResponse", new XElement("ErrorTxt", "No detail found")));

            }

            return Req;

        }

        public XElement HtlDesReqImageTag(IEnumerable<XElement> imgList)
        {
            XElement mgItem;
            if (!imgList.IsNullOrEmpty())
            {

                var result = from itm in imgList
                             select new XElement("Image", new XAttribute("Path", itm.Element("url").Value), new XAttribute("Caption", itm.Element("category").Value));
                mgItem = new XElement("Images", result);
            }
            else
            {
                mgItem = new XElement("Images", null);
            }
            return mgItem;

        }

        #endregion

        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region RoomSearch
        public XElement GetRoomAvail_DOTWOUT(XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            XElement getrm = null;
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "5").ToList();
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
                        dmc = "DOTW";
                    }
                    DotwService dotwObj = new DotwService(customerid);
                    roomavailabilityresponse.Add(dotwObj.RoomSearchReq(req, htlid, dmc, customerid));
                }
                #endregion
                getrm = new XElement("TotalRooms", roomavailabilityresponse);
                return getrm;
            }
            catch { return null; }
        }
        public XElement RoomSearchReq(XElement Req, string htlid, string xtype, string custoid)
        {
            try
            {
                customerid = custoid;
                dmc = xtype;
                HtlCode = htlid;
                travyoReq = Req;
                XDocument dowReq = new XDocument(
                       new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement("customer",
                           new XElement("username", _credetials.UserName),
                           new XElement("password", _credetials.Password),
                           new XElement("id", _credetials.Id),
                           new XElement("source", _credetials.Source),
                           new XElement("product", _credetials.Service),
                           new XElement("request",
                               new XAttribute("command", "getrooms"),
                                new XElement("bookingDetails",
                                    new XElement("fromDate", Req.Descendants("FromDate").FirstOrDefault().DotWDate()),
                                    new XElement("toDate", Req.Descendants("ToDate").FirstOrDefault().DotWDate()),
                                    new XElement("currency", _credetials.Currency),
                                    RmSeaReqTag(Req.Descendants("RoomPax")),
                                new XElement("productId", HtlCode)))));
                XElement dowResp = RmSeaResp(dowReq);
                XElement SearReq = Req.Descendants("searchRequest").FirstOrDefault();
                SearReq.AddAfterSelf(dowResp);
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "RoomSearchReq";
                custEx.PageName = "DotwService";
                custEx.CustomerID = customerid;
                custEx.TranID = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
                XElement SearReq = Req.Descendants("searchRequest").FirstOrDefault();
                SearReq.AddAfterSelf(new XElement("searchResponse", new XElement("Hotels", "No Hotel Find")));
            }

            return Req;
        }
        public XElement RmSeaReqTag(IEnumerable<XElement> rmlst)
        {
            int counter = 0;
            XElement rmItem;
            if (rmlst != null)
            {
                var result = from room in rmlst
                             select new XElement("room", new XAttribute("runno", counter++),
                                         new XElement("adultsCode", room.Element("Adult").Value),
                                       RmSeaReqChildTag(room.DescendantsOrEmpty("ChildAge")),
                                         new XElement("rateBasis", "-1"),
                                         new XElement("passengerNationality", travyoReq.Descendants("PaxNationality_CountryID").FirstOrDefault().Value.DotWCountry()),
                                           new XElement("passengerCountryOfResidence", travyoReq.Descendants("PaxResidenceID").FirstOrDefault().Value.DotWCountry()));
                rmItem = new XElement("rooms", new XAttribute("no", rmlst.ToList().Count), result);
            }
            else
            {
                rmItem = new XElement("rooms", new XAttribute("no", 0));
            }
            return rmItem;

        }
        public XElement RmSeaReqChildTag(IEnumerable<XElement> chldlst)
        {
            int counter = 0;
            XElement chlItem;
            if (!chldlst.IsNullOrEmpty())
            {
                var result = from chld in chldlst
                             let counItem = 0
                             select new XElement("child", new XAttribute("runno", counter++), chld.Value);
                chlItem = new XElement("children", new XAttribute("no", chldlst.Count()), result);
            }
            else
            {
                chlItem = new XElement("children", new XAttribute("no", 0));
            }
            return chlItem;
        }
        //////////////////////////////////RoomSearchResponse////////////////////
        public XElement RmSeaResp(XDocument _dotWReq)
        {
            XElement _travyoResp;

            Client model = new Client();
            model.Customer = Convert.ToInt64(customerid);
            model.TrackNo = travyoReq.Descendants("TransID").FirstOrDefault().Value;
            model.ActionId = 2;
            model.Action = "RoomAvail";
            XDocument response = _DotwRepo.GetResponse(_dotWReq, model);
            bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);
            if (result)
            {
                //string Filepath = HttpContext.Current.Server.MapPath(XmlPath + "Rooms/rm" + response.Root.Attribute("tID").Value + ".xml");
                
                string CityId = travyoReq.Descendants("CityID").FirstOrDefault().Value.DotWCity();
                string hotelId = HtlCode;
                //string htlPath = HttpContext.Current.Server.MapPath(XmlPath + "Hotels/City-" + CityId + ".xml");
                //XDocument htlList = XDocument.Load(htlPath);
                //var htldesc = htlList.Descendants("hotel").Where(x => x.Attribute("hotelid").Value == hotelId).FirstOrDefault();
                CrncyCode = response.Descendants("currencyShort").FirstOrDefault().Value;
                _travyoResp = new XElement("searchResponse",
                    new XElement("Hotels",
                        new XElement("Hotel",
                            new XElement("HotelID", response.Descendants("hotel").FirstOrDefault().Attribute("id").Value),
                            new XElement("HotelName", response.Descendants("hotel").FirstOrDefault().Attribute("name").Value),
                    //new XElement("PropertyTypeName", htldesc.Element("rating").Value == "48055" ? "Apartment" : "Hotel"),
                    //new XElement("CountryID", htldesc.Element("countryCode").Value),
                    //new XElement("CountryCode", htldesc.Element("countryCode").Value),
                    //new XElement("CountryName", htldesc.Element("countryName").Value),
                    //new XElement("CityId", htldesc.Element("cityCode").Value),
                    //new XElement("CityCode", htldesc.Element("cityCode").Value),
                    //new XElement("CityName", htldesc.Element("cityName").Value),
                    //new XElement("AreaId", htldesc.Element("locationId").Value),
                    //new XElement("AreaName", htldesc.Element("location").Value),
                    //new XElement("RequestID", null),
                    //new XElement("Address", HotelAddress(htldesc)),
                    //new XElement("Location", htldesc.Element("location").Value),
                    //new XElement("Description", htldesc.Element("description1").Element("language").Value + "\n" + htldesc.Element("description2").Element("language").Value),
                    //new XElement("StarRating", htldesc.Element("rating").Value.DotWRating()),
                    //new XElement("MinRate", 0.0),
                    //new XElement("HotelImgSmall", htldesc.Descendants("thumb").Count() != 0 ? htldesc.Descendants("thumb").FirstOrDefault().Value : ""),
                    //new XElement("HotelImgLarge", htldesc.Descendants("image").Count() != 0 ? htldesc.Descendants("image").FirstOrDefault().Element("url").Value : ""),
                    //new XElement("MapLink", ""),
                    //new XElement("Longitude", htldesc.Element("geoPoint").Element("lng").Value),
                    //new XElement("Latitude", htldesc.Element("geoPoint").Element("lat").Value),
                            new XElement("DMC", dmc),
                            new XElement("SupplierID", _credetials.Supplier),
                            new XElement("Currency", response.Descendants("currencyShort").FirstOrDefault().Value),
                            new XElement("Offers", ""),
                            new XElement("Facilities", null),
                            new XElement("Rooms", RmSeaRespTag(response.Descendants("room"), response.Root.Attribute("tID").Value)))));
                //_travyoResp.Save(Filepath);
                //_travyoResp.Descendants("CancellationPolicies").Remove();
                foreach (var item in _travyoResp.Descendants("Room"))
                {
                    item.Add(new XElement("CancellationPolicy", null));
                }
            }
            else
            {
                _travyoResp = new XElement("searchResponse",
                    new XElement("Hotels", null));
            }
            return _travyoResp;
        }
        public IEnumerable<XElement> RmSeaRespTag(IEnumerable<XElement> rmlst, string transId)
        {


            int night = Convert.ToInt16(travyoReq.Descendants("Nights").FirstOrDefault().Value);
            IEnumerable<XElement> rooms = null;
            if (!rmlst.IsNullOrEmpty())
            {
                int cxlCount = 1;
                List<List<RoomItem>> roomList = new List<List<RoomItem>>();
                foreach (XElement item in rmlst)
                {
                    var itemList = from x in item.Descendants("roomType")
                                   from y in x.Descendants("rateBasis").
                                   Where(m => (m.Element("onRequest").Value == "0" && m.Element("isBookable").Value == "yes") && m.Element("minStay").GetMinstay(night))
                                   select new RoomItem
                                   {
                                       roomId = Convert.ToInt64(x.Attribute("roomtypecode").Value),
                                       roomKey = y.Element("allocationDetails").Value,
                                       mealId = Convert.ToInt16(y.Attribute("id").Value),
                                       roomRate = Convert.ToDecimal(y.Element("total").Element("formatted").Value),
                                       refundable = (y.Element("rateType").Attribute("nonrefundable") != null ? (y.Element("rateType").Attribute("nonrefundable").Value == "yes" ? true : false) : false)
                                   };
                    itemList = itemList.GroupBy(y => new { y.roomId, y.mealId }).Select(x => x.First(z => z.roomRate == x.Min(p => p.roomRate)));
                    roomList.Add(itemList.ToList());
                }

                var allGroups = CommonHelper.CrossJoinLists(roomList);
                var rmGroups = from y in allGroups.Where(p => p.All(o => o.mealId.Equals(p[0].mealId)) && p.All(o => o.refundable.Equals(p[0].refundable)))
                               select new RoomType
                               {
                                   groupKey = y.Select(x => x.roomId).GroupKey(y.First().mealId),
                                   mealId = y.First().mealId,
                                   groupRate = y.Sum(z => z.roomRate),
                                   refundable = y.First().refundable,
                                   roomKeys = y.Select(x => x.roomKey).ToList()
                               };
                rmGroups = rmGroups.GroupBy(y => y.groupKey).Select(x => x.First(z => z.groupRate == x.Min(p => p.groupRate)));

                var results = from p in rmGroups
                              group p by p.mealId into g
                              select new { mealId = g.Key, roomList = g.OrderBy(x => x.groupRate).Take(g.Count() > 50 ? 50 : g.Count()).ToList() };

                var groupResult = results.SelectMany(x => x.roomList).ToList();

                int counter = 0;
                rooms = from t in groupResult
                        select new XElement("RoomTypes",
                            new XAttribute("Index", counter++), new XAttribute("FileNo", transId),
                            new XAttribute("HtlCode", HtlCode), new XAttribute("CrncyCode", CrncyCode), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                            new XAttribute("TotalRate", t.groupRate),
                            (from y in rmlst.Descendants("rateBasis")
                             join z in t.roomKeys on y.Element("allocationDetails").Value equals z
                             select new XElement("Room",
                                 new XAttribute("ID", y.Parent.Parent.Attribute("roomtypecode").Value),
                                 new XElement("RequestID", y.Element("allocationDetails").Value),
                                 new XAttribute("SuppliersID", _credetials.Supplier),
                                 new XAttribute("RoomSeq", Convert.ToInt16(y.Parent.Parent.Parent.Attribute("runno").Value) + 1),
                                 new XAttribute("SessionID", transId + "_" + cxlCount++),
                                 new XAttribute("RoomType", y.Parent.Parent.Element("name").Value),
                                 new XAttribute("OccupancyID", y.Descendants("validForOccupancy").Count() > 0 ? y.Descendants("changedOccupancy").FirstOrDefault().Value : string.Empty),
                                 new XAttribute("OccupancyName", (y.Descendants("validForOccupancy").Count() > 0 && y.DescendantsOrEmpty("extraBed").FirstOrDefault().Value != "0") ? "Extra Bed for " + y.Element("validForOccupancy").Descendants("extraBedOccupant").FirstOrDefault().Value : string.Empty),
                                 new XAttribute("MealPlanID", y.Attribute("id").Value),
                                 new XAttribute("MealPlanName", y.Attribute("description").Value),
                                 new XAttribute("MealPlanCode", y.Attribute("id").Value.travyoMealType()),
                                 new XAttribute("MealPlanPrice", ""),
                                 new XAttribute("PerNightRoomRate", Convert.ToDecimal(y.Element("total").Element("formatted").Value) / Convert.ToInt32(y.Element("dates").Attribute("count").Value)),
                                 new XAttribute("TotalRoomRate", y.Element("total").Element("formatted").Value),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", y.Element("isBookable").Value == "yes" ? true : false),
                                 new XElement("Offers", ""),
                                 new XElement("nonrefundable", t.refundable),
                                 RoomPromotion(y.Parent.Parent.Element("specials").DescendantsOrEmpty("special"), y.DescendantsOrEmpty("special")),
                                 //RoomCxlPolicy(y.Element("cancellationRules").DescendantsOrEmpty("rule"), y.Element("withinCancellationDeadline").GetValueOrDefault("no"), y.Element("total").Element("formatted").Value),
                                 new XElement("Amenities", new XElement("Amenity")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 SupplementTag(y.DescendantsOrEmpty("includedSupplement"), y.DescendantsOrEmpty("includedAdditionalService")),
                                 RmSeaRespPriceTag(y.Descendants("date")),
                                 new XElement("AdultNum", y.Parent.Parent.Parent.Attribute("adults").Value),
                                 new XElement("ChildNum", y.Parent.Parent.Parent.Attribute("children").Value))));

            }
            else
            {
                rooms = new List<XElement>() { new XElement("RoomTypes", null) };

            }
            return rooms;
        }
        public IEnumerable<XElement> RmSeaRespTag_old(IEnumerable<XElement> rmlst, string transId)
        {
            int night = Convert.ToInt16(travyoReq.Descendants("Nights").FirstOrDefault().Value);
            IEnumerable<XElement> rooms;
            if (!rmlst.IsNullOrEmpty())
            {
                int cxlCount = 1;
                List<List<XElement>> roomList = new List<List<XElement>>();
                foreach (XElement item in rmlst)
                {
                    var itemList = from x in item.Descendants("roomType")
                                   from y in x.Descendants("rateBasis").
                                   Where(m => (m.Element("onRequest").Value == "0" && m.Element("isBookable").Value == "yes") && m.Element("minStay").GetMinstay(night))
                                   select new XElement("Room",
                                        new XAttribute("ID", x.Attribute("roomtypecode").Value),
                                          new XElement("nonrefundable", (y.Element("rateType").Attribute("nonrefundable") != null ? (y.Element("rateType").Attribute("nonrefundable").Value == "yes" ? true : false) : false)),
                                        new XElement("RequestID", y.Element("allocationDetails").Value),
                                           new XAttribute("MealPlanID", y.Attribute("id").Value),
                                              new XAttribute("TotalRoomRate", y.Element("total").Element("formatted").Value));
                    roomList.Add(itemList.ToList());
                }
                var rmGroups = CommonHelper.CrossJoinLists(roomList);
                rooms = from grp in rmGroups.Where(p => p.All(o => o.Attribute("MealPlanID").Value.Equals(p[0].Attribute("MealPlanID").Value)) &&
                                p.All(o => o.Element("nonrefundable").Value.Equals(p[0].Element("nonrefundable").Value)))
                        let keyItem = grp.Attributes("ID").GroupKey(grp.Attributes("MealPlanID").FirstOrDefault().Value)
                        select new XElement("RoomTypes", new XAttribute("roomKey", keyItem), new XAttribute("TotalRoomRate", grp.Sum(p => Convert.ToDecimal(p.Attribute("TotalRoomRate").Value))), grp);
                rooms = rooms.GroupBy(x => x.Attribute("roomKey").Value).Select(x => x.First()).Take(200);
                int counter = 0;
                rooms = from t in rooms
                        select new XElement("RoomTypes",
                            new XAttribute("Index", counter++), new XAttribute("FileNo", transId),
                            new XAttribute("HtlCode", HtlCode), new XAttribute("CrncyCode", CrncyCode), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                            new XAttribute("TotalRate", t.Attribute("TotalRoomRate").Value),
                            (from x in t.Descendants("Room")
                             join y in rmlst.Descendants("rateBasis") on x.Element("RequestID").Value equals y.Element("allocationDetails").Value
                             select new XElement("Room",
                                 new XAttribute("ID", x.Attribute("ID").Value),
                                 new XElement("RequestID", y.Element("allocationDetails").Value),
                                 new XAttribute("SuppliersID", _credetials.Supplier),
                                 new XAttribute("RoomSeq", Convert.ToInt16(y.Parent.Parent.Parent.Attribute("runno").Value) + 1),
                                 new XAttribute("SessionID", transId + "_" + cxlCount++),
                                 new XAttribute("RoomType", y.Parent.Parent.Element("name").Value),
                                 new XAttribute("OccupancyID", y.Descendants("validForOccupancy").Count() > 0 ? y.Descendants("changedOccupancy").FirstOrDefault().Value : string.Empty),
                                 new XAttribute("OccupancyName", (y.Descendants("validForOccupancy").Count() > 0 && y.DescendantsOrEmpty("extraBed").FirstOrDefault().Value != "0") ? "Extra Bed for " + y.Element("validForOccupancy").Descendants("extraBedOccupant").FirstOrDefault().Value : string.Empty),
                                 new XAttribute("MealPlanID", y.Attribute("id").Value),
                                 new XAttribute("MealPlanName", y.Attribute("description").Value),
                                 new XAttribute("MealPlanCode", y.Attribute("id").Value.travyoMealType()),
                                 new XAttribute("MealPlanPrice", ""),
                                 new XAttribute("PerNightRoomRate", Convert.ToDecimal(y.Element("total").Element("formatted").Value) / Convert.ToInt32(y.Element("dates").Attribute("count").Value)),
                                 new XAttribute("TotalRoomRate", y.Element("total").Element("formatted").Value),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", y.Element("isBookable").Value == "yes" ? true : false),
                                 new XElement("Offers", ""),
                                 new XElement("nonrefundable", (y.Element("rateType").Attribute("nonrefundable") != null ? (y.Element("rateType").Attribute("nonrefundable").Value == "yes" ? true : false) : false)),
                                 RoomPromotion(x.Element("specials").DescendantsOrEmpty("special"), y.DescendantsOrEmpty("special")),
                                 RoomCxlPolicy(y.Element("cancellationRules").DescendantsOrEmpty("rule"), y.Element("withinCancellationDeadline").GetValueOrDefault("no"), y.Element("total").Element("formatted").Value),
                                 new XElement("Amenities", new XElement("Amenity")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements", ""),
                                 RmSeaRespPriceTag(y.Descendants("date")),
                                 new XElement("AdultNum", y.Parent.Parent.Parent.Attribute("adults").Value),
                                 new XElement("ChildNum", y.Parent.Parent.Parent.Attribute("children").Value))));

            }
            else
            {
                rooms = new List<XElement>() { new XElement("RoomTypes", null) };

            }
            return rooms;
        }















        public XElement RmSeaRespAmenity(IEnumerable<XElement> Amtylst)
        {
            XElement AmtyItem;
            if (!Amtylst.IsNullOrEmpty())
            {
                var result = from amty in Amtylst
                             select new XElement("Amenity", amty.Value);
                AmtyItem = new XElement("Amenities", result);
            }
            else
            {
                AmtyItem = new XElement("Amenities", new XElement("Amenity", null));
            }
            return AmtyItem;
        }
        public XElement RmSeaRespPriceTag(IEnumerable<XElement> datelst)
        {
            int counter = 0;
            XElement chlItem;
            if (!datelst.IsNullOrEmpty())
            {

                var result = from prc in datelst
                             let counItem = 0
                             select new XElement("Price", new XAttribute("Night", ++counter), new XAttribute("PriceValue", Convert.ToDecimal(prc.Element("price").FirstNode.ToString())));
                chlItem = new XElement("PriceBreakups", result);
            }
            else
            {
                chlItem = new XElement("PriceBreakups", null);
            }
            return chlItem;
        }
        #endregion

        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region CxlPolicy
        public XElement CxlReq(XElement Req)
        {
            Client model = new Client();
            APILogDetail log = new APILogDetail();
            var startTime = DateTime.Now;
            try
            {
                log.logrequestXML = Req.ToString();
                model.Customer = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                model.ActionId = 2;
                model.Action = "RoomAvail";
                model.TrackNo = Req.Descendants("TransID").FirstOrDefault().Value;
                model.LogTypeId = 2;
                XElement roomKeys = new XElement("roomKeys", Req.Descendants("Room").Select(x => new XElement("roomKey", x.Element("RequestID").Value)));
                string response = _DotwRepo.GetRoomPrice(model, roomKeys.ToString());
                var xResponse = XDocument.Parse(response);

                if (xResponse != null)
                {
                    var suplReq = Req.Descendants("RoomTypes").FirstOrDefault();
                    var rooms = from req in suplReq.Descendants("Room")
                                join rate in xResponse.Descendants("rateBasis") on req.Element("RequestID").Value equals rate.Element("allocationDetails").Value
                                select new XElement("Room",
                                    new XAttribute("ID", req.Attribute("ID").Value),
                                    new XAttribute("SuppliersID", _credetials.Supplier),
                                      RoomCxlPolicy(rate.Element("cancellationRules").DescendantsOrEmpty("rule"),
                                      rate.Element("withinCancellationDeadline").GetValueOrDefault("no"),
                                     rate.Element("total").Element("formatted").Value));
                    var cxlPolicy = MergCxlPolicy(rooms.ToList());
                    XElement _travyoResp;
                    _travyoResp = new XElement("HotelDetailwithcancellationResponse",
                        new XElement("Hotels",
                            new XElement("Hotel",
                                new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                new XElement("HotelName", null),
                                new XElement("HotelImgSmall", null),
                                new XElement("HotelImgLarge", null),
                                new XElement("MapLink", null),
                                new XElement("DMC", "Dotw"),
                                new XElement("Rooms", new XElement("Room", cxlPolicy)))));
                    XElement SearReq = Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
                    SearReq.AddAfterSelf(_travyoResp);
                }
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "CxlReq";
                custEx.PageName = "DotwService";
                custEx.CustomerID = travyoReq.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
                XElement SearReq = Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
                SearReq.AddAfterSelf(new XElement("HotelDetailwithcancellationResponse", new XElement("ErrorTxt", "No detail found")));
            }
            finally
            {
                SaveAPILog savelog = new SaveAPILog();
                log.customerID = model.Customer;
                log.LogTypeID = 3;
                log.LogType = "CXLPolicy";
                log.SupplierID = 5;
                log.TrackNumber = model.TrackNo;
                log.logresponseXML = Req.ToString();
                log.StartTime = startTime;
                log.EndTime = DateTime.Now;
                savelog.SaveAPILogs(log);
            }
            return Req;
        }
        public XElement CxlReq_Old(XElement Req)
        {
            APILogDetail log = new APILogDetail();
            try
            {
                travyoReq = Req;
                Client model = new Client();
                model.Customer = Convert.ToInt64(Req.Descendants("CustomerID").FirstOrDefault().Value);
                model.ActionId = 3;
                model.Action = "CXLPolicy";
                model.TrackNo = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                log.TrackNumber = model.TrackNo;
                log.customerID = model.Customer;
                log.LogTypeID = model.ActionId;
                log.LogType = model.Action;
                log.SupplierID = 0;
                log.logrequestXML = Req.ToString();

                string str = Req.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value;
                string[] strArray = str.Split('_').ToArray();
                string FileId = strArray[0];

                string RoomId = Req.Descendants("RoomTypes").FirstOrDefault().Attribute("Index").Value;
                if (!string.IsNullOrEmpty(FileId))
                {
                    string FilePath = HttpContext.Current.Server.MapPath(XmlPath + "Rooms/rm" + FileId + ".xml");
                    XDocument htlList = XDocument.Load(FilePath);
                    var rmtyp = htlList.Descendants("RoomTypes").Where(x => x.Attribute("Index").Value == RoomId).FirstOrDefault();
                    var cxlPolicy = MergCxlPolicy(rmtyp.Descendants("Room").ToList());
                    var cxlRoom = rmtyp.Descendants("Room").FirstOrDefault();
                    cxlRoom.Descendants().Remove();
                    cxlRoom.Descendants("CancellationPolicies").Remove();
                    cxlRoom.Add(cxlPolicy);

                    XElement _travyoResp;
                    _travyoResp = new XElement("HotelDetailwithcancellationResponse",
                        new XElement("Hotels",
                            new XElement("Hotel",
                                new XElement("HotelID", Req.Descendants("HotelID").FirstOrDefault().Value),
                                new XElement("HotelName", null),
                                new XElement("HotelImgSmall", null),
                                new XElement("HotelImgLarge", null),
                                new XElement("MapLink", null),
                                new XElement("DMC", "Dotw"),
                                new XElement("Rooms", cxlRoom))));

                    XElement SearReq = Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
                    SearReq.AddAfterSelf(_travyoResp);
                    log.logresponseXML = _travyoResp.ToString();
                    return Req;

                }

            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "CxlReq";
                custEx.PageName = "DotwService";
                custEx.CustomerID = travyoReq.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);


                XElement SearReq = Req.Descendants("hotelcancelpolicyrequest").FirstOrDefault();
                SearReq.AddAfterSelf(new XElement("HotelDetailwithcancellationResponse", new XElement("ErrorTxt", "No detail found")));
            }
            finally
            {


                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);

            }


            return Req;
        }
        #endregion

        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region PreBooking
        public XElement PreBookingSeaReq(XElement Req, string xmlout)
        {
            try
            {
                dmc = xmlout;
                travyoReq = Req;
                XDocument dowReq = new XDocument(
                     new XDeclaration("1.0", "utf-8", "yes"),
                     new XElement("customer",
                              new XElement("username", _credetials.UserName),
                        new XElement("password", _credetials.Password),
                          new XElement("id", _credetials.Id),
                            new XElement("source", _credetials.Source),
                                  new XElement("product", _credetials.Service),
                        new XElement("request",
                           new XAttribute("command", "getrooms"),
                          new XElement("bookingDetails",
                                new XElement("fromDate", Req.Descendants("FromDate").FirstOrDefault().DotWDate()),
                                new XElement("toDate", Req.Descendants("ToDate").FirstOrDefault().DotWDate()),
                                new XElement("currency", _credetials.Currency),
                                PreBookingSeaReqTag(Req.Descendants("Room")),
                              new XElement("productId", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value)))));


                XElement dowResp = PreBookingSeaResp(dowReq);
                XElement SearReq = Req.Descendants("HotelPreBookingRequest").FirstOrDefault();
                SearReq.AddAfterSelf(dowResp);

            }
            catch (Exception ex)
            {

                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "PreBookingSeaReq";
                custEx.PageName = "DotwService";
                custEx.CustomerID = travyoReq.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
                XElement SearReq = Req.Descendants("HotelPreBookingRequest").FirstOrDefault();
                SearReq.AddAfterSelf(new XElement("HotelPreBookingRequest", new XElement("ErrorTxt", "No detail found")));
            }
            return Req;
        }
        public XElement PreBookingSeaReqTag(IEnumerable<XElement> rmlst)
        {
            int counter = 0;
            XElement rmItem;
            if (rmlst != null)
            {
                var result = from room in rmlst
                             select new XElement("room", new XAttribute("runno", counter++),
                                         new XElement("adultsCode", room.Attribute("Adult").Value),
                                         PreBookingSeaReqChildTag(room.Attribute("ChildAge").Value),
                                         new XElement("rateBasis", room.Attribute("MealPlanCode").Value.DotWMealType()),
                                         new XElement("passengerNationality", travyoReq.Descendants("PaxNationality_CountryID").FirstOrDefault().Value.DotWCountry()),
                                           new XElement("passengerCountryOfResidence", travyoReq.Descendants("PaxResidenceID").FirstOrDefault().Value.DotWCountry()),
                                           new XElement("roomTypeSelected",
                                               new XElement("code", room.Attribute("ID").Value)
                                               , new XElement("selectedRateBasis", room.Attribute("MealPlanCode").Value.DotWMealType())
                                               , new XElement("allocationDetails", room.Element("RequestID").Value)
                                               ));

                rmItem = new XElement("rooms", new XAttribute("no", rmlst.ToList().Count), result);
            }
            else
            {
                rmItem = new XElement("rooms", new XAttribute("no", 0));
            }
            return rmItem;
        }
        public XElement PreBookingSeaReqChildTag(string chldStr)
        {
            XElement chlItem;
            if (!string.IsNullOrEmpty(chldStr))
            {
                int counter = 0;
                string[] chldlst = chldStr.Split(',').ToArray();
                var result = from chld in chldlst
                             let counItem = 0
                             select new XElement("child", new XAttribute("runno", counter++), chld);
                chlItem = new XElement("children", new XAttribute("no", chldlst.Length), result);
            }
            else
            {
                chlItem = new XElement("children", new XAttribute("no", 0));
            }
            return chlItem;

        }

        /////////////////////PreBooking Response////////////////////
        public XElement PreBookingSeaResp(XDocument _dotWReq)
        {
            XElement _travyooResp;
            Client model = new Client();
            model.Customer = Convert.ToInt64(travyoReq.Descendants("CustomerID").FirstOrDefault().Value);
            model.ActionId = 4;
            model.Action = "PreBook";
            model.TrackNo = travyoReq.Descendants("TransID").FirstOrDefault().Value;
            model.LogTypeId = 2;
            XDocument response = _DotwRepo.GetPreResponse(_dotWReq, model);
            bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);
            if (result)
            {

                var suplReq = _dotWReq.Descendants("request").FirstOrDefault();
                int rmCount = 1;

                var rooms = from req in suplReq.Descendants("room")
                            join t in response.Descendants("room") on req.Attribute("runno").Value equals t.Attribute("runno").Value
                            from x in t.Descendants("roomType").Where(p => p.Attribute("roomtypecode").Value == req.Element("roomTypeSelected").Element("code").Value)
                            from y in x.Descendants("rateBasis").Where(p => p.Element("allocationDetails").Value == req.Element("roomTypeSelected").Element("allocationDetails").Value)
                            select new XElement("Room",
                                new XAttribute("ID", x.Attribute("roomtypecode").Value),
                                new XAttribute("SuppliersID", _credetials.Supplier),
                                new XAttribute("RoomSeq", rmCount++),
                                new XAttribute("SessionID", ""),
                                new XAttribute("RoomType", x.Element("name").Value),
                                new XAttribute("OccupancyID", y.Descendants("validForOccupancy").Count() > 0 ? y.Descendants("changedOccupancy").FirstOrDefault().Value : string.Empty),
                                new XAttribute("OccupancyName", (y.Descendants("validForOccupancy").Count() > 0 && y.DescendantsOrEmpty("extraBed").FirstOrDefault().Value != "0") ?
                                      "Extra Bed for " + y.Element("validForOccupancy").Descendants("extraBedOccupant").FirstOrDefault().Value : string.Empty),
                                  new XAttribute("MealPlanID", y.Attribute("id").Value),
                                  new XAttribute("MealPlanName", y.Attribute("description").Value),
                                  new XAttribute("MealPlanCode", y.Attribute("id").Value.travyoMealType()),
                                  new XAttribute("MealPlanPrice", ""),
                                  new XAttribute("PerNightRoomRate", Convert.ToDecimal(y.Element("total").Element("formatted").Value) / Convert.ToInt32(y.Element("dates").Attribute("count").Value)),
                                  new XAttribute("TotalRoomRate", y.Element("total").Element("formatted").Value),
                                  new XAttribute("CancellationDate", ""),
                                  new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", y.Element("isBookable").Value == "yes" ? true : false),
                                  new XElement("RequestID", y.Element("allocationDetails").Value),
                                  new XElement("Offers", ""),
                                  new XElement("tariffNotes", Notes(x.Element("name").Value, y.Element("tariffNotes").Value)),
                                  RoomPromotion(x.Element("specials").DescendantsOrEmpty("special"), y.DescendantsOrEmpty("special")),
                                  RoomCxlPolicy(y.Element("cancellationRules").DescendantsOrEmpty("rule"),
                                  y.Element("withinCancellationDeadline").GetValueOrDefault("no"),
                                  y.Element("total").Element("formatted").Value),
                                  RoomRestriction(y.Element("cancellationRules").DescendantsOrEmpty("rule"), x.Element("name").Value),
                                  new XElement("Amenities", new XElement("Amenity")),
                                  new XElement("Images", ""),
                                  SupplementTag(y.DescendantsOrEmpty("includedSupplement"), y.DescendantsOrEmpty("includedAdditionalService")),
                                  PreBookingSeaRespPriceTag(y.Descendants("date")),
                                  new XElement("AdultNum", t.Attribute("adults").Value),
                                  new XElement("ChildNum", t.Attribute("children").Value));

                List<XElement> NotesList = new List<XElement>();
                NotesList = MergeRestriction(rooms.Descendants("Restriction"));
                if (NotesList.Count > 0)
                {
                    NotesList = NotesList.Concat(rooms.Descendants("tariffNotes").ToList()).ToList();
                }
                else
                {
                    NotesList = rooms.Descendants("tariffNotes").ToList();
                }
                _travyooResp = new XElement("HotelPreBookingResponse",
                    new XElement("NewPrice", string.Empty),
                    new XElement("Hotels",
                        new XElement("Hotel",
                            new XElement("HotelID", response.Descendants("hotel").FirstOrDefault().Attribute("id").Value),
                            new XElement("HotelName", response.Descendants("hotel").FirstOrDefault().Attribute("name").Value),
                            new XElement("TermCondition", HotelPolicy(NotesList)),
                            new XElement("Status", true),
                            new XElement("HotelImgSmall", null),
                            new XElement("HotelImgLarge", null),
                            new XElement("MapLink", null),
                            new XElement("DMC", dmc),
                            new XElement("Currency", response.Descendants("currencyShort").FirstOrDefault().Value),
                            new XElement("Offers", null),
                            new XElement("Rooms",
                                new XElement("RoomTypes", new XAttribute("Index", 1), new XAttribute("TotalRate", rooms.Sum(p => Convert.ToDecimal(p.Attribute("TotalRoomRate").Value))), rooms)))));

                var cxlPolicy = MergCxlPolicy(_travyooResp.Descendants("Room").ToList());

                //var cxl = _travyooResp.Descendants("CancellationPolicies").FirstOrDefault();
                _travyooResp.Descendants("CancellationPolicies").Remove();
                _travyooResp.Descendants("tariffNotes").Remove();
                _travyooResp.Descendants("Room").FirstOrDefault().AddBeforeSelf(cxlPolicy);
                foreach (var item in _travyooResp.Descendants("Room"))
                {
                    item.Add(new XElement("CancellationPolicy", null));
                }


            }
            else
            {
                _travyooResp = new XElement("HotelPreBookingResponse", new XElement("ErrorTxt", "No detail found"));
            }
            return _travyooResp;

        }

        public XElement PreBookingSeaRespPriceTag(IEnumerable<XElement> datelst)
        {
            int counter = 0;
            XElement chlItem;
            if (datelst != null)
            {

                var result = from prc in datelst
                             let counItem = 0
                             select new XElement("Price", new XAttribute("Night", ++counter), new XAttribute("PriceValue", prc.Element("freeStay").Value != "yes" ? Convert.ToDecimal(prc.Element("price").FirstNode.ToString()).ToString() : "0"));
                chlItem = new XElement("PriceBreakups", result);
            }
            else
            {
                chlItem = new XElement("PriceBreakups", null);
            }
            return chlItem;

        }

        public XElement PreBookingSeaRespAmenity(IEnumerable<XElement> Amtylst)
        {
            XElement AmtyItem;
            if (Amtylst != null)
            {
                var result = from amty in Amtylst
                             select new XElement("Amenity", amty.Value);
                AmtyItem = new XElement("Amenities", result);
            }
            else
            {
                AmtyItem = new XElement("Amenities", new XElement("Amenity", null));

            }
            return AmtyItem;
        }

        #endregion
        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region ConfirmBooking

        ////////////////////////////////////////////////////////////PreConfBookingRequest/////////////////////////////////////////////////////
        bool PreBookWithConfirmReq(XElement Req)
        {
            bool flag = false;
            try
            {

                price = travyoReq.Descendants("Room").Sum(x => x.Attribute("TotalRoomRate").AttributetoDecimal());
                XDocument dowReq = new XDocument(
                     new XDeclaration("1.0", "utf-8", "yes"),
                     new XElement("customer",
                              new XElement("username", _credetials.UserName),
                        new XElement("password", _credetials.Password),
                          new XElement("id", _credetials.Id),
                            new XElement("source", _credetials.Source),
                                  new XElement("product", _credetials.Service),
                        new XElement("request",
                           new XAttribute("command", "getrooms"),
                          new XElement("bookingDetails",
                                new XElement("fromDate", Req.Descendants("FromDate").FirstOrDefault().DotWDate()),
                                new XElement("toDate", Req.Descendants("ToDate").FirstOrDefault().DotWDate()),
                                new XElement("currency", _credetials.Currency),
                                PreBookWithRoomSeaTag(Req.Descendants("Room")),
                              new XElement("productId", Req.Descendants("HotelID").FirstOrDefault().Value)))));

                Client model = new Client();
                model.Customer = Convert.ToInt64(travyoReq.Descendants("CustomerID").FirstOrDefault().Value);
                model.ActionId = 5;
                model.Action = "PreBookConfirm";
                model.TrackNo = travyoReq.Descendants("TransactionID").FirstOrDefault().Value;
                XDocument response = _DotwRepo.GetResponse(dowReq, model);
                bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);
                if (result)
                {
                    string roomCount = response.Descendants("rooms").FirstOrDefault().Attribute("count").Value;
                    string blockCount = response.Descendants("rateBasis").Where(x => x.Element("status").Value == "checked").Count().ToString();

                    if (blockCount == roomCount)
                    {

                        decimal newPrice = response.Descendants("room").Descendants("rateBasis").Where(x =>
      x.Element("status").Value == "checked").Sum(x => x.Element("total").FirstNode.ToString().StringDecimal());

                        if (Math.Round(newPrice, 2) == Math.Round(price, 2))
                        {


                            foreach (var item in travyoReq.Descendants("Room"))
                            {
                                var xmlItem = response.Descendants("room").Where(x => x.Attribute("runno").Value == item.Attribute("DotwIndex").Value).FirstOrDefault().Descendants("rateBasis").Where(x =>
                                    x.Element("status").Value == "checked" && x.Parent.Parent.Attribute("roomtypecode").Value == item.Attribute("RoomTypeID").Value).FirstOrDefault();

                                string reqNo = xmlItem.Element("allocationDetails").Value;
                                item.Element("RequestID").SetValue(reqNo);
                                item.Add(new XElement("nonrefundable", (xmlItem.Element("rateType").Attribute("nonrefundable") != null ?
                                    (xmlItem.Element("rateType").Attribute("nonrefundable").Value == "yes" ? true : false) : false)));
                            }
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "PreBookWithConfirm";
                custEx.PageName = "DotwService";
                custEx.CustomerID = Req.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = Req.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
            }
            return flag;
        }
        XElement PreBookWithRoomSeaTag(IEnumerable<XElement> rmlst)
        {
            int counter = 0;
            XElement rmItem;
            if (rmlst != null)
            {
                var result = from room in rmlst
                             select new XElement("room", new XAttribute("runno", counter++),
                                         new XElement("adultsCode", room.Attribute("Adult").Value),
                                         PreBookingSeaReqChildTag(room.Attribute("ChildAge").Value),
                                         new XElement("rateBasis", room.Attribute("MealPlanID").Value),
                                         new XElement("passengerNationality", travyoReq.Descendants("PaxNationality_CountryID").FirstOrDefault().Value.DotWCountry()),
                                           new XElement("passengerCountryOfResidence", travyoReq.Descendants("PaxResidenceID").FirstOrDefault().Value.DotWCountry()),
                                           new XElement("roomTypeSelected",
                                               new XElement("code", room.Attribute("RoomTypeID").Value)
                                               , new XElement("selectedRateBasis", room.Attribute("MealPlanID").Value)
                                               , new XElement("allocationDetails", room.Element("RequestID").Value)
                                               ));
                rmItem = new XElement("rooms", new XAttribute("no", rmlst.ToList().Count), result);
            }
            else
            {
                rmItem = new XElement("rooms", new XAttribute("no", 0));
            }
            return rmItem;
        }

        ////////////////////////////////////////////////////////////BookingRequest/////////////////////////////////////////////////////
        public XElement CNFBookingSeaReq(XElement Req)
        {
            XElement dowResp;
            XElement SearReq = Req.Descendants("HotelBookingRequest").FirstOrDefault();
            try
            {

                int index = 0;
                foreach (var item in Req.Descendants("Room"))
                {
                    item.Add(new XAttribute("DotwIndex", index));
                    ++index;
                }
                travyoReq = Req;
                bool result = PreBookWithConfirmReq(Req);
                if (result)
                {
                    int rmCount = travyoReq.Descendants("Room").Count();
                    int nrefundable = travyoReq.Descendants("nonrefundable").Where(x => x.Value == "true").Count();
                    if (rmCount != nrefundable)
                    {
                        XDocument dowReq = new XDocument(
                                 new XDeclaration("1.0", "utf-8", "yes"),
                                 new XElement("customer",
                                    new XElement("username", _credetials.UserName),
                                      new XElement("password", _credetials.Password),
                                        new XElement("id", _credetials.Id),
                                          new XElement("source", _credetials.Source),
                                                new XElement("product", _credetials.Service),
                                    new XElement("request",
                                       new XAttribute("command", "confirmbooking"),
                                          new XElement("bookingDetails",
                                              new XElement("fromDate", Req.Descendants("FromDate").FirstOrDefault().DotWDate()),
                                              new XElement("toDate", Req.Descendants("ToDate").FirstOrDefault().DotWDate()),
                                              new XElement("currency", _credetials.Currency),
                                              new XElement("productId", Req.Descendants("HotelID").FirstOrDefault().Value),
                                              new XElement("customerReference", Req.Descendants("TransID").FirstOrDefault().Value),
                                              CNFBookingSeaReqRoomTag(travyoReq.Descendants("Room"))))));
                        dowResp = CNFBookingSeaResp(dowReq);
                    }
                    else
                    {
                        dowResp = SaveBookingReq(travyoReq);
                    }
                }
                else
                {
                    dowResp = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "Booking cann't be completed due to unavailablity of rooms. or change in price"));
                }
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "CNFBookingSeaReq";
                custEx.PageName = "DotwService";
                custEx.CustomerID = travyoReq.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = travyoReq.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
                dowResp = new XElement("HotelBookingResponse", new XElement("ErrorTxt", "No detail found"));
            }
            SearReq.AddAfterSelf(dowResp);
            return Req;
        }
        public XElement CNFBookingSeaReqRoomTag(IEnumerable<XElement> rmlst)
        {
            int counter = 0;
            XElement rmItem;
            if (rmlst != null)
            {
                var result = from room in rmlst
                             select new XElement("room", new XAttribute("runno", counter++),
                                 new XElement("roomTypeCode", room.Attribute("RoomTypeID").Value),
                                 new XElement("selectedRateBasis", room.Attribute("MealPlanID").Value),
                                 new XElement("allocationDetails", room.Element("RequestID").Value),
                                 CNFBookingSeaReqPaxTag(room).Elements(),
                                 new XElement("passengerNationality", travyoReq.Descendants("PaxNationality_CountryID").FirstOrDefault().Value.DotWCountry()),
                                 new XElement("passengerCountryOfResidence", travyoReq.Descendants("PaxResidenceID").FirstOrDefault().Value.DotWCountry()),
                                 CNFBookingSeaReqGuestTag(room.Descendants("PaxInfo")),
                                 new XElement("beddingPreference", "0"));

                rmItem = new XElement("rooms", new XAttribute("no", rmlst.ToList().Count), result);
            }
            else
            {
                rmItem = new XElement("rooms", new XAttribute("no", 0));
            }
            return rmItem;
        }

        public XElement CNFBookingSeaReqPaxTag(XElement room)
        {
            XElement rmItem;
            if (!string.IsNullOrEmpty(room.Attribute("OccupancyID").Value))
            {
                var rm = room.Attribute("OccupancyID").Value.Split(',').ToArray();
                string[] chldlstm;
                if (!string.IsNullOrEmpty(rm[2]))
                {
                    chldlstm = rm[2].Split('_').ToArray();

                }
                else
                {
                    chldlstm = null;
                }
                rmItem = new XElement("RoomTag",
                       new XElement("adultsCode", rm[0]),
                       new XElement("actualAdults", room.Attribute("Adult").Value),
                       CNFBookingSeaReqPaxChildTag(chldlstm),
                       CNFBookingSeaReqActualChildTag(room.Attribute("ChildAge").Value),
                       new XElement("extraBed", rm[3]));

            }
            else
            {
                rmItem = new XElement("RoomTag", new XElement("adultsCode", room.Attribute("Adult").Value),
                                  new XElement("actualAdults", room.Attribute("Adult").Value),
                                  CNFBookingSeaReqChildTag(room.Attribute("ChildAge").Value),
                                  CNFBookingSeaReqActualChildTag(room.Attribute("ChildAge").Value),
                                  new XElement("extraBed", 0));
            }

            return rmItem;
        }
        public XElement CNFBookingSeaReqChildTag(string chldStr)
        {
            XElement chlItem;
            if (!string.IsNullOrEmpty(chldStr))
            {
                int counter = 0;
                string[] chldlst = chldStr.Split(',').ToArray();
                var result = from chld in chldlst
                             let counItem = 0
                             select new XElement("child", new XAttribute("runno", counter++), chld);
                chlItem = new XElement("children", new XAttribute("no", chldlst.Length), result);

            }
            else
            {
                chlItem = new XElement("children", new XAttribute("no", 0));
            }
            return chlItem;
        }

        public XElement CNFBookingSeaReqPaxChildTag(string[] chldlst)
        {
            int counter = 0;
            XElement chlItem;
            if (chldlst != null)
            {
                var result = from chld in chldlst
                             let counItem = 0
                             select new XElement("child", new XAttribute("runno", counter++), chld);
                chlItem = new XElement("children", new XAttribute("no", chldlst.Length), result);
            }
            else
            {
                chlItem = new XElement("children", new XAttribute("no", 0));
            }
            return chlItem;
        }

        public XElement CNFBookingSeaReqActualChildTag(string chldStr)
        {

            XElement chlItem;
            if (!string.IsNullOrEmpty(chldStr))
            {
                int counter = 0;
                string[] chldlst = chldStr.Split(',').ToArray();
                var result = from chld in chldlst
                             let counItem = 0
                             select new XElement("actualChild", new XAttribute("runno", counter++), chld);
                chlItem = new XElement("actualChildren", new XAttribute("no", chldlst.Length), result);


            }
            else
            {
                chlItem = new XElement("actualChildren", new XAttribute("no", 0));
            }
            return chlItem;

        }
        public XElement CNFBookingSeaReqGuestTag(IEnumerable<XElement> guestlst)
        {
            XElement gstItem;
            if (guestlst != null)
            {

                var result = from gst in guestlst.Where(x => !string.IsNullOrEmpty(x.Element("FirstName").Value))
                             select new XElement("passenger", new XAttribute("leading", gst.Element("IsLead").Value.ToUpper() == "true".ToUpper() ? "yes" : "no"),
                                 new XElement("salutation", gst.Element("Title").Value.DotWTitle()),
                                 new XElement("firstName", gst.GuestName()),
                                 new XElement("lastName", gst.Element("LastName").Value.Trim()));
                gstItem = new XElement("passengersDetails", result);

            }
            else
            {
                gstItem = new XElement("passengersDetails", "Lead Guest Not Available");
            }
            return gstItem;

        }

        ////////////////////////////////////////////////////////////BookingResponse/////////////////////////////////////////////////////

        public XElement CNFBookingSeaResp(XDocument _dotWReq)
        {
            XElement _travyooResp;

            Client model = new Client();
            model.Customer = Convert.ToInt64(travyoReq.Descendants("CustomerID").FirstOrDefault().Value);
            model.ActionId = 5;
            model.Action = "Book";
            model.TrackNo = travyoReq.Descendants("TransactionID").FirstOrDefault().Value;
            XDocument response = _DotwRepo.GetResponse(_dotWReq, model);
            bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);

            if (result)
            {
                var Detail = new XElement("Hotels",
                                              new XElement("HotelID", travyoReq.Descendants("HotelID").FirstOrDefault().Value),
                                              new XElement("HotelName", ""),
                                              new XElement("FromDate", travyoReq.Descendants("FromDate").FirstOrDefault().Value),
                                              new XElement("ToDate", travyoReq.Descendants("ToDate").FirstOrDefault().Value),
                                              new XElement("AdultPax", _dotWReq.Descendants("adultsCode").Sum(x => Convert.ToInt16(x.Value))),//
                                              new XElement("ChildPax", _dotWReq.Descendants("children").Sum(x => Convert.ToInt16(x.Attribute("no").Value))),//
                                              new XElement("TotalPrice", response.Descendants("price").Sum(x => Convert.ToDecimal(x.Element("formatted").Value))),//
                                              new XElement("CurrencyID", travyoReq.Descendants("CurrencyID").FirstOrDefault().Value),
                                              new XElement("CurrencyCode", travyoReq.Descendants("CurrencyCode").FirstOrDefault().Value),
                                              new XElement("MarketID", ""),//
                                              new XElement("MarketName", ""),//              
                                              new XElement("HotelImgSmall", ""),
                                              new XElement("HotelImgLarge", ""),
                                              new XElement("MapLink", ""),//
                                              new XElement("VoucherRemark", ""),
                                              new XElement("TransID", travyoReq.Descendants("TransID").FirstOrDefault().Value),
                                              new XElement("ConfirmationNumber", response.Descendants("returnedCode").FirstOrDefault().Value),//
                                              new XElement("Status", response.Descendants("bookingStatus").Count() == response.Descendants("bookingStatus").Where(x => x.Value == "2").Count() ? "Success" : "Failed"),
                                              new XElement("PassengersDetail", CNFBookingSeaRespRoomTag(response))
                                              );
                _travyooResp = new XElement("HotelBookingResponse", Detail);

            }
            else
            {
                _travyooResp = new XElement("HotelBookingResponse",
                    new XElement("ErrorTxt", response.Descendants("details").FirstOrDefault().Value));

            }


            return _travyooResp;

        }

        public XElement CNFBookingSeaRespRoomTag(XDocument response)
        {
            XElement rmItem;
            if (response != null)
            {
                var result = from room in travyoReq.Descendants("Room")
                             join bok in response.Descendants("booking") on room.Attribute("DotwIndex").Value equals bok.Attribute("runno").Value
                             select new XElement("Room",
                                   new XAttribute("ID", room.Attribute("RoomTypeID").Value),
                                 new XAttribute("RoomType", room.Attribute("RoomType").Value),
                                 new XAttribute("ServiceID", bok.Element("bookingCode").Value),
                                 new XAttribute("RefNo", bok.Element("bookingReferenceNumber").Value),
                                 new XAttribute("MealPlanID", room.Attribute("MealPlanID").Value),
                                 new XAttribute("MealPlanName", ""),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", room.Attribute("MealPlanPrice").Value),
                                 new XAttribute("RoomStatus", bok.Element("bookingStatus").Value == "2" ? "confirmed" : "on request"),
                                 new XAttribute("TotalRoomRate", bok.Element("price").Element("formatted").Value),
                                 new XElement("RequestID", room.Element("RequestID").Value),
                                 new XElement("RoomGuest", room.Element("PaxInfo")),
                                 room.Element("Supplements"));
                rmItem = new XElement("GuestDetails", result);
            }
            else
            {
                rmItem = new XElement("GuestDetails", null);
            }
            return rmItem;
        }

        #endregion

        #region Common
        public XElement SupplementTag(IEnumerable<XElement> lst, IEnumerable<XElement> srvlst)
        {
            List<XElement> itemSupl = new List<XElement>();
            try
            {
                if (lst.Count() > 0)
                {
                    var item = from itm in lst
                               select new XElement("Supplement",
                            new XAttribute("suppId", "0"),
                                 new XAttribute("suppName", itm.Element("supplementName").Value),
                            new XAttribute("supptType", "0"),
                            new XAttribute("suppIsMandatory", "True"),
                            new XAttribute("suppChargeType", "Included"),
                            new XAttribute("suppPrice", "0.00"),
                            new XAttribute("suppType", "PerRoomSupplement"));
                    itemSupl.AddRange(item);
                }
                if (srvlst.Count() > 0)
                {
                    var item = from itm in srvlst
                               select new XElement("Supplement",
                            new XAttribute("suppId", "0"),
                            new XAttribute("suppName", itm.Element("serviceName").Value),
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
        public string Notes(string room, string note)
        {
            string txt = string.Empty;
            if (!string.IsNullOrEmpty(note))
            {
                txt = room + ": " + note.Replace("DOTW", "");
            }
            return txt;
        }
        public string HotelPolicy(IEnumerable<XElement> rmPolicy)
        {
            string txt = string.Empty;
            if (rmPolicy != null)
            {
                txt = "<ol>";
                foreach (var item in rmPolicy)
                {
                    if (!string.IsNullOrEmpty(item.Value))
                    {
                        txt += "<li>";
                        txt += item.Value;
                        txt += "</li>";
                    }


                }
                txt += "</ol>";
            }
            return txt;
        }

        public XElement RoomCxlPolicy(IEnumerable<XElement> Cxllst, string cxlFlag, string sumPrice)
        {

            XElement cxlItem;
            if (cxlFlag == "yes")
            {

                cxlItem = new XElement("CancellationPolicies", new XElement("CancellationPolicy",
                                 new XAttribute("LastCancellationDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                                 new XAttribute("ApplicableAmount", sumPrice),
                                 new XAttribute("NoShowPolicy", 0)));

            }
            else
            {
                if (Cxllst != null)
                {

                    var noshowList = Cxllst.Where(x => (x.Element("cancelRestricted").GetValueOrDefault("false") == "true")).ToList();

                    Cxllst = Cxllst.Where(x => ((x.Element("cancelRestricted").GetValueOrDefault("false") != "true"))).ToList();

                    var result = from cxl in Cxllst.Where(x => (x.Element("charge").Element("formatted").GetValueOrDefault("0.00") != "0.00"))
                                 let noShow = cxl.Element("noShowPolicy").GetValueOrDefault("false") == "true" ? 1 : 0
                                 let Price = (noShow == 0 ? cxl.Element("cancelCharge").Element("formatted").Value : cxl.Element("charge").Element("formatted").Value)
                                 let cxlDate = cxl.CxlDate()
                                 select new XElement("CancellationPolicy",
                                     new XAttribute("LastCancellationDate", cxlDate),
                                     new XAttribute("ApplicableAmount", Price),
                                     new XAttribute("NoShowPolicy", noShow));

                    if (noshowList.Count > 0)
                    {
                        var flagCxlItem = from cxl in noshowList
                                          let cxlDate = cxl.CxlDate()
                                          select new XElement("CancellationPolicy",
                         new XAttribute("LastCancellationDate", cxlDate),
                         new XAttribute("ApplicableAmount", sumPrice),
                         new XAttribute("NoShowPolicy", 0));

                        cxlItem = new XElement("CancellationPolicies", result, flagCxlItem);

                    }
                    else
                    {
                        cxlItem = new XElement("CancellationPolicies", result);
                    }

                    int noIndex = cxlItem.Descendants("CancellationPolicy").Where(x => x.Attribute("NoShowPolicy").Value == "0").Count();
                    int showIndex = cxlItem.Descendants("CancellationPolicy").Where(x => x.Attribute("NoShowPolicy").Value == "1").Count();
                    if (showIndex > 0 && noIndex == 0)
                    {
                        var item = new XElement("CancellationPolicies", new XElement("CancellationPolicy",
                                          new XAttribute("LastCancellationDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                                          new XAttribute("ApplicableAmount", sumPrice),
                                          new XAttribute("NoShowPolicy", 0)));
                        cxlItem.Element("CancellationPolicies").Add(item);
                    }

                }
                else
                {
                    cxlItem = new XElement("CancellationPolicies", null);
                }

            }
            return cxlItem;
        }

        public IEnumerable<XElement> FacilityTag(IEnumerable<XElement> lst)
        {
            IEnumerable<XElement> Facilities;
            Facilities = from itm in lst
                         select new XElement("Facility", itm.Value);
            return Facilities;
        }
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
                    foreach (var rm in rooms)
                    {
                        var prItem = rm.Descendants("CancellationPolicy").
                       Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && pq.Attribute("LastCancellationDate").Value == date)).
                       FirstOrDefault();
                        if (prItem != null)
                        {
                            var price = prItem.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            if (noShow == "1")
                            {
                                datePrice += Convert.ToDecimal(rm.Attribute("TotalRoomRate").Value);
                            }
                            else
                            {


                                var lastItem = rm.Descendants("CancellationPolicy").
                                    Where(pq => (pq.Attribute("NoShowPolicy").Value == noShow && Convert.ToDateTime(pq.Attribute("LastCancellationDate").Value) < date.chnagetoTime()));

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

                cxlList = cxlList.GroupBy(x => new { Convert.ToDateTime(x.Attribute("LastCancellationDate").Value).Date, x.Attribute("NoShowPolicy").Value }).
                    Select(y => new XElement("CancellationPolicy",
                        new XAttribute("LastCancellationDate", y.Key.Date.ToString("yyyy-MM-dd")),
                        new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))),
                        new XAttribute("NoShowPolicy", y.Key.Value))).OrderBy(p => p.Attribute("LastCancellationDate").Value).ToList();

                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("ApplicableAmount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", Convert.ToDateTime(fItem.Attribute("LastCancellationDate").Value).AddDays(-1).Date.ToString("yyyy-MM-dd")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0")));

                }
            }
            XElement cxlItem = new XElement("CancellationPolicies", cxlList);
            return cxlItem;

        }

        public string HotelAddress(XElement htldesc)
        {
            string Address = string.Empty;
            Address = (htldesc.Element("address") != null ? htldesc.Element("address").Value.Address() : "") +
                (htldesc.Element("zipCode") != null ? htldesc.Element("zipCode").Value.Address() : "") +
                (htldesc.Element("cityName") != null ? htldesc.Element("cityName").Value.Address() : "") +
                (htldesc.Element("stateName") != null ? htldesc.Element("stateName").Value.Address() : "") +
                (htldesc.Element("countryName") != null ? htldesc.Element("countryName").Value.Address().Replace(',', ' ') : "");
            return Address;
        }

        public XElement RoomPromotion(IEnumerable<XElement> Promotionlst, IEnumerable<XElement> rateSpeciallst)
        {
            XElement promoItem;
            if ((!Promotionlst.IsNullOrEmpty()) && (!rateSpeciallst.IsNullOrEmpty()))
            {
                //+ " (" + promo.Element("description").Value + ")"
                var result = from promo in Promotionlst
                             join prItem in rateSpeciallst on promo.Attribute("runno").Value equals prItem.Value
                             select new XElement("Promotions", promo.Element("specialName").Value);
                promoItem = new XElement("PromotionList", result);
            }
            else
            {
                promoItem = new XElement("PromotionList", new XElement("Promotions", null));
            }
            return promoItem;
        }

        public XElement RoomRestriction(IEnumerable<XElement> Cxllst, string RoomType)
        {
            XElement cxlItem;
            bool cxlflag = false;
            bool amendflag = false;
            string cancelFrom = string.Empty;
            string cancelTo = string.Empty;
            string amendFrom = string.Empty;
            string amendTo = string.Empty;
            string CheckInDate = travyoReq.Descendants("FromDate").FirstOrDefault().Value.GetDateTime("dd/MM/yyyy").ToString("yyyy-MM-dd HH:mm:ss");
            if (Cxllst.Count() > 0)
            {
                var cxlList = Cxllst.Where(x => (x.Element("cancelRestricted").GetValueOrDefault("false") == "true")).ToList();
                var amendList = Cxllst.Where(x => (x.Element("amendRestricted").GetValueOrDefault("false") == "true")).ToList();
                if (cxlList.Count > 0)
                {
                    cxlflag = true;
                    cancelFrom = cxlList.FirstOrDefault().CxlDate();
                    cancelTo = cxlList.FirstOrDefault().CxlToDate(CheckInDate);
                }
                if (amendList.Count > 0)
                {
                    amendflag = true;
                    amendFrom = amendList.FirstOrDefault().CxlDate();
                    amendTo = amendList.FirstOrDefault().CxlToDate(CheckInDate);
                }
            }
            cxlItem = new XElement("Restriction",
                new XAttribute("cancel", cxlflag),
                new XAttribute("cancelFrom", cancelFrom),
                new XAttribute("cancelTo", cancelTo),
                new XAttribute("amend", amendflag),
                new XAttribute("amendFrom", amendFrom),
                new XAttribute("amendTo", amendTo), RoomType);
            return cxlItem;
        }

        public List<XElement> MergeRestriction(IEnumerable<XElement> Restrictlst)
        {
            List<XElement> textList = new List<XElement>();
            if (!Restrictlst.IsNullOrEmpty())
            {
                int index = 0;
                foreach (var item in Restrictlst)
                {

                    ++index;
                    XElement textItem;
                    string strText = string.Empty;
                    if (item.Attribute("cancel").Value == "true")
                    {
                        strText = "Cancellation is restricted for Room" + index + "(" + item.Value + ")" + " from " + item.Attribute("cancelFrom").Value.GetDateTime("yyyy-MM-dd HH:mm:ss").AddDays(-2).ToString("d MMM, yy") + " to " + item.Attribute("cancelTo").Value.GetDateTime("yyyy-MM-dd HH:mm:ss").ToString("d MMM, yy") + " .";
                        textItem = new XElement("tariffNotes", strText);
                        textList.Add(textItem);

                    }

                    if (item.Attribute("amend").Value == "true")
                    {
                        strText = "Amendment is restricted for Room" + index + "(" + item.Value + ")" + " from " + item.Attribute("amendFrom").Value.GetDateTime("yyyy-MM-dd HH:mm:ss").AddDays(-2).ToString("d MMM, yy") + " to " + item.Attribute("amendTo").Value.GetDateTime("yyyy-MM-dd HH:mm:ss").ToString("d MMM, yy") + " .";
                        textItem = new XElement("tariffNotes", strText);
                        textList.Add(textItem);
                    }

                }
            }
            return textList;
        }

        #endregion

        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region Room wise Cancellation

        public bool IsCxlRequired(string TrackNo, string ReqNo)
        {
            bool flag = true;
            List<XElement> Cxllst = new List<XElement>();
            Client model = new Client();
            model.LogTypeId = 5;
            model.Action = "PreBookConfirm";
            model.TrackNo = TrackNo;
            string result = _DotwRepo.GetXml(model);
            XDocument response = XDocument.Parse(result);

            var rate = response.Descendants("rateBasis").
                Where(p => (
                    p.Element("status").Value == "checked" &&
                    p.Element("allocationDetails").Value == ReqNo)).FirstOrDefault();

            Cxllst = (from cxl in rate.Element("cancellationRules").DescendantsOrEmpty("rule")
                      let noShow = cxl.Element("noShowPolicy").GetValueOrDefault("false") == "true" ? 1 : 0
                      let Price = (cxl.Element("cancelCharge") != null ? cxl.Element("cancelCharge").Element("formatted").Value : rate.Element("total").Element("formatted").Value)
                      let cxlDate = cxl.CxlDate(response.Root.Attribute("date").Value)
                      select new XElement("CancelRule",
                          new XAttribute("LastCxlDate", cxlDate),
                          new XAttribute("Amount", Price),
                          new XAttribute("Cancel", cxl.Element("cancelRestricted").GetValueOrDefault("false")),
                          new XAttribute("NoShow", noShow))).ToList();


            //if (rate.Element("withinCancellationDeadline").GetValueOrDefault("no") == "yes")
            //{
            // XElement cxlItem;
            //    cxlItem = new XElement("CancelRule",
            //                             new XAttribute("LastCxlDate", response.Root.Attribute("date").Value),
            //                             new XAttribute("Amount", rate.Element("total").Element("formatted").Value),
            //                             new XAttribute("Cancel", true),
            //                             new XAttribute("NoShow", 0));
            //    Cxllst.Add(cxlItem);
            //}
            //else
            //{           
            //}

            var CancelList = Cxllst.Where(x => x.Attribute("Cancel").Value == "true" && x.Attribute("NoShow").Value == "0" && x.Attribute("LastCxlDate").Value.GetDateTime("yyyy-MM-dd HH:mm:ss").Date <= DateTime.Now.Date).ToList();
            if (CancelList.Count > 0)
            {
                flag = false;
            }
            return flag;
        }
        public XElement RoomCxlReq(XElement Req)
        {
            try
            {
                travyoReq = Req;
                string TrackNo = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                string ReqNo = travyoReq.Descendants("RequestID").FirstOrDefault().Value;
                XElement SearReq = travyoReq.Descendants("HotelCancellationRequest").FirstOrDefault();
                XElement dowResp = null;
                if (IsCxlRequired(TrackNo, ReqNo))
                {
                    XDocument dowReq = new XDocument(
                       new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement("customer",
                          new XElement("username", _credetials.UserName),
                            new XElement("password", _credetials.Password),
                              new XElement("id", _credetials.Id),
                                new XElement("source", _credetials.Source),
                          new XElement("request",
                             new XAttribute("command", "cancelbooking"),
                                new XElement("bookingDetails",
                                    new XElement("bookingType", 1),
                                    new XElement("bookingCode", Req.Descendants("ServiceID").FirstOrDefault().Value),
                                    new XElement("confirm", "no")
                                ))));
                    dowResp = RoomCxlResp(dowReq);
                }
                else
                {
                    dowResp = new XElement("HotelCancellationResponse", new XElement("ErrorTxt", "Cancellation not allowed at supplier End"));
                }
                SearReq.AddAfterSelf(dowResp);
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "RoomCxlReq";
                custEx.PageName = "DotwService";
                custEx.CustomerID = travyoReq.Descendants("CustomerID").FirstOrDefault().Value;
                custEx.TranID = travyoReq.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
                XElement SearReq = Req.Descendants("HotelCancellationRequest").FirstOrDefault();
                SearReq.AddAfterSelf(new XElement("HotelCancellationResponse", new XElement("ErrorTxt", "No detail found")));
            }
            return Req;
        }

        public XElement RoomCxlResp(XDocument _dotWReq)
        {
            XElement _travyooResp = null;
            Client model = new Client();
            model.Customer = Convert.ToInt64(travyoReq.Descendants("CustomerID").FirstOrDefault().Value);
            model.ActionId = 6;
            model.Action = "Cancel";
            model.TrackNo = travyoReq.Descendants("TransID").FirstOrDefault().Value;
            XDocument response = _DotwRepo.GetResponse(_dotWReq, model);
            bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);

            if (result)
            {
                var cxlPrice = new XElement("testPricesAndAllocation",
                    new XElement("service", new XAttribute("referencenumber", _dotWReq.Descendants("bookingCode").FirstOrDefault().Value),
                        new XElement("penaltyApplied", response.Descendants("charge").FirstOrDefault().Nodes().OfType<XText>().First())));

                XElement priceReq = _dotWReq.Descendants("confirm").FirstOrDefault();
                priceReq.AddAfterSelf(cxlPrice);
                priceReq.SetValue("yes");
                _dotWReq.Descendants("result").Remove();


                XDocument newResponse = _DotwRepo.GetResponse(_dotWReq, model);

                bool newResult = Convert.ToBoolean(newResponse.Descendants("successful").FirstOrDefault().Value);
                if (newResult)
                {
                    _travyooResp = new XElement("HotelCancellationResponse",
                        new XElement("Rooms",
                            new XElement("Room",
                                new XElement("Cancellation",
                                    new XElement("Amount", (travyoReq.Descendants("TotalPrice").FirstOrDefault().Value.ModifyToDecimal() - newResponse.Descendants("penaltyApplied").FirstOrDefault().Element("formatted").Value.ModifyToDecimal())),
                                    new XElement("Status", "Success")))));
                }
            }

            if (_travyooResp == null)
            {
                _travyooResp = new XElement("HotelCancellationResponse",
                  new XElement("Rooms",
                      new XElement("Room",
                          new XElement("Cancellation",
                              new XElement("Amount", travyoReq.Descendants("TotalPrice").FirstOrDefault().Value),
                              new XElement("Status", "Failed")))));
            }
            return _travyooResp;
        }

        #endregion


        /// <summary>
        /// This region  is  created by user Rakesh
        /// </summary>  
        #region SaveBooking
        public XElement SaveBookingReq(XElement Req)
        {
            XElement _resp = null;

            XDocument dowReq = new XDocument(
                       new XDeclaration("1.0", "utf-8", "yes"),
                       new XElement("customer",
                          new XElement("username", _credetials.UserName),
                            new XElement("password", _credetials.Password),
                              new XElement("id", _credetials.Id),
                                new XElement("source", _credetials.Source),
                                      new XElement("product", _credetials.Service),
                          new XElement("request",
                             new XAttribute("command", "savebooking"),
                                new XElement("bookingDetails",
                                    new XElement("fromDate", Req.Descendants("FromDate").FirstOrDefault().DotWDate()),
                                    new XElement("toDate", Req.Descendants("ToDate").FirstOrDefault().DotWDate()),
                                    new XElement("currency", _credetials.Currency),
                                    new XElement("productId", Req.Descendants("HotelID").FirstOrDefault().Value),
                                    new XElement("customerReference", Req.Descendants("TransID").FirstOrDefault().Value),
                                    CNFBookingSeaReqRoomTag(travyoReq.Descendants("Room"))))));
            Client model = new Client();
            model.Customer = Convert.ToInt64(travyoReq.Descendants("CustomerID").FirstOrDefault().Value);
            model.ActionId = 5;
            model.Action = "SaveBooking";
            model.TrackNo = travyoReq.Descendants("TransactionID").FirstOrDefault().Value;
            XDocument response = _DotwRepo.GetResponse(dowReq, model);
            bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);
            if (result)
            {
                _resp = BookingIteniaryRep(response.Root);

            }
            else
            {
                _resp = new XElement("HotelBookingResponse",
                    new XElement("ErrorTxt", response.Descendants("details").FirstOrDefault().Value));
            }
            return _resp;
        }



        public XElement BookingIteniaryRep(XElement req)
        {
            XElement _resp = null;
            XElement _room = IteniaryServiceTag(req);
            XDocument dowReq = new XDocument(
                         new XDeclaration("1.0", "utf-8", "yes"),
                         new XElement("customer",
                            new XElement("username", _credetials.UserName),
                              new XElement("password", _credetials.Password),
                                new XElement("id", _credetials.Id),
                                  new XElement("source", _credetials.Source),
                            new XElement("request",
                               new XAttribute("command", "bookitinerary"),
                                  new XElement("bookingDetails",
                                      new XElement("bookingType", 2),
                                      new XElement("bookingCode", req.Descendants("returnedCode").FirstOrDefault().Value),
                                      new XElement("confirm", "no"),
                                         new XElement("sendCommunicationTo", "info@ingeniumsoftech.com")))));
            Client model = new Client();
            model.Customer = Convert.ToInt64(travyoReq.Descendants("CustomerID").FirstOrDefault().Value);
            model.ActionId = 5;
            model.Action = "BookConfirm";
            model.TrackNo = travyoReq.Descendants("TransactionID").FirstOrDefault().Value;
            XDocument response = _DotwRepo.GetResponse(dowReq, model);
            bool result = Convert.ToBoolean(response.Descendants("successful").FirstOrDefault().Value);
            if (result)
            {
                int roomCount = response.Descendants("product").Count();
                int blockCount = response.Descendants("product").Where(x => x.Element("available").Value == "TRUE").Count();

                if (blockCount == roomCount)
                {
                    decimal newPrice = response.Descendants("product").Sum(x => x.Element("price").FirstNode.ToString().StringDecimal());
                    if (Math.Round(newPrice, 2) == Math.Round(price, 2))
                    {

                        XElement priceReq = dowReq.Descendants("confirm").FirstOrDefault();
                        priceReq.AddAfterSelf(_room);
                        priceReq.SetValue("yes");
                        dowReq.Descendants("sendCommunicationTo").Remove();
                        dowReq.Descendants("result").Remove();
                        model.Action = "Book";
                        XDocument newResponse = _DotwRepo.GetResponse(dowReq, model);
                        bool newResult = Convert.ToBoolean(newResponse.Descendants("successful").FirstOrDefault().Value);
                        if (newResult)
                        {
                            var Detail = new XElement("Hotels",
                                                      new XElement("HotelID", travyoReq.Descendants("HotelID").FirstOrDefault().Value),
                                                      new XElement("HotelName", ""),
                                                      new XElement("FromDate", travyoReq.Descendants("FromDate").FirstOrDefault().Value),
                                                      new XElement("ToDate", travyoReq.Descendants("ToDate").FirstOrDefault().Value),
                                                      new XElement("AdultPax", dowReq.Descendants("adultsCode").Sum(x => Convert.ToInt16(x.Value))),//
                                                      new XElement("ChildPax", dowReq.Descendants("children").Sum(x => Convert.ToInt16(x.Attribute("no").Value))),//
                                                      new XElement("TotalPrice", newResponse.Descendants("price").Sum(x => Convert.ToDecimal(x.Element("formatted").Value))),//
                                                      new XElement("CurrencyID", travyoReq.Descendants("CurrencyID").FirstOrDefault().Value),
                                                      new XElement("CurrencyCode", travyoReq.Descendants("CurrencyCode").FirstOrDefault().Value),
                                                      new XElement("MarketID", ""),//
                                                      new XElement("MarketName", ""),//              
                                                      new XElement("HotelImgSmall", ""),
                                                      new XElement("HotelImgLarge", ""),
                                                      new XElement("MapLink", ""),//
                                                      new XElement("VoucherRemark", ""),
                                                      new XElement("TransID", travyoReq.Descendants("TransID").FirstOrDefault().Value),
                                                      new XElement("ConfirmationNumber", newResponse.Descendants("returnedCode").FirstOrDefault().Value),//
                                                      new XElement("Status", newResponse.Descendants("bookingStatus").Count() == newResponse.Descendants("bookingStatus").Where(x => x.Value == "2").Count() ? "Success" : "Failed"),
                                                      new XElement("PassengersDetail", CNFBookingSeaRespRoomTag(newResponse))
                                                      );
                            _resp = new XElement("HotelBookingResponse", Detail);
                        }
                        else
                        {
                            _resp = new XElement("HotelBookingResponse",
                                                                new XElement("ErrorTxt", newResponse.Descendants("details").FirstOrDefault().Value));
                        }


                    }
                    else
                    {
                        _resp = new XElement("HotelBookingResponse",
                                          new XElement("ErrorTxt", "Booking Price has been changed"));
                    }

                }
                else
                {
                    _resp = new XElement("HotelBookingResponse",
                                         new XElement("ErrorTxt", "Room Not Available"));
                }
            }
            else
            {
                _resp = new XElement("HotelBookingResponse",
              new XElement("ErrorTxt", response.Descendants("details").FirstOrDefault().Value));
            }
            return _resp;
        }

        public XElement IteniaryServiceTag(XElement response)
        {

            XElement rmItem;
            if (response != null)
            {
                var result = from room in travyoReq.Descendants("Room")
                             join bok in response.Descendants("returnedServiceCode") on room.Attribute("DotwIndex").Value equals bok.Attribute("runno").Value
                             select new XElement("service", new XAttribute("referencenumber", bok.Value),
                              new XElement("testPrice", room.Attribute("TotalRoomRate").Value),
                              new XElement("allocationDetails", room.Element("RequestID").Value));
                rmItem = new XElement("testPricesAndAllocation", result);
            }
            else
            {
                rmItem = new XElement("testPricesAndAllocation", null);
            }
            return rmItem;
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