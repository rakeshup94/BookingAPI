using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Common;
using TravillioXMLOutService.Models.JacTravel;
using System.Configuration;
using System.IO;

namespace TravillioXMLOutService.Supplier.Miki
{
    public class MikiService : IDisposable
    {
        bool disposed = false;
        XElement travyoReq;
        TravayooRepository objRepo;
        RequestModel objModel;
        //string serviceHost, userName, password, suplId;
        string serviceHost, userName, password, suplId, currency;
        XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        string dmc = string.Empty;
        string customerId = string.Empty;

        public MikiService(string customerId, string _suplId, string _dmc)
        {
            objRepo = new TravayooRepository();
            objModel = new RequestModel();
            #region Credentials
            XElement suppliercred = supplier_Cred.getsupplier_credentials(customerId, _suplId);
            try
            {
                this.dmc = _dmc;
                this.customerId = customerId;
                this.suplId = _suplId;
                this.serviceHost = suppliercred.Element("endpoint").Value;
                this.userName = suppliercred.Element("username").Value;
                this.password = suppliercred.Element("password").Value;
                //currency = Convert.ToInt32(suppliercred.Element("Currency").Value);
                objModel.HostName = suppliercred.Element("endpoint").Value;
                objModel.Method = "POST";
                objModel.ContentType = "application/soap+xml;charset=UTF-8;action=hotelSearch";
                this.currency = suppliercred.Element("currency").Value;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            #endregion
        }

        public XElement SearchByHotel(XElement req)
        {
            XElement resp = null;
            try
            {
                DateTime startTime = DateTime.Now;
                this.travyoReq = req;
                XDocument mikiReq = CreateSearchReq(req);
                objModel.RequestStr = mikiReq.ToString();
                string response = objRepo.GetHttpResponse(objModel);
                XElement mikiResp = XElement.Parse(response);
                mikiResp = mikiResp.RemoveXmlns();
               
                #region Save Log
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(req.Descendants("CustomerID").FirstOrDefault().Value);
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = 11;
                log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = mikiReq.ToString();
                log.logresponseXML = mikiResp.ToString();
                log.StartTime = startTime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion




   
                resp = CreateSearchResp(mikiResp);


             

            }
            catch (Exception ex)
            {
                resp = new XElement("searchResponse", new XElement("Hotels", null),
                     new XElement("ErrorText", ex.Message));


                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "SearchByHotel";
                ex1.PageName = "MikiService";
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
            XElement htlItem;

            //DataTable cityMapping = jac_staticdata.CityMapping(this.travyoReq.Descendants("CityID").FirstOrDefault().Value, suplId.ToString());
            //int CityId = Convert.ToInt32(cityMapping.Rows[0]["SupCityId"].ToString());
            string SupCityId = TravayooRepository.SupllierCity(suplId, this.travyoReq.Descendants("CityID").FirstOrDefault().Value);
            int CityId = Convert.ToInt32(SupCityId);

            var model = new SqlModel()
            {
                flag = 2,
                columnList = "HotelID,HotelName,StarRating",
                table = "MikiStaticData",
                filter = "CityID=" + CityId.ToString() + " AND HotelName LIKE '%" + req.Descendants("HotelName").FirstOrDefault().Value + "%'",
                SupplierId = 11
            };
            if (!string.IsNullOrEmpty(req.Descendants("HotelID").FirstOrDefault().Value))
            {
                model.HotelCode = req.Descendants("HotelID").FirstOrDefault().Value;
            }
            DataTable htlList = TravayooRepository.GetData(model);


            if (htlList.Rows.Count > 0)
            {
                var result = htlList.AsEnumerable().Select(y => new XElement("productCode", y.Field<string>("HotelID")));
                htlItem = new XElement("productCodes", result);
            }
            else
            {
                //throw new Exception("There is no hotel available in database");
                return null;
            }
            
       
            Random rnd = new Random();
            XDocument mikiDoc = new XDocument(
                             new XDeclaration("1.0", "utf-8", "yes"),
                             new XElement(soap + "Envelope",
                                 new XAttribute(XNamespace.Xmlns + "soap", soap),
                                 new XElement(soap + "Header"),
                                 new XElement(soap + "Body",
                             new XElement("hotelSearchRequest",
                                 new XAttribute("versionNumber", "7.0"),
                                 new XElement("requestAuditInfo",
                                     new XElement("agentCode", this.userName),
                                     new XElement("requestPassword", this.password),
                                     new XElement("requestID", Convert.ToString(rnd.Next(999999999))),
                                     new XElement("requestDateTime", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"))),
                                 new XElement("hotelSearchCriteria",
                                    //new XAttribute("currencyCode", "EUR"),
                                    new XAttribute("currencyCode", this.currency),
                                     new XAttribute("paxNationality", req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                                     new XAttribute("languageCode", "en"),
                                     new XElement("destination",
                                         new XElement("hotelRefs", htlItem)),
                                     new XElement("stayPeriod",
                                         new XElement("checkinDate", req.Descendants("FromDate").FirstOrDefault().Value.AlterFormat("dd/MM/yyyy", "yyyy-MM-dd")),
                                         new XElement("checkoutDate", req.Descendants("ToDate").FirstOrDefault().Value.AlterFormat("dd/MM/yyyy", "yyyy-MM-dd"))),
                                     new XElement("rooms", BindRoomTag(req.Descendants("RoomPax"))),
                                     new XElement("availabilityCriteria",
                                         new XElement("availabilityIndicator", "2")),
                                     new XElement("priceCriteria",
                                         new XElement("returnBestPriceIndicator", "true")),
                                     new XElement("hotelCriteria",
                                          new XElement("starRatings",
                                              BindRating(req.Descendants("MinStarRating").FirstOrDefault().Value, req.Descendants("MaxStarRating").FirstOrDefault().Value))),
                                     new XElement("resultDetails",
                                         new XElement("returnDailyPrices", "1"),
                                         new XElement("returnHotelInfo", "1"),
                                         new XElement("returnSpecialOfferDetails", "1")))))));
            return mikiDoc;
        }

        IEnumerable<XElement> BindRoomTag(IEnumerable<XElement> rmlst)
        {
            int roomCount = 1;
            var rmItem = from room in rmlst
                         select new XElement("room",
                             new XElement("roomNo", roomCount++),
                             new XElement("guests", GusestTag(Convert.ToInt16(room.Element("Adult").Value)),
                                 ChildTag(room.Descendants("ChildAge"))));
            return rmItem;
        }
        IEnumerable<XElement> GusestTag(int adult)
        {
            var gst = from room in Enumerable.Range(1, adult)
                      select new XElement("guest",
                                     new XElement("type", "ADT"));
            return gst;
        }
        IEnumerable<XElement> ChildTag(IEnumerable<XElement> ChildList)
        {
            var gst = from item in ChildList
                      select new XElement("guest",
                                  new XElement("type", "CHD"),
                                  new XElement("age", item.Value));
            return gst;
        }
        IEnumerable<XElement> BindRating(string min, string max)
        {
            int minRating = Convert.ToInt32(min);
            minRating = minRating == 0 ? 1 : minRating;
            int maxRating = Convert.ToInt32(max);
            var rating = from itm in Enumerable.Range(minRating, maxRating - minRating + 1)
                         select new XElement("starRating", itm);
            return rating;
        }

        XElement CreateSearchResp(XElement mikiResp)
        {
            XElement _travayoResp = null;

            if (mikiResp.Descendants("error").Count() == 0)
            {


                string xmlouttype = string.Empty;
                try
                {
                    if (dmc == "Miki")
                    {
                        xmlouttype = "false";
                    }
                    else
                    { xmlouttype = "true"; }
                }
                catch { }
                
                string hotelCodes = string.Empty;
                foreach (var item in mikiResp.Descendants("productCode"))
                {
                    hotelCodes += item.Value + ","; 
                }
                hotelCodes = hotelCodes.TrimEnd(',');
                var model = new SqlModel()
                {
                    flag = 4,
                    filter = hotelCodes
                };
                DataTable htlList = TravayooRepository.GetData(model);

                var _req = this.travyoReq.Descendants("searchRequest").FirstOrDefault();


                var respItem = from item in mikiResp.Descendants("hotel")
                               join htldesc in htlList.AsEnumerable()
                                   on item.Element("productCode").Value equals htldesc.Field<string>("HotelID")
                               select new XElement("Hotel",
                                            new XElement("HotelID", item.Element("productCode").Value),
                                            new XElement("HotelName", item.Element("hotelInfo").Element("hotelName").Value),
                                            new XElement("PropertyTypeName", "Hotel"),
                                            new XElement("CountryID", travyoReq.Descendants("CountryID").FirstOrDefault().Value),
                                            new XElement("CountryName", travyoReq.Descendants("CountryName").FirstOrDefault().Value),
                                            new XElement("CountryCode", travyoReq.Descendants("CountryCode").FirstOrDefault().Value),
                                            new XElement("CityId", travyoReq.Descendants("CityID").FirstOrDefault().Value),
                                            new XElement("CityCode", travyoReq.Descendants("CityCode").FirstOrDefault().Value),
                                            new XElement("CityName", travyoReq.Descendants("CityName").FirstOrDefault().Value),
                                            new XElement("AreaId"),
                                            new XElement("AreaName", htldesc.Field<string>("Location")),
                                            new XElement("RequestID", travyoReq.Descendants("TransID").FirstOrDefault().Value),
                                            new XElement("Address", htldesc.Field<string>("HotelAddress")),
                                            new XElement("Location"),
                                            new XElement("Description", htldesc.Field<string>("HotelDescription")),
                                            new XElement("StarRating", item.Element("hotelInfo").Element("starRating").Value),
                                            new XElement("MinRate", item.Descendants("roomOptions").Any() ? MinRate(item.Descendants("roomOptions").FirstOrDefault(), travyoReq) : "0"),
                                            new XElement("HotelImgSmall", htldesc.Field<string>("ImageThumbnail")),
                                            new XElement("HotelImgLarge", htldesc.Field<string>("ImageLarge")),
                                            new XElement("MapLink"),
                                            new XElement("Longitude", htldesc.Field<string>("Longitude")),
                                            new XElement("Latitude", htldesc.Field<string>("Latitude")),
                                            new XElement("DMC", dmc),
                                            new XElement("xmloutcustid", customerId),
                                            new XElement("xmlouttype", xmlouttype),
                                            new XElement("SupplierID", suplId.ToString()),
                                            new XElement("Currency", item.Element("currencyCode").Value),
                                            new XElement("Offers", null),
                                            new XElement("Facilities", null),
                                        new XElement("Rooms", null));

                _travayoResp = new XElement("searchResponse", new XElement("Hotels", respItem));
            }
            else
            {
                _travayoResp = new XElement("searchResponse", new XElement("Hotels", null));
            }
            return _travayoResp;
        }

        public string MinRate(XElement rooms, XElement Req)
        {
            double minimumPrice = MinPriceGrouping(rooms, Req);
            string rate = null;

            if (minimumPrice == double.MaxValue)
                rate = "0.0";
            else
                rate = Convert.ToString(minimumPrice);
            return rate;
        }

        public bool roomnumber(IEnumerable<XElement> roomNumber, string check)
        {
            bool abc = false;
            var rrr = from rn in roomNumber
                      select rn.Value;
            if (rrr.Contains(check))
                abc = true;
            return abc;
        }

        public List<XElement> RoomTag1(XElement MikiRoom, XElement occupancy)
        {
            List<XElement> roomlist = new List<XElement>();

            var forrooms = from rn in MikiRoom.Descendants("roomNumber")
                           select rn.Value;
            if (forrooms.Contains(occupancy.Descendants("id").FirstOrDefault().Value))
            {

                try
                {
                    roomlist.Add(new XElement("Room",
                                      new XAttribute("Occupancy", occupancy.Descendants("id").FirstOrDefault().Value),
                                      new XAttribute("MealPlanCode", MikiRoom.Descendants("mealBasis").FirstOrDefault().Attribute("mealBasisCode").Value),
                                      new XAttribute("RoomPrice", MikiRoom.Descendants("roomTotalPrice").FirstOrDefault().Descendants("price").FirstOrDefault().Value)));
                }
                catch { }
            }
            return roomlist;
        }

        public double MinPriceGrouping(XElement MikiRooms, XElement travReq)
        {
            int count = 1;


            double response = 0.0;

            XElement retRooms = new XElement("Rooms");
            int roomcount = travReq.Descendants("RoomPax").Count();
            #region Room Count = 1
            if (roomcount == 1)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
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
            }
            #endregion
            #region Room Count = 2
            else if (roomcount == 2)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
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
            }
            #endregion
            #region Room Count = 3
            else if (roomcount == 3)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {

                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
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
            }
            #endregion
            #region Room Count = 4
            else if (roomcount == 4)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
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
            }
            #endregion
            #region Room Count = 5
            else if (roomcount == 5)
            {
                var occupancy = new List<XElement>(travReq.Descendants("RoomPax")).ToList();
                foreach (var pax in occupancy)
                {
                    pax.Add(new XElement("id", Convert.ToString(count++)));
                    var roomtypes = from rooms in MikiRooms.Descendants("roomOption")
                                    where roomnumber(rooms.Descendants("roomNumber"), pax.Descendants("id").FirstOrDefault().Value)
                                    select
                                    RoomTag1(rooms, pax);
                    retRooms.Add(roomtypes);
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
                                var query5 = retRooms.Descendants("Room").Where(x => x.Attribute("Occupancy").Value.Equals("5"));
                                foreach (var rm5 in query5)
                                {

                                    List<XElement> mpc = new List<XElement>();
                                    mpc.Add(rm1);
                                    mpc.Add(rm2);
                                    mpc.Add(rm3);
                                    mpc.Add(rm4);
                                    mpc.Add(rm5);
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
                }
                #endregion
                response = minimum;
            }
            #endregion

            return response;
        }






        #region Dispose






        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                travyoReq = null;
                objRepo = null;
                objModel = null;

                // Free any other managed objects here.
                //
            }

            disposed = true;
        }



        ~MikiService()
        {
            Dispose(false);
        }






        #endregion











    }
}