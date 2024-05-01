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
    public class RTS_VoucherRequest
    {

       public XDocument VoucherRequest(XDocument Req, XElement myele)
        {
            XDocument Responce = null;
            try
            {
                //string sitecode = ConfigurationManager.AppSettings["RTSitecode"].ToString();
                //string Password = ConfigurationManager.AppSettings["RTSPassword"].ToString();
                //string Requetype = ConfigurationManager.AppSettings["RTSReqType"].ToString();
                #region Credential
                XElement suppliercred = supplier_Cred.getsupplier_credentials(myele.Descendants("CustomerID").FirstOrDefault().Value, "9");
                string sitecode = suppliercred.Descendants("RTSitecode").FirstOrDefault().Value;
                string Password = suppliercred.Descendants("RTSPassword").FirstOrDefault().Value;
                string Requetype = suppliercred.Descendants("RTSReqType").FirstOrDefault().Value;
                #endregion

                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace rts = "http://www.rts.co.kr/";
                IEnumerable<XElement> lst = from htl in Req.Descendants(rts+"BookingMaster")
                                            select new XElement(soap + "Envelope",
                                                                            new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                                             new XAttribute(XNamespace.Xmlns + "rts", rts),
                                                                             new XElement(soap + "Header",
                                                                                 new XElement(rts + "BaseInfo",
                                                                                 new XElement(rts + "SiteCode", sitecode),
                                                                                  new XElement(rts + "Password", Password),
                                                                                   new XElement(rts + "RequestType", Requetype))),
                                                                                   new XElement(soap + "Body",
                                                                                       new XElement(rts + "GetBookingVoucher",
                                                                                           new XElement(rts+"BookingVoucher",
                                                                                           new XElement(rts + "BookingCode", htl.Element(rts + "BookingCode").Value)))));


                //string RTSBokCXlURL = ConfigurationManager.AppSettings["RTSBokDtlURL"].ToString();
                string RTSBokCXlURL = suppliercred.Descendants("RTSBokDtlURL").FirstOrDefault().Value;
                RequestClass obj = new RequestClass();
                Responce = obj.HttpPostRequest(RTSBokCXlURL, lst.FirstOrDefault().ToString(), myele, "Voucher", 5);



            }
            catch (Exception)
            {

                return null;
            }
            return Responce;

        }


       
    }
}