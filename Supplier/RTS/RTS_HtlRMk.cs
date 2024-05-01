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
    public class RTS_HtlRMk
    {
        public XDocument GetRemarkRequest(XElement Req,string Guestnationalitycode)
        {
            
            try
            {
                //string sitecode = ConfigurationManager.AppSettings["RTSitecode"].ToString();
                //string Password = ConfigurationManager.AppSettings["RTSPassword"].ToString();
                //string Requetype = ConfigurationManager.AppSettings["RTSReqType"].ToString();                
                //string RTSellerMarkup = ConfigurationManager.AppSettings["RTSellerMarkup"].ToString();
                //string RTSClientCurrencyCode = ConfigurationManager.AppSettings["RTSClientCurrencyCode"].ToString();
                #region Credential
                XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "9");
                string sitecode = suppliercred.Descendants("RTSitecode").FirstOrDefault().Value;
                string Password = suppliercred.Descendants("RTSPassword").FirstOrDefault().Value;
                string Requetype = suppliercred.Descendants("RTSReqType").FirstOrDefault().Value;
                string RTSellerMarkup = suppliercred.Descendants("RTSellerMarkup").FirstOrDefault().Value;
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
                                                                                 new XElement(rts + "SiteCode", sitecode),
                                                                                  new XElement(rts + "Password", Password),
                                                                                   new XElement(rts + "RequestType", Requetype))),
                                                                                   new XElement(soap + "Body",
                                                                                       new XElement(rts + "GetRemarkHotelInformationForCustomerCount",
                                                                                           new XElement(rts + "HotelSearchListNetGuestCount",
                                                                                                new XElement(rts + "LanguageCode", "EN"),
                                                                                                 new XElement(rts + "TravelerNationality", Guestnationalitycode),
                                                                                                 new XElement(rts + "CityCode", string.Empty),
                                                                                                  new XElement(rts + "CheckInDate", JacHelper.MyDate(htl.Element("FromDate").Value)),
                                                                                                  new XElement(rts + "CheckOutDate", JacHelper.MyDate(htl.Element("ToDate").Value)),
                                                                                                 new XElement(rts + "StarRating", htl.Element("MinStarRating").Value),
                                                                                                 new XElement(rts + "LocationCode", string.Empty),
                                                                                                 new XElement(rts + "SupplierCompCode", string.Empty),
                                                                                                 new XElement(rts + "AvailableHotelOnly", true),
                                                                                                 new XElement(rts + "RecommendHotelOnly", false),
                                                                                                  new XElement(rts + "ClientCurrencyCode", RTSClientCurrencyCode),
                                                                                                    new XElement(rts + "ItemName", htl.Element("HotelName").Value),
                                                                                                    new XElement(rts + "SellerMarkup", RTSellerMarkup),
                                                                                                    new XElement(rts + "CompareYn", false),
                                                                                                      new XElement(rts + "SortType", string.Empty),
                                                                                                      new XElement(rts + "ItemCodeList",
                                                                                                          new XElement(rts + "ItemCodeInfo",
                                                                                           //new XElement(rts + "ItemCode", htl.Element("HotelID").Value),
                                                                                            new XElement(rts + "ItemCode", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                                                           new XElement(rts + "ItemNo", htl.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value))),
                                                                                           //new XElement(rts + "RoomTypeCode", htl.Descendants("Room").FirstOrDefault().Attribute("ID").Value),
                                                                                            //new XElement(rts + "GuestList",
                                                                                            //   from pax in htl.Descendants("RoomPax")
                                                                                            //   select new XElement(rts + "GuestsInfo",
                                                                                            //        new XElement(rts + "AdultCount", pax.Element("Adult").Value),
                                                                                            //              new XElement(rts + "ChildCount", pax.Element("Child").Value),
                                                                                            //                  new XElement(rts + "RoomCount", 1),
                                                                                            //                    GetChildAge(pax.Descendants("ChildAge"), rts)))
                                                                                                                new XElement(rts + "GuestList",
                                                                                                             GetRomTag(htl.Descendants("Rooms").FirstOrDefault(), rts))
                                                                                                             
                                                                                                                ),
                                                                                                                new XElement(rts + "RoomTypeCode", htl.Descendants("Room").FirstOrDefault().Attribute("ID").Value)
                                                                                                                )));


                
                //string RTSrhhtlURL = ConfigurationManager.AppSettings["RTSrhhtlURL"].ToString();
                string RTSrhhtlURL = suppliercred.Descendants("RTSrhhtlURL").FirstOrDefault().Value;
                RequestClass obj = new RequestClass();
                XDocument Responce = obj.HttpPostRequest(RTSrhhtlURL, lst.FirstOrDefault().ToString(), Req, "PreBook", 4);
               return Responce;
                //if (Responce != null)
                //{
                //    return BindResponce(Responce, Req);
                //}

            }
            catch (Exception)
            {

              
            }
            return null;
        }
        List<XElement> GetRomTag(XElement rooms, XNamespace rts)
        {
            List<XElement> finalele = new List<XElement>();
            Dictionary<int, int> dic = new Dictionary<int, int>();
            List<XElement> adultlst = rooms.Descendants("RoomPax").Where(x => x.Element("Child").Value == "0").ToList();
            List<XElement> chidlst = rooms.Descendants("RoomPax").Where(x => x.Element("Child").Value != "0").ToList();


            foreach (XElement item in adultlst)
            {
                int val = Convert.ToInt16(item.Element("Adult").Value);
                if (dic.ContainsKey(val))
                {
                    int dicval = dic[val] + 1;
                    dic[val] = dicval;
                }
                else
                {
                    dic.Add(val, 1);
                }
            }

            foreach (var item in dic)
            {
                XElement ele = new XElement(rts + "GuestsInfo",
                                 new XElement(rts + "AdultCount", item.Key),
                                  new XElement(rts + "ChildCount", 0),
                                  new XElement(rts + "RoomCount", item.Value));
                finalele.Add(ele);
            }
            foreach (XElement item in chidlst)
            {
                XElement ele = new XElement(rts + "GuestsInfo",
                    new XElement(rts + "AdultCount", item.Element("Adult").Value),
                          new XElement(rts + "ChildCount", item.Element("Child").Value),
                               new XElement(rts + "RoomCount", 1),
                               GetChildAge(item.Descendants("ChildAge"), rts));
                finalele.Add(ele);
            }
            return finalele;
        }


        List<XElement> GetChildAge(IEnumerable<XElement> chld, XNamespace rts)
        {
            List<XElement> lst = new List<XElement>();
            int count = chld.Count();
            int i = 1;
            foreach (XElement item in chld)
            {
                string str = "ChildAge" + i.ToString();
                lst.Add(new XElement(rts + str, item.Value));
                i++;
            }
            for (int j = lst.Count; j < 2; j++)
            {
                i = j + 1;
                string str = "ChildAge" + i.ToString();
                lst.Add(new XElement(rts + str, 0));
            }


            return lst;
        }
        //List<XElement> GetChildAge(IEnumerable<XElement> ChildAgetag, XNamespace ns)
        //{
        //    int i = 0;
        //    List<XElement> lst = new List<XElement>();

        //    if (ChildAgetag == null || ChildAgetag.Count() == 0)
        //    {
        //        lst.Add(new XElement(ns + "ChildAge1", 0));
        //        lst.Add(new XElement(ns + "ChildAge2", 0));
        //    }

        //    foreach (XElement item in ChildAgetag)
        //    {
        //        if (i == 0)
        //        {
        //            lst.Add(new XElement(ns + "ChildAge1", item.Value));
        //        }
        //        else if (i == 1)
        //        {
        //            lst.Add(new XElement(ns + "ChildAge2", item.Value));
        //        }
        //        i++;
        //    }
        //    return lst;
        //}


    }
}