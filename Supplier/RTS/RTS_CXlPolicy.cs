using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.RTS
{
    public class RTS_CXlPolicy
    {

        XElement request = null;
        public XElement GetCancelPolicy(XElement Req,string GuestCountyCode)
        {
            request = Req;
            XDocument doc = CXLPolicyRequest(Req, GuestCountyCode);          
            XElement ele = CXLResponce(doc, Req);
            return ele;
        }

        XDocument CXLPolicyRequest(XElement Req, string GuestCountyCode)
        {            
            XDocument Responce = null;
            try
            {
                //string sitecode = ConfigurationManager.AppSettings["RTSitecode"].ToString();
                //string Password = ConfigurationManager.AppSettings["RTSPassword"].ToString();
                //string Requetype = ConfigurationManager.AppSettings["RTSReqType"].ToString();
                //string RTSDefaultTvlNationality = ConfigurationManager.AppSettings["RTSDefaultTvlNationality"].ToString();
                #region Credential
                XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "9");
                string sitecode = suppliercred.Descendants("RTSitecode").FirstOrDefault().Value;
                string Password = suppliercred.Descendants("RTSPassword").FirstOrDefault().Value;
                string Requetype = suppliercred.Descendants("RTSReqType").FirstOrDefault().Value;
                string RTSDefaultTvlNationality = suppliercred.Descendants("RTSDefaultTvlNationality").FirstOrDefault().Value;
                #endregion
                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace rts = "http://www.rts.co.kr/";
                IEnumerable<XElement> lst = from htl in Req.Descendants("hotelcancelpolicyrequest")
                                            select new XElement(soap + "Envelope",
                                                                            new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                                             new XAttribute(XNamespace.Xmlns + "rts", rts),
                                                                             new XElement(soap + "Header",
                                                                                 new XElement(rts + "BaseInfo",
                                                                                 new XElement(rts + "SiteCode", sitecode),
                                                                                  new XElement(rts + "Password", Password),
                                                                                   new XElement(rts + "RequestType", Requetype))),
                                                                                   new XElement(soap + "Body",
                                                                                       new XElement(rts + "GetCancelDeadlineForCustomerCount",
                                                                                           new XElement(rts + "GetCancelDeadline",
                                                                                           //new XElement(rts + "ItemCode", htl.Element("HotelID").Value),
                                                                                           new XElement(rts + "ItemCode", Req.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                                                           new XElement(rts + "ItemNo", htl.Descendants("Room").FirstOrDefault().Attribute("SessionID").Value),
                                                                                           new XElement(rts + "RoomTypeCode", htl.Descendants("Room").FirstOrDefault().Attribute("ID").Value),
                                                                                           new XElement(rts + "CheckInDate", JacHelper.MyDate(htl.Element("FromDate").Value)),
                                                                                           new XElement(rts + "CheckOutDate", JacHelper.MyDate(htl.Element("ToDate").Value)),
                                                                                            //new XElement(rts + "GuestList",
                                                                                            //   from pax in htl.Descendants("RoomPax")
                                                                                            //   select new XElement(rts + "GuestsInfo",
                                                                                            //        new XElement(rts + "AdultCount", pax.Element("Adult").Value),
                                                                                            //              new XElement(rts + "ChildCount", pax.Element("Child").Value),
                                                                                            //                  new XElement(rts + "RoomCount", 1),
                                                                                            //                    GetChildAge(pax.Descendants("ChildAge"),rts))),
                                                                                             new XElement(rts + "GuestList",
                                                                                                             GetRomTag(htl.Descendants("Rooms").FirstOrDefault(), rts)),
                                                                                                                new XElement(rts +"LanguageCode","EN"),
                                                                                                                new XElement(rts + "TravelerNationality", GuestCountyCode)))));

            
                //string RTSBokCXlURL = ConfigurationManager.AppSettings["RTSBokCXlURL"].ToString();
                string RTSBokCXlURL = suppliercred.Descendants("RTSBokCXlURL").FirstOrDefault().Value;
                RequestClass obj = new RequestClass();
                Responce = obj.HttpPostRequest(RTSBokCXlURL, lst.FirstOrDefault().ToString(), Req, "CXlPolicy", 3);



            }
            catch (Exception)
            {

                throw;
            }
            return Responce;

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
        //List<XElement> GetChildAge(IEnumerable<XElement> chld,XNamespace rts)
        //{
        //    List<XElement> lst = new List<XElement>();
        //    int count = chld.Count();
        //    int i = 1;
        //    foreach (XElement item in chld)
        //    {
        //        string str = "ChildAge" + i.ToString();
        //        lst.Add(new XElement(rts+str, item.Value));
        //        i++;
        //    }
        //    for (int j = lst.Count; j < 2; j++)
        //    {
        //        i = j + 1;
        //        string str = "ChildAge" + i.ToString();
        //        lst.Add(new XElement(rts+str, 0));
        //    }


        //    return lst;
        //}



        #region BindCXLPolicyResponce
        XElement CXLResponce(XDocument Responce, XElement myEle)
        {
            int duration = 0;
            foreach (var htl in myEle.Descendants("hotelcancelpolicyrequest"))
            {
                JacHelper.GetDuration(htl.Element("ToDate").Value, htl.Element("FromDate").Value, out duration);
            }


            XNamespace ns = "http://www.rts.co.kr/";
            IEnumerable<XElement> CountryList = from htl in myEle.Descendants("hotelcancelpolicyrequest")
                                                select new XElement("HotelDetailwithcancellationResponse",
                                                     new XElement("Hotels",
                                                      new XElement("Hotel",
                                                       new XElement("HotelID", htl.Element("HotelID").Value),
                                                        new XElement("HotelName", htl.Element("HotelName").Value),
                                                         new XElement("Status", true),
                                                          new XElement("TermCondition", ""),
                                                           new XElement("HotelImgLarge", string.Empty),
                                                           new XElement("HotelImgSmall", string.Empty),
                                                            new XElement("MapLink", ""),
                                                             new XElement("DMC", "RTS"),
                                                             new XElement("Currency", htl.Element("CurrencyName").Value),
                                                             new XElement("Offers", ""),
                                                             new XElement("Rooms",
                                                               new XElement("RoomTypes",
                                                                  new XAttribute("Index", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("Index").Value),
                                                                    new XAttribute("TotalRate", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value), 
                                                             //new XAttribute("HtlCode", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                             // new XAttribute("CrncyCode", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("CrncyCode").Value),
                                                                    CreateRoomTag(htl.Element("Rooms"), duration, htl.Element("CurrencyName").Value, Responce.Descendants(ns + "GetCancelDeadlineResult").FirstOrDefault(), ns, htl.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value))))));




            IEnumerable<XElement> Descoll = myEle.Descendants("hotelcancelpolicyrequest");
            foreach (XElement item in Descoll)
            {
                item.AddAfterSelf(CountryList);
            }



            return myEle;
        }


        // Add Room for client PreBooking Response
        List<XElement> CreateRoomTag(XElement rmlst, int duration, string Currname, XElement cxl, XNamespace ns, string TotalRate)
        {
            List<XElement> Noofpax = rmlst.Elements("RoomPax").ToList();
            List<XElement> NoofRom = rmlst.Element("RoomTypes").Elements("Room").ToList();
            decimal RomPrice = Convert.ToDecimal(TotalRate) / Noofpax.Count();
            decimal PerN8Price = RomPrice / duration;
            List<XElement> rooms = new List<XElement>();
            
            for (int i = 0; i < NoofRom.Count; i++)
            {

                XElement roomdeatil = new XElement("Room",
                                                   new XAttribute("ID", NoofRom[i].Attribute("ID").Value),
                                                   new XAttribute("SuppliersID", NoofRom[i].Attribute("SuppliersID").Value),
                                                   new XAttribute("SessionID", NoofRom[i].Attribute("SessionID").Value),                                                   
                                                   new XAttribute("MealPlanID", NoofRom[i].Attribute("MealPlanID").Value),
                                                   new XAttribute("MealPlanName", NoofRom[i].Attribute("MealPlanName").Value),
                                                   new XAttribute("TotalRoomRate", NoofRom[i].Attribute("TotalRoomRate").Value),
                                                    new XAttribute("MealPlanCode", ""),
                                                    new XAttribute("RoomType", NoofRom[i].Attribute("RoomType").Value != null ? NoofRom[i].Attribute("TotalRoomRate").Value : string.Empty),
                                                   new XAttribute("MealPlanPrice", ""),
                                                   new XAttribute("OccupancyName", ""),
                                                     new XAttribute("OccupancyID", ""),
                                                      new XAttribute("PerNightRoomRate", NoofRom[i].Attribute("PerNightRoomRate").Value),
                                                         new XAttribute("CancellationDate", ""),
                                                           new XAttribute("CancellationAmount", ""),
                                                           new XAttribute("isavailable", ""),
                                                   new XElement("RequestID", string.Empty),
                                                   new XElement("Offers", ""),
                                                    new XElement("PromotionList", new XElement("Promotions", string.Empty)),
                                                   new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                    new XElement("Supplements", ""),
                                                   new XElement("Amenities", ""),
                                                   new XElement("PriceBreakups", JacHelper.PriceBreakup(PerN8Price, duration)),
                                                   new XElement("AdultNum", Noofpax[i].Element("Adult").Value),
                                                   new XElement("ChildNum", Noofpax[i].Element("Child").Value),
                                                   new XElement("CancellationPolicies", BindCXlPolicy(cxl, Currname, ns, rmlst.Element("RoomTypes").Attribute("TotalRate").Value, duration)));
                rooms.Add(roomdeatil);

            }




            return rooms;
        }




       


        public bool CancelPolicyChk = true;

        IEnumerable<XElement> BindCXlPolicy(XElement CxlColl, string CurrName, XNamespace ns, string TotalRate, int duration)
        {
            //string RTSClientCurrencyCode = ConfigurationManager.AppSettings["RTSClientCurrencyCode"].ToString();
            XElement suppliercred = supplier_Cred.getsupplier_credentials(request.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string RTSClientCurrencyCode = suppliercred.Descendants("RTSClientCurrencyCode").FirstOrDefault().Value;
            if (CancelPolicyChk == true)
            {
                XElement ele = new XElement("CancelPolicy", string.Empty);
                string type = CxlColl.Element(ns + "TypeCode").Value;

                if (type == "Penalty")
                {

                    DateTime dt = Convert.ToDateTime(CxlColl.Element(ns + "CancelDeadlineDate").Value).AddDays(-1);

                    XElement Newele = new XElement("Cancellation", new XElement("Date", dt.Date.ToString("yyyy-MM-dd")),
                                                   new XElement("Penalty", 0.00));
                    ele.Add(Newele);
                    DateTime NextDate = dt.AddDays(1);
                    XElement ele1 = new XElement("Cancellation", new XElement("Date", NextDate.Date.ToString("yyyy-MM-dd")),
                                 new XElement("Penalty", TotalRate));
                    if (ele1 != null)
                    {
                        ele.Add(ele1);
                    }


                }
                else if (type == "Normal")
                {
                    decimal rate = Convert.ToDecimal(TotalRate);
                    decimal singlentprice = rate / duration;
                    DateTime dt = Convert.ToDateTime(CxlColl.Element(ns + "CancelDeadlineDate").Value).AddDays(-1);
                    // DateTime date = DateTime.ParseExact(JacHelper.DtFormat(item.Element("StartDate").Value.Replace("T00:00:00", "")), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    XElement Newele = new XElement("Cancellation", new XElement("Date", dt.Date.ToString("yyyy-MM-dd")),
                                                   new XElement("Penalty", 0.00));
                    ele.Add(Newele);
                    DateTime NextDate = dt.AddDays(1);
                    XElement ele1 = new XElement("Cancellation", new XElement("Date", NextDate.Date.ToString("yyyy-MM-dd")),
                                 new XElement("Penalty", singlentprice));
                    if (ele1 != null)
                    {
                        ele.Add(ele1);
                    }


                }
                else if (type == "Other")
                {
                    decimal rate = Convert.ToDecimal(TotalRate);

                    DateTime dt = Convert.ToDateTime(CxlColl.Element(ns + "CancelDeadlineDate").Value).AddDays(-1);
                    // DateTime date = DateTime.ParseExact(JacHelper.DtFormat(item.Element("StartDate").Value.Replace("T00:00:00", "")), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);

                    XElement Newele = new XElement("Cancellation", new XElement("Date", dt.Date.ToString("yyyy-MM-dd")),
                                                   new XElement("Penalty", 0.00));
                    ele.Add(Newele);
                    DateTime NextDate = dt.AddDays(1);
                    XElement ele1 = new XElement("Cancellation", new XElement("Date", NextDate.Date.ToString("yyyy-MM-dd")),
                                 new XElement("Penalty", rate));
                    if (ele1 != null)
                    {
                        ele.Add(ele1);
                    }


                }






                var data = from cxl in ele.Descendants("Cancellation")
                           select new XElement("CancellationPolicy",
                                  new XAttribute("LastCancellationDate", JacHelper.DtFormat(cxl.Element("Date").Value.Replace("T00:00:00", ""))),
                                      new XAttribute("ApplicableAmount", cxl.Element("Penalty").Value),
                                      new XAttribute("NoShowPolicy", "0"),
                                      "Cancellation done on after " + (Convert.ToDecimal(cxl.Element("Penalty").Value) == 0 ? JacHelper.DtFormat(cxl.Element("Date").Value.Replace("T00:00:00", "")) : JacHelper.DtFormat(cxl.Element("Date").Value.Replace("T00:00:00", ""))) + " will apply " + RTSClientCurrencyCode + " " + cxl.Element("Penalty").Value + " Cancellation fee");


                CancelPolicyChk = false;
                return data;
            }
            return null;

        }
        #endregion


    }
}
