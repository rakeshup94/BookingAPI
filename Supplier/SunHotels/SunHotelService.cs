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

namespace TravillioXMLOutService.Supplier.SunHotels
{
    public class SunHotelService : IDisposable
    {

        bool disposed = false;
        XElement travyoReq;
        TravayooRepository objRepo;
        RequestModel objModel;
        string serviceHost, userName, password, suplId;

        XNamespace sunhotel = "http://xml.sunhotels.net/15/";
        XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

        string imageURL = "http://www.sunhotels.net/Sunhotels.net/HotelInfo/hotelImage.aspx";
        string dmc = string.Empty;

        string customerId = string.Empty;
        public SunHotelService(string customerId, string _suplId, string _dmc)
        {
            this.dmc = _dmc;
            this.customerId = customerId;
            objRepo = new TravayooRepository();
            objModel = new RequestModel();
            #region Credentials
            XElement suppliercred = supplier_Cred.getsupplier_credentials(customerId, _suplId);
            try
            {
                this.suplId = _suplId;
                this.serviceHost = suppliercred.Element("HostUrl").Value;
                this.userName = suppliercred.Element("Login").Value;
                this.password = suppliercred.Element("Password").Value;
                //currency = Convert.ToInt32(suppliercred.Element("Currency").Value);
                objModel.HostName = suppliercred.Element("HostUrl").Value;
                objModel.Method = "POST";
                objModel.ContentType = "text/xml;charset=UTF-8;";
                objModel.Header = @"SOAPAction:http://xml.sunhotels.net/15/";

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
                XDocument sunReq = CreateSearchReq(req);
                if (sunReq == null)
                {
                    return null;
                }
                objModel.RequestStr = sunReq.ToString();
                objModel.Header += "SearchV2";

                objModel.SupplierId = 36;
                objModel.CustomerId = Convert.ToInt64(req.Descendants("CustomerID").FirstOrDefault().Value);
                objModel.TrackNo = req.Descendants("TransID").FirstOrDefault().Value;
                objModel.ActionId = 1;
                objModel.Action = "Search";


                string response = objRepo.GetHttpResponse(objModel);
                XElement sunResp = XElement.Parse(response);
                sunResp = sunResp.RemoveXmlns();

                #region Save Log
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").FirstOrDefault().Value);
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.SupplierID = 36;
                    log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                    log.logrequestXML = sunReq.ToString();
                    log.logresponseXML = sunResp.ToString();
                    log.StartTime = startTime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch { }
                #endregion

                resp = CreateSearchResp(sunResp);





            }
            catch (Exception ex)
            {
                resp = new XElement("searchResponse", new XElement("Hotels", null),
                     new XElement("ErrorText", ex.Message));




                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "SearchByHotel";
                ex1.PageName = "SunHotelService";
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
            string codeList = string.Empty;

            //DataTable cityMapping = jac_staticdata.CityMapping(this.travyoReq.Descendants("CityID").FirstOrDefault().Value, suplId.ToString());
            //int CityId = Convert.ToInt32(cityMapping.Rows[0]["SupCityId"].ToString());


            string SupCityId = TravayooRepository.SupllierCity(suplId, this.travyoReq.Descendants("CityID").FirstOrDefault().Value);
            int CityId = Convert.ToInt32(SupCityId);


            var model = new SqlModel()
            {
                flag = 2,
                columnList = "HotelID,HotelName,Rating",
                table = "tblSunHotelDetails",
                filter = "CityID=" + CityId.ToString() + " AND HotelName LIKE '%" + req.Descendants("HotelName").FirstOrDefault().Value + "%'",
                SupplierId = 36
            };
            if (!string.IsNullOrEmpty(req.Descendants("HotelID").FirstOrDefault().Value))
            {
                model.HotelCode = req.Descendants("HotelID").FirstOrDefault().Value;
            }

            DataTable htlList = TravayooRepository.GetData(model);

            if (htlList.Rows.Count > 0)
            {
                foreach (DataRow item in htlList.Rows)
                {
                    codeList += item["HotelID"] + ",";
                }
            }
            else
            {
                //throw new Exception("There is no hotel available in database");
                return null;
            }







            var paxnumber = GetPaxNumber(req);
            int adult = paxnumber.Item1, child = paxnumber.Item2, infant = paxnumber.Item3;
            string strChildAge = paxnumber.Item4;
            XDocument sunDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(soap + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                    new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                    new XAttribute(XNamespace.Xmlns + "soap12", soap),
                    new XElement(soap + "Body",
                        new XElement(sunhotel + "SearchV2",
                            new XAttribute(XNamespace.None + "xmlns", sunhotel),
                            new XElement(sunhotel + "userName", this.userName),
                            new XElement(sunhotel + "password", this.password),
                            new XElement(sunhotel + "language", "en"),
                            new XElement(sunhotel + "currencies", "USD"),
                            new XElement(sunhotel + "checkInDate", req.Descendants("FromDate").FirstOrDefault().Value.AlterFormat("dd/MM/yyyy", "yyyy-MM-dd")),
                            new XElement(sunhotel + "checkOutDate", req.Descendants("ToDate").FirstOrDefault().Value.AlterFormat("dd/MM/yyyy", "yyyy-MM-dd")),
                            new XElement(sunhotel + "numberOfRooms", req.Descendants("RoomPax").Count()),
                            new XElement(sunhotel + "hotelIDs", codeList),
                            new XElement(sunhotel + "numberOfAdults", adult.ToString()),
                            new XElement(sunhotel + "numberOfChildren", child.ToString()),
                            new XElement(sunhotel + "childrenAges", strChildAge),
                            new XElement(sunhotel + "infant", infant.ToString()),
                            new XElement(sunhotel + "showCoordinates", "1"),
                            new XElement(sunhotel + "minStarRating", req.Descendants("MinStarRating").FirstOrDefault().Value.Equals("0") ? "1" : req.Descendants("MinStarRating").FirstOrDefault().Value),
                            new XElement(sunhotel + "maxStarRating", req.Descendants("MaxStarRating").FirstOrDefault().Value),
                            new XElement(sunhotel + "paymentMethodId", "1"),
                            new XElement(sunhotel + "customerCountry", req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                            new XElement(sunhotel + "b2c", "0")))));
            return sunDoc;
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



        XElement CreateSearchResp(XElement sunResp)
        {
            XElement _travayoResp = null;

            if (sunResp.Descendants("error").Count() == 0)
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

                string hotelCodes = string.Empty;
                foreach (var item in sunResp.Descendants("hotel.id"))
                {
                    hotelCodes += item.Value + ","; 
                }
                hotelCodes = hotelCodes.TrimEnd(',');
                var model = new SqlModel()
                {
                    flag = 5,
                    filter = hotelCodes
                };
                DataTable htlList = TravayooRepository.GetData(model);

                var _req = this.travyoReq.Descendants("searchRequest").FirstOrDefault();
                var respItem = from item in sunResp.Descendants("hotel")
                               join htldesc in htlList.AsEnumerable()
                               on item.Element("hotel.id").Value equals htldesc.Field<string>("HotelID")
                               select new XElement("Hotel",
                                   new XElement("HotelID", item.Element("hotel.id").Value),
                                   new XElement("HotelName", htldesc.Field<string>("HotelName")),
                                   new XElement("PropertyTypeName", "Hotel"),
                                   new XElement("CountryID", travyoReq.Descendants("CountryID").FirstOrDefault().Value),
                                   new XElement("CountryName", travyoReq.Descendants("CountryName").FirstOrDefault().Value),
                                   new XElement("CountryCode", travyoReq.Descendants("CountryCode").FirstOrDefault().Value),
                                   new XElement("CityId", travyoReq.Descendants("CityID").FirstOrDefault().Value),
                                   new XElement("CityCode", travyoReq.Descendants("CityCode").FirstOrDefault().Value),
                                   new XElement("CityName", travyoReq.Descendants("CityName").FirstOrDefault().Value),
                                   new XElement("AreaId"),
                                   new XElement("AreaName"),
                                   new XElement("RequestID", travyoReq.Descendants("TransID").FirstOrDefault().Value),
                                   new XElement("Address", htldesc.Field<string>("Address")),
                                   new XElement("Location"),
                                   new XElement("Description", htldesc.Field<string>("Description")),
                                   new XElement("StarRating", htldesc.Field<string>("Rating")),
                                   new XElement("MinRate", MinRate(item.Descendants("roomtypes").FirstOrDefault())),
                                   new XElement("HotelImgSmall", imageURL + htldesc.Field<string>("SmallImage")),
                                   new XElement("HotelImgLarge", imageURL + htldesc.Field<string>("FullSizeImage")),
                                   new XElement("MapLink"),
                                   new XElement("Longitude", htldesc.Field<string>("Longitude")),
                                   new XElement("Latitude", htldesc.Field<string>("Latitude")),
                                   new XElement("DMC", dmc),
                                   new XElement("xmloutcustid", customerId),
                                   new XElement("xmlouttype", xmlouttype),
                                   new XElement("SupplierID", suplId.ToString()),
                                   new XElement("Currency", "USD"),
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

        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>

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



        ~SunHotelService()
        {
            Dispose(false);
        }













        #endregion












    }
}