using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models.DotW;
using TravillioXMLOutService.Common;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Common;
using System.Configuration;
using System.IO;
using System.Data;
using TravillioXMLOutService.Models.JacTravel;


namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class JacService : IDisposable
    {
        bool disposed = false;
        XElement travyoReq;
        TravayooRepository objRepo;
        RequestModel objModel;
        string serviceHost, userName, password, suplId;
        string dmc = string.Empty;
        string customerId = string.Empty;

        public JacService(string _customerId, string _suplId, string _dmc)
        {
            this.dmc = _dmc;
            this.customerId = _customerId;
            objRepo = new TravayooRepository();
            objModel = new RequestModel();
            #region Credentials
            XElement suppliercred = supplier_Cred.getsupplier_credentials(_customerId, _suplId);
            try
            {
                this.suplId = _suplId;
                this.serviceHost = suppliercred.Element("endpoint").Value;
                this.userName = suppliercred.Element("Login").Value;
                this.password = suppliercred.Element("Password").Value;
                //currency = Convert.ToInt32(suppliercred.Element("Currency").Value);
                objModel.HostName = suppliercred.Element("endpoint").Value;
                objModel.Method = "POST";
                objModel.ContentType = "application/x-www-form-urlencoded";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            #endregion
        }
        public XElement SearchByHotel(XElement req)
        {
            XElement resp;
            try
            {
                DateTime startTime = DateTime.Now;
                this.travyoReq = req;
                XDocument jacReq = CreateSearchReq(req);
                objModel.RequestStr = jacReq.ToString().TotalStayEncode();
                string response = objRepo.GetHttpResponse(objModel);


                XElement jacResp = XElement.Parse(response);
                jacResp = jacResp.RemoveXmlns();


                #region Save Log
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(req.Descendants("CustomerID").FirstOrDefault().Value);
                log.LogTypeID = 1;
                log.LogType = "Search";
                log.SupplierID = 37;
                log.TrackNumber = req.Descendants("TransID").FirstOrDefault().Value;
                log.logrequestXML = jacReq.ToString();
                log.logresponseXML = jacResp.ToString();
                log.StartTime = startTime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
                #endregion




                resp = CreateSearchResp(jacResp);



            }
            catch (Exception ex)
            {
                resp = new XElement("searchResponse", new XElement("Hotels", null),
                     new XElement("ErrorText", ex.Message));

                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "SearchByHotel";
                ex1.PageName = "JacService";
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

            if (!string.IsNullOrEmpty(req.Descendants("HotelID").FirstOrDefault().Value))
            {
                var model = new SqlModel()
                {
                    flag = 2,
                    SupplierId = 37,
                    HotelCode = req.Descendants("HotelID").FirstOrDefault().Value
                };
                DataTable htlList = TravayooRepository.GetData(model);



                if (htlList.Rows.Count > 0)
                {
                    var result = htlList.AsEnumerable().Select(y => new XElement("PropertyReferenceID", y.Field<string>("HotelID")));
                    htlItem = new XElement("PropertyReferenceIDs", result);
                }
                else
                {
                    throw new Exception("There is no hotel available in database");
                }











              
            }
            else
            {
                DataTable cityMapping = jac_staticdata.CityMapping(this.travyoReq.Descendants("CityID").FirstOrDefault().Value, suplId.ToString());
                int CityId = Convert.ToInt32(cityMapping.Rows[0]["SupCityId"].ToString());
                string strPath = ConfigurationManager.AppSettings["JacPath"] + @"Property\" + CityId + ".xml";
                string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
                XDocument htlList = XDocument.Load(FilePath);

                var result = htlList.Descendants("Property").
                          Where(x => x.Element("PropertyName").Value.Contains(req.Descendants("HotelName").FirstOrDefault().Value)).
                          Select(y => new XElement("PropertyReferenceID", y.Element("PropertyReferenceID").Value));
             

                if (result.Count() > 0)
                {

                    htlItem = new XElement("PropertyReferenceIDs", result);
                }
                else
                {
                    throw new Exception("There is no hotel available in database");
                }
                
            }

            XDocument jacDoc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("SearchRequest",
                    new XElement("LoginDetails",
                        new XElement("Login", this.userName),
                        new XElement("Password", this.password),
                        new XElement("CurrencyID", "2")),
                        new XElement("SearchDetails",
                            new XElement("ArrivalDate", req.Descendants("FromDate").FirstOrDefault().Value.AlterFormat("dd/MM/yyyy", "yyyy-MM-dd")),
                            new XElement("Duration", req.Descendants("Nights").FirstOrDefault().Value), htlItem,
                            new XElement("MealBasisID", "0"),
                                new XElement("MinStarRating", req.Descendants("MinStarRating").FirstOrDefault().Value),
                                new XElement("RoomRequests", BindRoomTag(req.Descendants("RoomPax"))))));
            return jacDoc;
        }
        IEnumerable<XElement> BindRoomTag(IEnumerable<XElement> rmlst)
        {
            var rmItem = from room in rmlst
                         select new XElement("RoomRequest",
                                     new XElement("Adults", room.Element("Adult").Value),
                                     new XElement("Children", JacHelper.GetChildCount(room.Descendants("ChildAge"))),
                                     new XElement("Infants", JacHelper.GetInfantsCount(room.Descendants("ChildAge"))),
                                     JacHelper.BindChild(room.Descendants("ChildAge")));
            return rmItem;
        }

        XElement CreateSearchResp(XElement JacResp)
        {
            XElement _travayoResp;
            bool result = Convert.ToBoolean(JacResp.Descendants("Success").FirstOrDefault().Value);
            if (result)
            {




                string xmlouttype = string.Empty;
                try
                {
                    if (dmc == "TotalStay" || dmc == "JacTravels")
                    {
                        xmlouttype = "false";
                    }
                    else
                    { xmlouttype = "true"; }
                }
                catch { }










                var _req = this.travyoReq.Descendants("searchRequest").FirstOrDefault();
                DataTable cityMapping = jac_staticdata.CityMapping(this.travyoReq.Descendants("CityID").FirstOrDefault().Value, suplId.ToString());
                int RegionID = Convert.ToInt32(cityMapping.Rows[0]["SupCityId"].ToString());
                string strPath = ConfigurationManager.AppSettings["JacPath"] + @"Property\" + RegionID + ".xml";
                string FilePath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);

                if (File.Exists(FilePath))
                {
                    XDocument htlList = XDocument.Load(FilePath);
                    var respItem = from item in JacResp.Descendants("PropertyResult")
                                   join htldesc in htlList.Descendants("Property")
                                       on item.Element("PropertyReferenceID").Value equals htldesc.Element("PropertyReferenceID").Value
                                   select new XElement("Hotel",
                                         new XElement("HotelID", item.Element("PropertyReferenceID").Value),
                                         new XElement("HotelName", item.Element("PropertyName").Value),
                                         new XElement("PropertyTypeName", htldesc.Element("PropertyType").Value),
                                         new XElement("RequestID", item.Element("PropertyReferenceID").Value),
                                         new XElement("CountryID", _req.Element("CountryID").Value),
                                         new XElement("CountryCode", _req.Element("CountryCode").Value),
                                         new XElement("CountryName", htldesc.Element("Country").Value),
                                         new XElement("CityID", _req.Element("CityID").Value),
                                         new XElement("CityCode", _req.Element("CityCode").Value),
                                         new XElement("CityName", htldesc.Element("Region").Value),
                                         new XElement("AreaName", htldesc.Element("Region").Value),
                                         new XElement("AreaId", htldesc.Element("RegionID").Value),
                                         new XElement("Address", htldesc.Element("Address1").Value + ", " + htldesc.Element("Address2").Value),
                                         new XElement("Location", htldesc.Element("TownCity").Value),
                                         new XElement("Description", string.Empty),
                                         new XElement("StarRating", htldesc.Element("Rating").Value),
                                         new XElement("MinRate", minHotelPrice(item)),
                                         new XElement("HotelImgSmall", htldesc.Element("ThumbnailURL").Value),
                                         new XElement("HotelImgLarge", htldesc.Element("Image1URL").Value),
                                         new XElement("MapLink", ""),
                                         new XElement("Longitude", htldesc.Element("Longitude").Value),
                                         new XElement("Latitude", htldesc.Element("Latitude").Value),
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
            }
            else
            {
                _travayoResp = new XElement("searchResponse", new XElement("Hotels", null));
            }
            return _travayoResp;
        }

        private decimal minHotelPrice(XElement rooms)
        {
            int totalrooms = this.travyoReq.Descendants("RoomPax").Count();
            decimal minrt = 0;
            try
            {
                List<XElement> tttroom = rooms.Descendants("RoomTypes").Elements("RoomType").ToList();
                if (rooms.Descendants("RoomTypes").Descendants("RSP").Any())
                {
                    for (int i = 0; i < totalrooms; i++)
                    {
                        var cost = tttroom.Where(x => x.Descendants("Seq").FirstOrDefault().Value == Convert.ToString(i + 1)).Min(x => (decimal)x.Element("RSP"));
                        minrt += cost;
                    }
                }
                else
                {
                    for (int i = 0; i < totalrooms; i++)
                    {
                        var cost = tttroom.Where(x => x.Descendants("Seq").FirstOrDefault().Value == Convert.ToString(i + 1)).Min(x => (decimal)x.Element("Total"));
                        minrt += cost;
                    }
                }
            }
            catch { }
            return minrt;
        }

        public XElement HotelTag(List<XElement> hlst)
        {
            XElement htlItem;
            if (hlst != null)
            {
                var result = from itm in hlst
                             select new XElement("fieldValue", itm.Value);
                htlItem = new XElement("fieldValues", result);
            }
            else
            {
                htlItem = new XElement("fieldValues", new XElement("fieldValue", 0));
            }
            return htlItem;
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



        ~JacService()
        {
            Dispose(false);
        }

















        #endregion





    }
}