using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.RTS
{
    public class RTS_BookingCXL
    {
        public XElement HtlBookingCXl(XElement req)
        {
            //string sitecode = ConfigurationManager.AppSettings["RTSitecode"].ToString();
            //string Password = ConfigurationManager.AppSettings["RTSPassword"].ToString();
            //string Requetype = ConfigurationManager.AppSettings["RTSReqType"].ToString();
            //string RTSalesUserNo = ConfigurationManager.AppSettings["RTSalesUserNo"].ToString();
            //string RTSCXlReasonCode = ConfigurationManager.AppSettings["RTSCXlReasonCode"].ToString();
            #region Credential
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string sitecode = suppliercred.Descendants("RTSitecode").FirstOrDefault().Value;
            string Password = suppliercred.Descendants("RTSPassword").FirstOrDefault().Value;
            string Requetype = suppliercred.Descendants("RTSReqType").FirstOrDefault().Value;
            string RTSalesUserNo = suppliercred.Descendants("RTSalesUserNo").FirstOrDefault().Value;
            string RTSCXlReasonCode = suppliercred.Descendants("RTSCXlReasonCode").FirstOrDefault().Value;
            #endregion
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace rts = "http://www.rts.co.kr/";
            IEnumerable<XElement> lst = from htl in req.Descendants("HotelCancellationRequest")
                                        select new XElement(soap + "Envelope",
                                                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                                         new XAttribute(XNamespace.Xmlns + "rts", rts),
                                                                         new XElement(soap + "Header",
                                                                             new XElement(rts + "BaseInfo",
                                                                             new XElement(rts + "SiteCode", sitecode),
                                                                              new XElement(rts + "Password", Password),
                                                                               new XElement(rts + "RequestType", Requetype))),
                                                                               new XElement(soap + "Body",
                                                                                   new XElement(rts + "BookingCancel",
                                                                                       new XElement(rts + "BookingCancel",
                                                                                       new XElement(rts + "LanguageCode", "EN"),
                                                                                       new XElement(rts + "BookingCode", htl.Element("BookingCode").Value),
                                                                                       new XElement(rts + "ItemNo", 0),
                                                                                       new XElement(rts + "CancelReasonCode", RTSCXlReasonCode),
                                                                                        new XElement(rts + "LastWriterUno", RTSalesUserNo)))));
                       
            string RTSBokCXlURL = suppliercred.Descendants("RTSBokCXlURL").FirstOrDefault().Value;
            RequestClass obj = new RequestClass();
            XDocument Responce = obj.HttpPostRequest(RTSBokCXlURL, lst.FirstOrDefault().ToString(), req, "Cancel", 6);
            //#region need to comment
            //XDocument Responce = XDocument.Load(HttpContext.Current.Server.MapPath(@"~\rtsfile.xml"));
            //#endregion
            IEnumerable<XElement> ele = Responce.Descendants(rts + "BookingItemInfo");
            foreach (XElement item in ele)
            {
                if (item.Element(rts + "ItemStatusCode").Value == "BS05")
                {
                    XElement cancel = Responce.Descendants(rts + "BookingItemInfo").FirstOrDefault();
                    return cancellResponce(cancel, rts, req);                   
                }
            }
            return null;
        }
        XElement cancellResponce(XElement Responce, XNamespace rts, XElement Req)
        {
            XElement CxlResponce = new XElement("HotelCancellationResponse",
                                new XElement("Rooms",
                                    new XElement("Room",
                                        new XElement("Cancellation",
                                            new XElement("Amount", Responce.Element(rts + "ClientCancelCharge").Value),
                                            new XElement("Status", "Success")))));

            IEnumerable<XElement> Descoll = Req.Descendants("HotelCancellationRequest");
            foreach (XElement item in Descoll)
            {
                item.AddAfterSelf(CxlResponce);
            }

            return CxlResponce;

        }
    }
}