using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.RTS
{
    public class RTS_PriceChangeChk
    {
        public XDocument PriceChkReq(XElement Req)
        {
            //string sitecode = ConfigurationManager.AppSettings["RTSitecode"].ToString();
            //string Password = ConfigurationManager.AppSettings["RTSPassword"].ToString();
            //string Requetype = ConfigurationManager.AppSettings["RTSReqType"].ToString();           
            //string RTSalesSiteCode = ConfigurationManager.AppSettings["RTSalesSiteCode"].ToString();
            //string RTSClientCurrencyCode = ConfigurationManager.AppSettings["RTSClientCurrencyCode"].ToString();
            #region Credential
            XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string sitecode = suppliercred.Descendants("RTSitecode").FirstOrDefault().Value;
            string Password = suppliercred.Descendants("RTSPassword").FirstOrDefault().Value;
            string Requetype = suppliercred.Descendants("RTSReqType").FirstOrDefault().Value;
            string RTSalesSiteCode = suppliercred.Descendants("RTSalesSiteCode").FirstOrDefault().Value;
            string RTSClientCurrencyCode = suppliercred.Descendants("RTSClientCurrencyCode").FirstOrDefault().Value;
            #endregion

                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace rts = "http://www.rts.co.kr/";
                IEnumerable<XElement> lst = from htl in Req.Descendants("HotelPreBookingRequest")
                                            select new XElement(soap + "Envelope",
                                                                            new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                                             new XAttribute(XNamespace.Xmlns + "rts", rts),
                                                                             new XElement(soap + "Header",
                                                                                 new XElement(rts + "BaseInfo",
                                                                                 new XElement(rts + "SiteCode",sitecode),
                                                                                  new XElement(rts + "Password", Password),
                                                                                   new XElement(rts + "RequestType", Requetype))),
                                                                                   new XElement(soap + "Body",
                                                                                       new XElement(rts + "GetHotelPriceCheck",
                                                                                           //new XElement(rts + "ItemCode", htl.Element("HotelID").Value),
                                                                                           new XElement(rts + "ItemCode", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                                                            new XElement(rts + "ItemNo", htl.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value),
                                                                                           new XElement(rts + "NetPartnerPrice", htl.Element("TotalRoomRate").Value),
                                                                                           new XElement(rts + "RoomTypeCode", htl.Descendants("Room").FirstOrDefault().Attribute("ID").Value),
                                                                                             new XElement(rts + "CheckInDate", JacHelper.MyDate(htl.Element("FromDate").Value)),
                                                                                                new XElement(rts + "SalesSiteCode", RTSalesSiteCode),
                                                                                                new XElement(rts + "ClientCurrencyCode", RTSClientCurrencyCode),
                                                                                                new XElement(rts + "SellerMarkupPrice", htl.Descendants("Room").FirstOrDefault().Attribute("OccupancyID").Value))));


                //string RTSrhhtlURL = ConfigurationManager.AppSettings["RTSrhhtlURL"].ToString();
                string RTSrhhtlURL = suppliercred.Descendants("RTSrhhtlURL").FirstOrDefault().Value;
                RequestClass obj = new RequestClass();
                XDocument Responce = obj.HttpPostRequest(RTSrhhtlURL, lst.FirstOrDefault().ToString(), Req, "PriceChangeCheck", 4);
                return Responce;

            
        }
    }
}