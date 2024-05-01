using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Transfer.Model;
using TravillioXMLOutService.Transfer.Repository;
using TravillioXMLOutService.Transfer.Helper;
using TravillioXMLOutService.Models.HotelBeds;

namespace TravillioXMLOutService.Transfer.Service
{
    public class HotelBedService
    {
        XElement travyoReq = null;
        XElement travayooResp = null;
        HBCredentials model;
        HotelBedRepository _repo;
        RequestModel reqModel;
        int guestId = 0;
        public HotelBedService(string customerId)
        {
            model = new HBCredentials();
            #region Credentials
            XElement _credentials = CommonHelper.ReadCredential(customerId, model.SupplierId.ToString());
            try
            {
                model.ServiceHost = _credentials.Element("ServiceHost").Value;
                model.UserName = _credentials.Element("UserName").Value;
                model.Password = _credentials.Element("Password").Value;
                _repo = new HotelBedRepository(model);
            }
            catch(Exception ex)
            {

            }
            #endregion
        }

        #region HotelBedsRequest

        public XElement SearchRequest(XElement _travyoReq)
        {
            try
            {
                int searchType = _travyoReq.Descendants("Itinerary").Count();
                travyoReq = _travyoReq;
                reqModel = CreateReqModel(_travyoReq);
                XElement _hbReq = CreateRequest(_travyoReq);
                reqModel.RequestStr = _hbReq.ToString();
                reqModel.EndTime = DateTime.Now;
                reqModel.ResponseStr = _repo.GetResponse(reqModel);
                XElement xmlResp = XElement.Parse(reqModel.ResponseStr);
                xmlResp = xmlResp.RemoveAllNamespaces();
                int error = xmlResp.Descendants("Error").Count();
                if (error == 0)
                {
                    #region CreateTravayooRespone
                    string pickupType = _travyoReq.Descendants("Itinerary").FirstOrDefault().Element("PickupLocation").Attribute("type").Value;
                    string dropupType = _travyoReq.Descendants("Itinerary").FirstOrDefault().Element("DestinationLocation").Attribute("type").Value;

                    string pickupCode = _hbReq.Descendants("AvailData").FirstOrDefault().Element("PickupLocation").Element("Code").Value;
                    string dropupCode = _hbReq.Descendants("AvailData").FirstOrDefault().Element("DestinationLocation").Element("Code").Value;

                    int inCount = xmlResp.Descendants("ServiceTransfer").Where(x => x.Element("PickupLocation").Element("Code").Value == pickupCode).Count();
                    int outCount = xmlResp.Descendants("ServiceTransfer").Where(x => x.Element("PickupLocation").Element("Code").Value == dropupCode).Count();

                    var InList = xmlResp.Descendants("ServiceTransfer").Where(x => x.Element("PickupLocation").Element("Code").Value == pickupCode);
                    XElement TransferList = null;

                    string[] parmArray = new string[10];
                    string JTime = string.Empty;
                    if (xmlResp.Descendants("EstimatedTransferDuration").Count() > 0)
                    {
                        JTime = xmlResp.Descendants("EstimatedTransferDuration").FirstOrDefault().Value;
                    }
                    parmArray[0] = JTime;
                    parmArray[1] = pickupType;
                    parmArray[2] = dropupType;
                    if ((searchType == 2) && (inCount > 0 && outCount > 0))
                    {

                        var OutList = xmlResp.Descendants("ServiceTransfer").Where(x => x.Element("PickupLocation").Element("Code").Value == dropupCode);

                        var result = from Inway in xmlResp.Descendants("ServiceTransfer").Where(x => x.Element("PickupLocation").Element("Code").Value == pickupCode)
                                     join Outway in xmlResp.Descendants("ServiceTransfer").Where(x => x.Element("PickupLocation").Element("Code").Value == dropupCode)
                                     on mapProduct(Inway.Element("ProductSpecifications")) equals mapProduct(Outway.Element("ProductSpecifications"))
                                     select new XElement("Vehicle", new XAttribute("SupplierID", model.SupplierId),
                                         new XAttribute("Currency", Inway.Element("Currency").Attribute("code").Value),
                                         BindTrip(Inway, "IN", Inway.Attribute("availToken").Value, parmArray),
                                         BindTrip(Outway, "OUT", Outway.Attribute("availToken").Value, parmArray));
                        TransferList = new XElement("VehicleList", result);


                    }
                    if (searchType == 1)
                    {
                        var result = from Inway in xmlResp.Descendants("ServiceTransfer").Where(x => x.Element("PickupLocation").Element("Code").Value == pickupCode)
                                     select new XElement("Vehicle", new XAttribute("SupplierID", model.SupplierId),
                                         new XAttribute("Currency", Inway.Element("Currency").Attribute("code").Value),
                                         BindTrip(Inway, "IN", Inway.Attribute("availToken").Value, parmArray));
                        TransferList = new XElement("VehicleList", result);
                    }

                    if (TransferList != null)
                    {

                        var paxItem = _travyoReq.Descendants("Occupancy").FirstOrDefault();
                        travayooResp = new XElement("SearchResponse",
                            new XElement("Services",
                                new XElement("ServiceTransfer",
                                    new XAttribute("adult", paxItem.Attribute("adult").Value),
                                    new XAttribute("child", paxItem.Attribute("child").Value),
                                    new XAttribute("childage", GetAgeString(paxItem.Descendants("childage"))),
                                    TransferList)));
                    }
                    else
                    {
                        travayooResp = new XElement("SearchResponse",
                                                     new XElement("ErrorTxt", "for rountrip only one side transfer available"));
                    }
                    #endregion
                }
                else
                {
                    travayooResp = new XElement("SearchResponse",
                               new XElement("ErrorTxt", xmlResp.Descendants("DetailedMessage").FirstOrDefault().Value));
                }
            }
            catch (Exception ex)
            {
                travayooResp = new XElement("SearchResponse",
                                  new XElement("ErrorTxt", ex.Message));
                throw ex;
            }
            return travayooResp;
        }
        public XElement CxlPolicyRequest(XElement _travyoReq)
        {
            try
            {
                HotelBeds_Detail hbcxlpolicy = new HotelBeds_Detail();
                HB_CXLPolicyDetail trfhbdetail = new HB_CXLPolicyDetail();
                hbcxlpolicy.TrackNumber = _travyoReq.Attribute("TransID").Value;
                DataTable dtcxlpolicy = trfhbdetail.GetCXLPolicy_HotelBeds(hbcxlpolicy);
                string cxlresp = string.Empty;
                if (dtcxlpolicy != null)
                {
                    cxlresp = dtcxlpolicy.Rows[0]["logresponsexml"].ToString();
                }
                XElement xmlResp = XElement.Parse(cxlresp);
                xmlResp = xmlResp.RemoveAllNamespaces();

                var joinData = from trip in xmlResp.Descendants("ServiceTransfer")
                               join req in _travyoReq.Descendants("Itinerary") on trip.Element("TransferInfo").Element("Code").Value equals req.Element("TransferCode").Value
                               select new XElement("Transfer", TransferCxlPolicy(trip.Descendants("CancellationPolicy")));


                var cxlPolicy = MergCxlPolicy(joinData.ToList());
                travayooResp = new XElement("TransferCXLPolicyResponse",
                                      new XElement("Services",
                                          new XElement("ServiceTransfer",
                                              new XElement("VehicleList",
                                                   new XElement("Vehicle", new XAttribute("SupplierID", model.SupplierId),
                                                       cxlPolicy
                                         )))));
            }
            catch (Exception ex)
            {
                travayooResp = new XElement("TransferCXLPolicyResponse",
                                   new XElement("ErrorTxt", ex.Message));
                throw ex;
            }
            return travayooResp;
        }
        public XElement PreBookingRequest(XElement _travyoReq)
        {
            try
            {
                int searchType = _travyoReq.Descendants("Itinerary").Count();

                string shopingCartId = string.Empty;
                travyoReq = _travyoReq;
                bool flag = true;
                List<XElement> responseList = new List<XElement>();

                foreach (XElement item in _travyoReq.Descendants("Itinerary"))
                {
                    reqModel = CreateReqModel(_travyoReq);
                    XElement _hbReq = CreatePreBookRequest(_travyoReq, item, shopingCartId);
                    reqModel.RequestStr = _hbReq.ToString();
                    reqModel.EndTime = DateTime.Now;
                    reqModel.ResponseStr = _repo.GetResponse(reqModel);

                    XElement xmlResp = XElement.Parse(reqModel.ResponseStr);
                    xmlResp = xmlResp.RemoveAllNamespaces();
                    int error = xmlResp.Descendants("Error").Count();
                    if (error == 0)
                    {
                        shopingCartId = xmlResp.Element("Purchase").Attribute("purchaseToken").Value;
                        xmlResp = xmlResp.Element("Purchase");
                        xmlResp.Add(new XAttribute("tripType", item.Attribute("type").Value));


                        if (item.Attribute("type").Value == "OUT")
                        {
                            string code = responseList.Descendants("Service").FirstOrDefault().Attribute("SPUI").Value;
                            xmlResp.Descendants("Service").Where(el => el.Attribute("SPUI").Value == code).Remove();
                        }
                        responseList.Add(xmlResp);
                    }
                    else
                    {
                        travayooResp = new XElement("PrebookResponse",
                                 new XElement("ErrorTxt", xmlResp.Descendants("DetailedMessage").FirstOrDefault().Value));
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    XElement suplResponseList = new XElement("ResponseList", responseList);
                    travayooResp = CreatePreBookResponse(suplResponseList);
                    var cxlPolicy = MergCxlPolicy(travayooResp.Descendants("Transfer").ToList());
                    travayooResp.Descendants("CancellationPolicies").Remove();
                    travayooResp.Descendants("Vehicle").FirstOrDefault().AddFirst(cxlPolicy);
                }
            }
            catch (Exception ex)
            {

                travayooResp = new XElement("PrebookResponse",
                              new XElement("ErrorTxt", ex.Message));

                throw ex;
            }
            return travayooResp;
        }
        public XElement ConfirmRequest(XElement _travyoReq)
        {
            try
            {
                travyoReq = _travyoReq;
                reqModel = CreateReqModel(_travyoReq);
                XElement _hbReq = CreateConfirmReq(_travyoReq);
                reqModel.RequestStr = _hbReq.ToString();
                reqModel.ResponseStr = _repo.GetResponse(reqModel);
                reqModel.EndTime = DateTime.Now;
                XElement xmlResp = XElement.Parse(reqModel.ResponseStr);
                xmlResp = xmlResp.RemoveAllNamespaces();
                int error = xmlResp.Descendants("Error").Count();
                if (error == 0)
                {
                    xmlResp = xmlResp.Element("Purchase");
                    var result = from Inway in xmlResp.Descendants("Service")
                                 select new XElement("Transfer",
                                     new XAttribute("SPUICODE", Inway.Attribute("SPUI").Value),
                                                       new XAttribute("type", Inway.Attribute("transferType").Value),
                                                       new XAttribute("amount", Inway.Element("TotalAmount").Value),
                                                       new XAttribute("bookRefno", Inway.Element("Reference").Element("FileNumber").Value),
                                                       new XAttribute("status", Inway.Element("Status").Value),
                                                       new XAttribute("PickUpTime", Inway.Element("TransferPickupTime") != null ? Inway.Element("TransferPickupTime").Attribute("time").Value.AlterFormat("HHmm", "HH:mm") : ""),
                                                       new XAttribute("PickUpDate", Inway.Element("TransferPickupTime") != null ? Inway.Element("TransferPickupTime").Attribute("date").Value.AlterFormat("yyyyMMdd", "d MMM, yy") : ""),
                                                        new XElement("voucherRemarks",
                                                            Inway.Element("Supplier").Attribute("name").Value + " vatNumber " + Inway.Element("Supplier").Attribute("vatNumber").Value),
                                                               new XElement("PickUp", LocationAddress(Inway.Descendants("PickupLocation").FirstOrDefault().Element("LocationInformation"))),
                                                               new XElement("DropOff", LocationAddress(Inway.Descendants("DestinationLocation").FirstOrDefault().Element("LocationInformation"))),

                                                        new XElement("web", new XAttribute("webTime",
                                                            Inway.Element("TimeBeforeConsultingWeb").GetValueOrDefault("")),
                                                            Inway.Element("TransferWebInformation").GetValueOrDefault()),
                                                            (Inway.Element("ContactInfoList") != null ? Inway.Element("ContactInfoList") : new XElement("ContactInfoList", null)),
                                                            new XElement("MeetingPoint", (Inway.Element("TransferPickupInformation") != null ? Inway.Element("TransferPickupInformation").Element("Description").Value : null)));
                    travayooResp = new XElement("bookResponse",
                                       new XElement("Services",
                                           new XElement("ServiceTransfer",
                                               new XAttribute("bookConfirmno", xmlResp.Element("Reference").Element("FileNumber").Value),
                                               new XAttribute("voucherRefno", xmlResp.Element("Reference").Element("IncomingOffice").Attribute("code").Value + "-" + xmlResp.Element("Reference").Element("FileNumber").Value),
                                               new XAttribute("bookToken", xmlResp.Attribute("purchaseToken").Value),
                                               new XAttribute("currencyCode", xmlResp.Element("Currency").Attribute("code").Value),
                                               new XElement("Vehicle", new XAttribute("SupplierID", model.SupplierId), result))));
                }
                else
                {
                    travayooResp = new XElement("bookResponse",
                            new XElement("ErrorTxt", xmlResp.Descendants("DetailedMessage").FirstOrDefault().Value));
                }
            }
            catch (Exception ex)
            {
                travayooResp = new XElement("bookResponse",
                              new XElement("ErrorTxt", ex.Message));
                throw ex;
            }
            return travayooResp;
        }
        public string LocationAddress(XElement loc)
        {
            string address = string.Empty;
            if (loc != null)
            {
                address += loc.Element("Address").GetValuewithSuffix();
                address.Trim();
                address += loc.Element("Town").GetValuewithSuffix();
                address.Trim();
                address += loc.Element("Zip").GetValuewithSuffix();
                address.Trim();
                address += loc.Element("Description").GetValuewithSuffix();


                if (address.EndsWith(","))
                {
                    address = address.Remove(address.Length - 1);
                }



            }
            return address;
        }






        public XElement CancelRequest(XElement _travyoReq)
        {
            try
            {
                travyoReq = _travyoReq;
                reqModel = CreateReqModel(_travyoReq);
                XElement _hbReq = CreateCxlRequest(_travyoReq);
                reqModel.RequestStr = _hbReq.ToString();
                reqModel.EndTime = DateTime.Now;
                reqModel.ResponseStr = _repo.GetResponse(reqModel);
                XElement xmlResp = XElement.Parse(reqModel.ResponseStr);
                xmlResp = xmlResp.RemoveAllNamespaces();
                int error = xmlResp.Descendants("Error").Count();
                if (error == 0)
                {
                    #region CreateTravayooRespone

                    travayooResp = new XElement("TransferCancelResponse",
                                     new XElement("Services",
                                         new XElement("ServiceTransfer",
                                             new XAttribute("amount", xmlResp.Element("Amount").Value),
                                             new XAttribute("status", xmlResp.Element("Purchase").Element("Status").Value == "CANCELLED" ? "Success" : "Failed"))));
                    #endregion
                }
                else
                {
                    travayooResp = new XElement("TransferCancelResponse",
                               new XElement("ErrorTxt", xmlResp.Descendants("DetailedMessage").FirstOrDefault().Value));
                }

            }
            catch (Exception ex)
            {
                travayooResp = new XElement("TransferCancelResponse",
                                  new XElement("ErrorTxt", ex.Message));
                throw ex;
            }
            return travayooResp;

        }

        #endregion

        #region Method


        XElement Credentials()
        {
            XElement _login = new XElement("Credentials",
                                 new XElement("User", model.UserName),
                                 new XElement("Password", model.Password),
                                 new XElement("Platform", model.Platform));
            return _login;
        }









        Product mapProduct(XElement inWay)
        {
            Product _prod = new Product();
            _prod.ServiceType = inWay.Element("MasterServiceType").Attribute("code").Value;
            _prod.ProductType = inWay.Element("MasterProductType").Attribute("code").Value;
            _prod.VehicleType = inWay.Element("MasterVehicleType").Attribute("code").Value;
            return _prod;
        }
        struct Product
        {
            public string ServiceType { get; set; }
            public string ProductType { get; set; }
            public string VehicleType { get; set; }
        }

        RequestModel CreateReqModel(XElement _travyoReq)
        {
            reqModel = new RequestModel();
            reqModel.StartTime = DateTime.Now;
            reqModel.Customer = Convert.ToInt64(_travyoReq.Attribute("CustomerID").Value);
            reqModel.TrackNo = _travyoReq.Attribute("TransID").Value;
            reqModel.ActionId = (int)_travyoReq.Name.LocalName.GetAction();
            reqModel.Action = _travyoReq.Name.LocalName.GetAction().ToString();
            return reqModel;
        }
        XElement CreateRequest(XElement _travyoReq)
        {
            XNamespace defaultNamespace = "http://www.hotelbeds.com/schemas/2005/06/messages";
            XNamespace xmlns2 = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xmlns3 = "http://www.hotelbeds.com/schemas/2005/06/messages TransferValuedAvailRQ.xsd";
            XElement hbReq;
            int tripNo = _travyoReq.Descendants("Itinerary").Count();
            hbReq = new XElement(
                     new XElement("TransferValuedAvailRQ",
                         new XAttribute("echoToken", "DummyEchoToken"),
                          new XAttribute("sessionId", "134132121666"),
                           new XAttribute(XNamespace.Xmlns + "xsi", xmlns2),
                           new XAttribute(XNamespace.Xmlns + "schemaLocation", xmlns3),
                              new XAttribute("version", "2013/12"),
                               new XElement("Language", _travyoReq.Element("language").Value),
                              Credentials(),
                                   from tripItem in _travyoReq.Descendants("Itinerary")
                                   select locationElement(tripItem, PaxElemnt(_travyoReq.Element("Occupancy"))),
                                   new XElement("ReturnContents", "Y")));
            return hbReq;
        }

        XElement TransferCxlPolicy(IEnumerable<XElement> Cxllst)
        {


            var result = Cxllst.Select(x => new XElement("CancellationPolicy",
                              new XAttribute("LastCxlDate", x.Attribute("dateFrom").Value.GetDateTime(x.Attribute("time").Value, "yyyyMMdd HHmm").ToString("yyyyMMdd HHmm")),
                              new XAttribute("CxlAmount", x.Attribute("amount").Value),
                              new XAttribute("NoShow", 0)));
            XElement cxlItem = new XElement("CancellationPolicies", result);
            return cxlItem;
        }
        XElement MergCxlPolicy(List<XElement> PolicyList)
        {
            List<XElement> cxlList = new List<XElement>();
            IEnumerable<XElement> dateLst = PolicyList.Descendants("CancellationPolicy").
               GroupBy(r => new
               {
                   r.Attribute("LastCxlDate").Value.GetDateTime("yyyyMMdd HHmm").Date,
                   noshow = r.Attribute("NoShow").Value
               }).Select(y => y.First()).
               OrderBy(p => p.Attribute("LastCxlDate").Value.GetDateTime("yyyyMMdd HHmm"));



            if (dateLst.Count() > 0)
            {
                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCxlDate").Value;
                    string noShow = item.Attribute("NoShow").Value;
                    decimal datePrice = 0.0m;
                    foreach (var rm in PolicyList)
                    {
                        var prItem = rm.Descendants("CancellationPolicy").
                       Where(pq => (pq.Attribute("NoShow").Value == noShow && pq.Attribute("LastCxlDate").Value == date)).
                       FirstOrDefault();
                        if (prItem != null)
                        {
                            var price = prItem.Attribute("CxlAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            if (noShow == "1")
                            {
                                datePrice += Convert.ToDecimal(rm.Element("TotalAmount").Value);
                            }
                            else
                            {
                                var lastItem = rm.Descendants("CancellationPolicy").
                                    Where(pq => (pq.Attribute("NoShow").Value == noShow && pq.Attribute("LastCxlDate").Value.GetDateTime("yyyyMMdd HHmm") < date.GetDateTime("yyyyMMdd HHmm")));
                                if (lastItem.Count() > 0)
                                {
                                    var lastDate = lastItem.Max(y => y.Attribute("LastCxlDate").Value);
                                    var lastprice = rm.Descendants("CancellationPolicy").
                                        Where(pq => (pq.Attribute("NoShow").Value == noShow && pq.Attribute("LastCxlDate").Value == lastDate)).
                                        FirstOrDefault().Attribute("CxlAmount").Value;
                                    datePrice += Convert.ToDecimal(lastprice);
                                }
                            }
                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy",
                        new XAttribute("LastCxlDate", date),
                        new XAttribute("CxlAmount", datePrice),
                        new XAttribute("NoShow", noShow));
                    cxlList.Add(pItem);
                }

                cxlList = cxlList.GroupBy(x => new { x.Attribute("LastCxlDate").Value.GetDateTime("yyyyMMdd HHmm").Date, x.Attribute("NoShow").Value }).
                    Select(y => new XElement("CancellationPolicy",
                        new XAttribute("LastCxlDate", y.Key.Date.ToString("d MMM, yy")),
                        new XAttribute("CxlAmount", y.Max(p => Convert.ToDecimal(p.Attribute("CxlAmount").Value))),
                        new XAttribute("NoShow", y.Key.Value))).OrderBy(p => p.Attribute("LastCxlDate").Value.GetDateTime("d MMM, yy").Date).ToList();



                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("CxlAmount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCxlDate", fItem.Attribute("LastCxlDate").Value.GetDateTime("d MMM, yy").AddDays(-1).Date.ToString("d MMM, yy")), new XAttribute("CxlAmount", "0.00"), new XAttribute("NoShow", "0")));
                }
            }
            XElement cxlItem = new XElement("CancellationPolicies", cxlList);
            return cxlItem;
        }
        XElement CreatePreBookRequest(XElement _travyoReq, XElement trip, string cartNo)
        {
            XNamespace defaultNamespace = "http://www.hotelbeds.com/schemas/2005/06/messages";
            XNamespace xmlns2 = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xmlns3 = "http://www.hotelbeds.com/schemas/2005/06/messages TransferValuedAvailRQ.xsd";
            XElement hbReq;
            int tripNo = _travyoReq.Descendants("Itinerary").Count();
            hbReq = new XElement("ServiceAddRQ",
                         new XAttribute("echoToken", "DummyEchoToken"),
                           new XAttribute(XNamespace.Xmlns + "xsi", xmlns2),
                           new XAttribute(XNamespace.Xmlns + "schemaLocation", xmlns3),
                              new XAttribute("version", "2013/12"),
                               new XElement("Language", _travyoReq.Element("language").Value),
                                   Credentials(),
                                   ServiceElement(trip, PreBookPaxElemnt(_travyoReq.Element("Occupancy"))));

            if (!string.IsNullOrEmpty(cartNo))
            {
                hbReq.Add(new XAttribute("purchaseToken", cartNo));

            }
            return hbReq;
        }
        XElement CreatePreBookResponse(XElement _responseList)
        {
            XElement _resp;

            string[] parmArray = new string[10];
            parmArray[0] = "";
            string pickupType = travyoReq.Descendants("Itinerary").FirstOrDefault().Element("PickupLocation").Attribute("type").Value;
            string dropupType = travyoReq.Descendants("Itinerary").FirstOrDefault().Element("DestinationLocation").Attribute("type").Value;
            parmArray[1] = pickupType;
            parmArray[2] = dropupType;

            var result = from Inway in _responseList.Descendants("Service")
                         select BindTrip(Inway, Inway.Parent.Parent.Attribute("tripType").Value, Inway.Parent.Parent.Attribute("purchaseToken").Value, parmArray);
            var paxItem = travyoReq.Descendants("Occupancy").FirstOrDefault();
            _resp = new XElement("PrebookResponse",
                new XElement("Services",
                    new XElement("ServiceTransfer",
                        new XAttribute("adult", paxItem.Attribute("adult").Value),
                        new XAttribute("child", paxItem.Attribute("child").Value),
                        new XAttribute("childage", GetAgeString(paxItem.Descendants("childage"))),
                        new XElement("VehicleList", new XElement("Vehicle", new XAttribute("SupplierID", model.SupplierId), result)))));
            return _resp;
        }
        XElement PreBookPaxElemnt(XElement pax)
        {
            XElement paxItem;
            List<XElement> adultAge;
            IEnumerable<XElement> childAge;
            int adult = Convert.ToInt16(pax.Attribute("adult").Value);
            adultAge = new List<XElement>();
            for (int i = 0; i < adult; i++)
            {
                adultAge.Add(new XElement("Customer", new XAttribute("type", "AD"), new XElement("Age", 30)));
            }
            int child = Convert.ToInt16(pax.Attribute("child").Value);
            if (child > 0)
            {
                childAge = from gst in pax.Descendants("childage")
                           select new XElement("Customer",
                               new XAttribute("type", "CH"), new XElement("Age", gst.Value));
            }
            else
            {
                childAge = null;
            }
            paxItem = new XElement("Paxes",
                               new XElement("AdultCount", pax.Attribute("adult").Value),
                               new XElement("ChildCount", pax.Attribute("child").Value),
                               new XElement("GuestList", adultAge, childAge));
            return paxItem;
        }
        string GetAgeString(IEnumerable<XElement> chldlst)
        {
            string AgeStr = string.Empty;
            if (!chldlst.IsNullOrEmpty())
            {
                foreach (var item in chldlst)
                {
                    AgeStr += item.Value;
                    AgeStr += ",";
                }
                AgeStr = AgeStr.Remove(AgeStr.Length - 1, 1);
            }
            return AgeStr;
        }
        XElement Guideline(XElement guid)
        {
            XElement guidItem;
            string Desc = string.Empty;
            string title = string.Empty;
            if (guid.Element("Value") != null)
            {
                Desc = guid.Element("Description").Value + "  is " + guid.Element("Value").Value + " " + guid.Element("Metric").Value;
            }
            else if (guid.Element("DetailedDescription") != null)
            {
                Desc = guid.Element("DetailedDescription").Value;
                title = guid.Element("Description").Value;
            }
            else
            {
                Desc = guid.Element("Description").Value;
            }
            guidItem = new XElement("Description", new XAttribute("id", guid.Attribute("id").GetValueOrDefault("")), new XAttribute("title", title), Desc);
            return guidItem;
        }
        string WaitingTime(XElement item)
        {
            string waiting = string.Empty;
            if (item != null)
            {
                waiting = item.Attribute("time").GetValueOrDefault("") + " minutes";
            }
            return waiting;
        }
        string VehicleName(XElement item)
        {
            string Vehicle = string.Empty;
            if (item != null)
            {
                Vehicle = item.Element("MasterServiceType").Attribute("name").Value;
                Vehicle += " ";
                Vehicle += item.Element("MasterProductType").Attribute("name").Value;
                Vehicle += " ";
                Vehicle += item.Element("MasterVehicleType").Attribute("name").Value;
            }
            return Vehicle;
        }
        XElement PickUpDate(XElement Inway, string type)
        {
            XElement pickUp;
            if (Inway.Element("PickupLocation").Element("TerminalType") != null)
            {
                string strDate = string.Empty, strTime = string.Empty;
                var item = Inway.Element("ArrivalTravelInfo").Element("ArrivalInfo").Element("DateTime");

                strDate = item.Attribute("date").GetValueOrDefault();
                if (!string.IsNullOrEmpty(strDate))
                {

                    strDate = strDate.AlterFormat("yyyyMMdd", "d MMM, yy");
                }
                strTime = item.Attribute("time").GetValueOrDefault();
                if (!string.IsNullOrEmpty(strTime))
                {
                    strTime = strTime.AlterFormat("HHmm", "HH:mm");
                }
                pickUp = new XElement("PickupTime",
                    new XAttribute("date", strDate),
                    new XAttribute("time", strTime));
            }
            else
            {
                var item = travyoReq.Descendants("Itinerary").Where(x => x.Attribute("type").Value == type).FirstOrDefault().Element("PickupLocation").Element("PickupTime");
                pickUp = item;
            }
            return pickUp;
        }
        XElement DropUpDate(XElement Inway, string type)
        {
            XElement dropUp;
            if (Inway.Element("DestinationLocation").Element("TerminalType") != null)
            {
                string strDate = string.Empty, strTime = string.Empty;
                var item = Inway.Element("DepartureTravelInfo").Element("DepartInfo").Element("DateTime");



                strDate = item.Attribute("date").GetValueOrDefault();
                if (!string.IsNullOrEmpty(strDate))
                {

                    strDate = strDate.AlterFormat("yyyyMMdd", "d MMM, yy");
                }
                strTime = item.Attribute("time").GetValueOrDefault();
                if (!string.IsNullOrEmpty(strTime))
                {
                    strTime = strTime.AlterFormat("HHmm", "HH:mm");
                }

                dropUp = new XElement("DropOffTime",
                new XAttribute("date", strDate),
                new XAttribute("time", strTime));
            }
            else
            {
                var item = travyoReq.Descendants("Itinerary").Where(x => x.Attribute("type").Value == type).FirstOrDefault().Element("DestinationLocation").Element("DropOffTime");
                dropUp = item;
            }
            return dropUp;
        }
        XElement PaxElemnt(XElement pax)
        {
            XElement paxItem;
            List<XElement> adultAge;
            IEnumerable<XElement> childAge;
            int adult = Convert.ToInt16(pax.Attribute("adult").Value);
            adultAge = new List<XElement>();
            for (int i = 0; i < adult; i++)
            {
                adultAge.Add(new XElement("Customer", new XAttribute("type", "AD"), new XElement("Age", 30)));
            }
            int child = Convert.ToInt16(pax.Attribute("child").Value);
            if (child > 0)
            {
                childAge = from gst in pax.Descendants("childage")
                           select new XElement("Customer",
                               new XAttribute("type", "CH"), new XElement("Age", gst.Value));
            }
            else
            {
                childAge = null;
            }
            paxItem = new XElement("Occupancy",
                               new XElement("AdultCount", pax.Attribute("adult").Value),
                               new XElement("ChildCount", pax.Attribute("child").Value),
                               new XElement("GuestList", adultAge, childAge));
            return paxItem;
        }
        XElement locationElement(XElement Inway, XElement paxItem)
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XElement tripItem;
            string type = string.Empty;
            int InType = Convert.ToInt16(Inway.Element("PickupLocation").Attribute("type").Value);
            int OutType = Convert.ToInt16(Inway.Element("DestinationLocation").Attribute("type").Value);

            string strDate = string.Empty, strTime = string.Empty;
            strDate = Inway.Element("PickupLocation").Element("PickupTime").Attribute("date").GetValueOrDefault();
            strTime = Inway.Element("PickupLocation").Element("PickupTime").Attribute("time").GetValueOrDefault();
            DateTime pickUpdate = strDate.GetDateTime(strTime, "d MMM, yy HH:mm");

            XElement pTime = new XElement("DateTime",
         new XAttribute("date", pickUpdate.ToString("yyyyMMdd")),
         new XAttribute("time", pickUpdate.ToString("HHmm")));


            XElement source = new XElement("PickupLocation",
                new XAttribute(xsi + "type", InType == 1 ? "ProductTransferTerminal" : "ProductTransferHotel"),
                new XElement("Code", Inway.Element("PickupLocation").Element("Code").Value), InType == 1 ? pTime : null);


            XElement dTime = new XElement("DateTime",
         new XAttribute("date", pickUpdate.AddHours(+4).ToString("yyyyMMdd")),
         new XAttribute("time", pickUpdate.AddHours(+4).ToString("HHmm")));


            XElement destination = new XElement("DestinationLocation",
                new XAttribute(xsi + "type", OutType == 1 ? "ProductTransferTerminal" : "ProductTransferHotel"),
                new XElement("Code", Inway.Element("DestinationLocation").Element("Code").Value), OutType == 1 ? dTime : null);
            tripItem = new XElement("AvailData",
                new XAttribute("type", Inway.Attribute("type").Value),
                new XElement("ServiceDate",
                    new XAttribute("date", OutType == 1 ? dTime.Attribute("date").Value : pTime.Attribute("date").Value),
                    new XAttribute("time", OutType == 1 ? dTime.Attribute("time").Value : pTime.Attribute("time").Value)),
                    paxItem, source, destination);
            return tripItem;
        }

        XElement ServiceElement(XElement Inway, XElement paxItem)
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XElement tripItem;
            string type = string.Empty;

            int InType = Convert.ToInt16(Inway.Element("PickupLocation").Attribute("type").Value);
            int OutType = Convert.ToInt16(Inway.Element("DestinationLocation").Attribute("type").Value);


            string strDate = string.Empty, strTime = string.Empty;
            strDate = Inway.Element("PickupLocation").Element("PickupTime").Attribute("date").GetValueOrDefault();
            strTime = Inway.Element("PickupLocation").Element("PickupTime").Attribute("time").GetValueOrDefault();
            DateTime pickUpdate = strDate.GetDateTime(strTime, "d MMM, yy HH:mm");


            XElement pTime = new XElement("DateTime",
         new XAttribute("date", pickUpdate.ToString("yyyyMMdd")),
         new XAttribute("time", pickUpdate.ToString("HHmm")));

            XElement source = new XElement("PickupLocation",
                new XAttribute(xsi + "type", InType == 1 ? "ProductTransferTerminal" : "ProductTransferHotel"),
                new XElement("Code", Inway.Element("PickupLocation").Element("Code").Value), InType == 1 ? pTime : null);
            XElement dTime = new XElement("DateTime",
             new XAttribute("date", pickUpdate.AddHours(+4).ToString("yyyyMMdd")),
             new XAttribute("time", pickUpdate.AddHours(+4).ToString("HHmm")));

            XElement destination = new XElement("DestinationLocation",
                new XAttribute(xsi + "type", OutType == 1 ? "ProductTransferTerminal" : "ProductTransferHotel"),
                new XElement("Code", Inway.Element("DestinationLocation").Element("Code").Value), OutType == 1 ? dTime : null);

            tripItem = new XElement("Service",
                new XAttribute("availToken", Inway.Attribute("availToken").Value),
                      new XAttribute("transferType", Inway.Attribute("trtype").Value),
                  new XAttribute(xsi + "type", "ServiceTransfer"),
                    new XElement("ContractList", new XElement("Contract", new XElement("Name", Inway.Attribute("servicename").Value),
                        new XElement("IncomingOffice", new XAttribute("code", Inway.Attribute("servicecode").Value)))),
                new XElement("DateFrom",
                    new XAttribute("date", OutType == 1 ? pickUpdate.AddHours(+4).ToString("yyyyMMdd") : pTime.Attribute("date").Value),
                    new XAttribute("time", OutType == 1 ? pickUpdate.AddHours(+4).ToString("HHmm") : pTime.Attribute("time").Value)),
       new XElement("TransferInfo",
            new XAttribute(xsi + "type", "ProductTransfer"),
           new XElement("Code", Inway.Element("TransferInfo").Element("Code").Value),
                 new XElement("Type", new XAttribute("code", Inway.Element("TransferInfo").Element("Type").Attribute("code").Value)),
                       new XElement("VehicleType", new XAttribute("code", Inway.Element("TransferInfo").Element("VehicleType").Attribute("code").Value))),
                            paxItem, source, destination);


            return tripItem;
        }
        XElement BindTrip(XElement Inway, string tripType, string purchasetoken, string[] parmArray)
        {
            XElement tripItem;
            if (Inway != null)
            {
                string trDate = Inway.Element("DateFrom").Attribute("date").Value;
                trDate = trDate.AlterFormat("yyyyMMdd", "d MMM, yy");
                var guidList = from guid in Inway.Descendants("TransferBulletPoint")
                               select Guideline(guid);

                XElement guidItem = new XElement("Description",
                    new XAttribute("id", ""),
                    new XAttribute("title", ""),
                    Inway.Element("TransferPickupInformation") != null ? Inway.Element("TransferPickupInformation").Element("Description").Value : "");
                guidList.ToList().Add(guidItem);
                tripItem = new XElement("Transfer",
                    new XAttribute("trtype", Inway.Attribute("transferType").Value),
                     new XAttribute("travelDate", trDate),
                    new XAttribute("type", tripType),
                    new XAttribute("purchaseToken", purchasetoken),
                new XAttribute("shoppingCartNo", Inway.Attribute("SPUI").GetValueOrDefault("")),
                    new XAttribute("serviceName", Inway.Element("ContractList").Element("Contract").Element("Name").Value),
                    new XAttribute("serviceCode", Inway.Element("ContractList").Element("Contract").Element("IncomingOffice").Attribute("code").Value),
                    new XAttribute("amount", Inway.Element("TotalAmount").Value),
                    new XAttribute("currency", Inway.Element("Currency").Attribute("code").Value),
                    new XElement("PickupLocation",
                        new XElement("Type", tripType == "IN" ? parmArray[1] : parmArray[2]),
                        new XElement("Name", Inway.Element("PickupLocation").Element("Name").Value),
                        new XElement("Code", Inway.Element("PickupLocation").Element("Code").Value),
                        new XElement("TerminalType", Inway.Element("PickupLocation").Element("TerminalType").GetValueOrDefault()),
                        PickUpDate(Inway, tripType)),
                        new XElement("DestinationLocation",
                              new XElement("Type", tripType == "IN" ? parmArray[2] : parmArray[1]),
                                new XElement("Name", Inway.Element("DestinationLocation").Element("Name").Value),
                                new XElement("Code", Inway.Element("DestinationLocation").Element("Code").Value),
                                new XElement("TerminalType", Inway.Element("DestinationLocation").Element("TerminalType").GetValueOrDefault()),
                                  DropUpDate(Inway, tripType)),
                                  new XElement("ImageList",
                                      from img in Inway.Descendants("Image")
                                      select new XElement("Image",
                                          new XAttribute("type", img.Element("Type").Value), img.Element("Url").Value)),
                                          new XElement("TransferInfo",
                                        new XElement("Product", VehicleName(Inway.Element("ProductSpecifications"))),
                                        new XElement("TransferType",
                                           new XAttribute("code", Inway.Element("TransferInfo").Element("Type").Attribute("code").Value),
                                           Inway.Element("TransferInfo").Element("DescriptionList").Descendants("Description").Where(x => x.Attribute("type").Value == "GENERAL").FirstOrDefault().Value),
                                           new XElement("TransferCode", Inway.Element("TransferInfo").Element("Code").Value),
                                           new XElement("VehicleType", new XAttribute("code", Inway.Element("TransferInfo").Element("VehicleType").Attribute("code").Value),
                                               Inway.Element("TransferInfo").Element("DescriptionList").Descendants("Description").Where(x => x.Attribute("type").Value == "VEHICLE").FirstOrDefault().Value),
                                               new XElement("GuidelinesList", guidList),
                                                 new XElement("TransferCapacityList",
                                                     new XAttribute("totpax", (Inway.Element("Paxes").Element("AdultCount").ChangeToInt() + Inway.Element("Paxes").Element("ChildCount").ChangeToInt())),
                                                     new XAttribute("totlug", ""),
                                                     new XAttribute("lugtype", ""),
                                                     new XElement("Description", "")),
                                                     new XElement("JourneyTime",
                                                         new XAttribute("jtime", Inway.Element("EstimatedTransferDuration").GetValueOrDefault(parmArray[0]))),
                                                         new XElement("WaitingTime",
                                                             new XAttribute("cwtime",
                                                                 WaitingTime(Inway.Element("TransferInfo").Element("TransferSpecificContent").Element("MaximumWaitingTime"))),
                                                                 new XAttribute("domestic",
                                                                     WaitingTime(Inway.Element("TransferInfo").Element("TransferSpecificContent").Element("MaximumWaitingTimeSupplierDomestic"))),
                                                                     new XAttribute("international",
                                                                         WaitingTime(Inway.Element("TransferInfo").Element("TransferSpecificContent").Element("MaximumWaitingTimeSupplierInternational"))))),
                                                                         TransferCxlPolicy(Inway.Descendants("CancellationPolicy"))
                                                                         );
            }
            else
            {
                tripItem = null;
            }
            return tripItem;
        }
        XElement CreateConfirmReq(XElement _travyoReq)
        {


            XNamespace defaultNamespace = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace HBNamespace = "http://www.hotelbeds.com/schemas/2005/06/messages";
            XNamespace schemaLocation = "http://www.hotelbeds.com/schemas/2005/06/messages TransferValuedAvailRQ.xsd";
            XElement hbReq;
            hbReq = new XElement(HBNamespace + "PurchaseConfirmRQ",
                    new XAttribute(XNamespace.Xmlns + "xsi", defaultNamespace),
                    new XAttribute(XNamespace.Xmlns + "schemaLocation", schemaLocation),
                    new XAttribute("echoToken", "DummyEchoToken"),
                    new XAttribute("version", "2013/12"),
                    new XElement("Language", _travyoReq.Element("language").Value),
                       Credentials(),
                        new XElement("ConfirmationData",
                            new XAttribute("purchaseToken", _travyoReq.Descendants("Itinerary").FirstOrDefault().Attribute("booktoken").Value),
                            new XElement("Holder",
                                new XAttribute("type", "AD"),
                                new XElement("CustomerId", 1),
                                new XElement("Age", 30),
                                new XElement("Name", _travyoReq.Descendants("PaxItem").Where(x => x.Attribute("IsLead").Value == "true").FirstOrDefault().Element("FirstName").Value),
                                new XElement("LastName", _travyoReq.Descendants("PaxItem").Where(x => x.Attribute("IsLead").Value == "true").FirstOrDefault().Element("LastName").Value)),
                                new XElement("AgencyReference", _travyoReq.Attribute("TransactionID").Value),
                                new XElement("ConfirmationServiceDataList",
                                    from trip in _travyoReq.Descendants("Itinerary")
                                    select ConfServiceElement(trip, _travyoReq.Element("Pax")))));
            return hbReq;
        }
        XElement ConfServiceElement(XElement Inway, XElement paxItem)
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XElement tripItem;
            tripItem = new XElement("ServiceData",
                new XAttribute("SPUI", Inway.Attribute("SPUICODE").Value),
                new XAttribute(xsi + "type", "ConfirmationServiceDataTransfer"),
                //new XElement("TransferType", Inway.Attribute("trtype").Value),
                ConfPaxElemnt(paxItem),
                CommntElement(),
                ConfDateTime(Inway.Element("PickupLocation"), 1),
                ConfDateTime(Inway.Element("DestinationLocation"), 2));
            return tripItem;
        }
        XElement CommntElement()
        {
            XElement _comment = null;
            string text = travyoReq.Descendants("Comment").FirstOrDefault().Value;
            if (!string.IsNullOrEmpty(text))
            {
                _comment = new XElement("CommentList",
                        new XElement("Comment", new XAttribute("type", "INCOMING"), travyoReq.Descendants("Comment").FirstOrDefault().Value));
            }
            return _comment;
        }
        XElement ConfPaxElemnt(XElement pax)
        {
            XElement paxItem;
            var paxList = from gst in pax.Descendants("PaxItem")
                          select new XElement("Customer",
                              new XAttribute("type", gst.Attribute("type").Value),
                              new XElement("CustomerId", ++guestId),
                              new XElement("Name", gst.Element("FirstName").Value),
                              new XElement("LastName", gst.Element("LastName").Value),
                              new XElement("Age", gst.Attribute("age").Value));

            paxItem = new XElement("CustomerList", paxList);
            return paxItem;
        }
        XElement ConfDateTime(XElement source, int placeType)
        {
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            int InType = Convert.ToInt16(source.Attribute("type").Value);
            XElement pickUp;
            string strDate = string.Empty, strTime = string.Empty;
            if (placeType == 1)
            {
                strDate = source.Element("PickupTime").Attribute("date").GetValueOrDefault();
                strTime = source.Element("PickupTime").Attribute("time").GetValueOrDefault();
            }
            else
            {
                strDate = source.Element("DropOffTime").Attribute("date").GetValueOrDefault();
                strTime = source.Element("DropOffTime").Attribute("time").GetValueOrDefault();
            }
            if (!string.IsNullOrEmpty(strDate))
            {
                strDate = strDate.AlterFormat("d MMM, yy", "yyyyMMdd");
            }
            if (!string.IsNullOrEmpty(strTime))
            {
                strTime = strTime.AlterFormat("HH:mm", "HHmm");
            }
            if (placeType == 1 && InType == 1)
            {
                pickUp = new XElement("ArrivalTravelInfo",
                               new XElement("ArrivalInfo",
                                    new XAttribute(xsi + "type", "ProductTransferTerminal"),
                                   new XElement("Code", source.Element("Code").Value),
                                   new XElement("DateTime",
                                       new XAttribute("date", strDate),
                                       new XAttribute("time", strTime))),
                                       new XElement("TravelNumber", source.Element("TravelNumber").Value));


            }
            else if (placeType == 2 && InType == 1)
            {
                pickUp = new XElement("DepartureTravelInfo",
                    new XElement("DepartInfo",
                        new XAttribute(xsi + "type", "ProductTransferTerminal"),
                        new XElement("Code", source.Element("Code").Value),
                        new XElement("DateTime",
                               new XAttribute("date", strDate),
                               new XAttribute("time", strTime))),
                            new XElement("TravelNumber", source.Element("TravelNumber").Value));
            }
            else
            {
                pickUp = null;
            }
            return pickUp;
        }

        XElement CreateCxlRequest(XElement _travyoReq)
        {
            XNamespace defaultNamespace = "http://www.hotelbeds.com/schemas/2005/06/messages";
            XNamespace xmlns2 = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace xmlns3 = "http://www.hotelbeds.com/schemas/2005/06/messages PurchaseCancelRQ.xsd";
            XElement hbReq;
            string[] strConf = travyoReq.Element("ConfirmationNumber").Value.Split('-');


            hbReq = new XElement("PurchaseCancelRQ",
             new XAttribute("echoToken", "DummyEchoToken"),
             new XAttribute("type", "C"),
               new XAttribute(XNamespace.Xmlns + "xsi", xmlns2),
               new XAttribute(XNamespace.Xmlns + "schemaLocation", xmlns3),
                  new XAttribute("version", "2013/12"),
                   new XElement("Language", _travyoReq.Element("language").Value),
                      Credentials(),
                         new XElement("PurchaseReference",
                       new XElement("FileNumber", strConf[1]),
                       new XElement("IncomingOffice", new XAttribute("code", strConf[0]))));
            return hbReq;
        }

        #endregion

        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        bool disposed = false;
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
                travayooResp = null;
                model = null;
                _repo = null;
                reqModel = null;
                // Free any other managed objects here.
                //
            }
            // Free any unmanaged objects here.
            //
            disposed = true;
        }
        ~HotelBedService()
        {
            Dispose(false);
        }
        #endregion

    }
}
