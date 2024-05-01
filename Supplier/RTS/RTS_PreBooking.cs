using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.RTS
{
    public class RTS_PreBooking
    {
        string dmc = string.Empty;
        XElement request = null;
        public XElement GetPreBook(XElement req, string GuestCountyCode,string xmlout)
        {
            request = req;
            dmc = xmlout;
                RTS_HtlRMk Rkmobj = new RTS_HtlRMk();
                RTS_PreBookCXl preBookCXL = new RTS_PreBookCXl();
                RTS_PriceChangeChk PriceChk = new RTS_PriceChangeChk();
                XDocument RKMresponce = null;
                XDocument CXLresponce = null;
                XDocument PrChkDoc = null;

                Thread th = new Thread(new ThreadStart(() => RKMresponce = Rkmobj.GetRemarkRequest(req, GuestCountyCode)));
                Thread th1 = new Thread(new ThreadStart(() => CXLresponce = preBookCXL.GetCancelPolicy(req, GuestCountyCode)));
                Thread th2 = new Thread(new ThreadStart(() => PrChkDoc = PriceChk.PriceChkReq(req)));
                th.Start();
                th1.Start();
                th2.Start();
                th.Join();
                th1.Join();
                th2.Join();

                th.Abort();
                th1.Abort();
                th2.Abort();

                return BindResponce(RKMresponce, req, CXLresponce, PrChkDoc);
            }
           
        


        List<XElement> GetChildAge(IEnumerable<XElement> ChildAgetag, XNamespace ns)
        {
            int i = 0;
            List<XElement> lst = new List<XElement>();

            if (ChildAgetag == null || ChildAgetag.Count() == 0)
            {
                lst.Add(new XElement(ns + "ChildAge1", 0));
                lst.Add(new XElement(ns + "ChildAge2", 0));
            }

            foreach (XElement item in ChildAgetag)
            {
                if (i == 0)
                {
                    lst.Add(new XElement(ns + "ChildAge1", item.Value));
                }
                else if (i == 1)
                {
                    lst.Add(new XElement(ns + "ChildAge2", item.Value));
                }
                i++;
            }
            return lst;
        }

        XElement BindResponce(XDocument Doc, XElement Req,XDocument CXLDOC,XDocument PriceCheck)
        {
            int duration = 0;
            foreach (var htl in Req.Descendants("HotelPreBookingRequest"))
            {
                JacHelper.GetDuration(htl.Element("ToDate").Value, htl.Element("FromDate").Value, out duration);
            }
            XNamespace ns = "http://www.rts.co.kr/";
            IEnumerable<XElement> CountryList = from htl in Req.Descendants("HotelPreBookingRequest")
                                                select new XElement("HotelPreBookingResponse",
                                                     new XElement("NewPrice", ChangePriceBind(PriceCheck, ns, htl.Element("TotalRoomRate").Value)),
                                                     new XElement("Hotels",
                                                      new XElement("Hotel",
                                                       new XElement("HotelID", htl.Element("HotelID").Value),
                                                        new XElement("HotelName", htl.Element("HotelName").Value),
                                                         new XElement("Status", true),
                                                          new XElement("TermCondition", "<ul>" + BindRemarks(Doc, ns) + "</ul>"),
                                                           new XElement("HotelImgLarge", string.Empty),
                                                           new XElement("HotelImgSmall", string.Empty),
                                                            new XElement("MapLink", string.Empty),
                                                             new XElement("DMC", dmc),
                                                             new XElement("Currency", htl.Element("CurrencyName").Value),
                                                             new XElement("Offers", ""),
                                                             new XElement("Rooms",
                                                               new XElement("RoomTypes",
                                                                  new XAttribute("Index", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("Index").Value),
                                                                    new XAttribute("TotalRate", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value),
                                                                    CreateRoomTag(htl.Element("Rooms"), CXLDOC, duration, ns, htl.Element("CurrencyName").Value, htl.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value),
                                                                    new XElement("CancellationPolicies", BindCXlPolicy(CXLDOC.Descendants(ns + "GetCancelDeadlineResult").FirstOrDefault(), htl.Element("CurrencyName").Value, ns, htl.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value, duration))
                                                                    )))));
            IEnumerable<XElement> Descoll = Req.Descendants("HotelPreBookingRequest");
            foreach (XElement item in Descoll)
            {
                item.AddAfterSelf(CountryList.FirstOrDefault());
            }
            return Req;
        }
        List<XElement> CreateRoomTag(XElement rmlst, XDocument CXLDoc, int duration, XNamespace ns,string CurrName,string Totalrate)
        {
           
            List<XElement> Noofpax = rmlst.Elements("RoomPax").ToList();
            List<XElement> NoofRom = rmlst.Element("RoomTypes").Elements("Room").ToList();
            List<XElement> rooms = new List<XElement>();
            decimal RomPrice = Convert.ToDecimal(Totalrate) / Noofpax.Count();
            decimal PerN8Price = RomPrice / duration;
           
            for (int i = 0; i < NoofRom.Count; i++)
            {

                XElement roomdeatil = new XElement("Room",
                                                   new XAttribute("ID", NoofRom[i].Attribute("ID").Value),
                                                   new XAttribute("SuppliersID", NoofRom[i].Attribute("SuppliersID").Value),
                                                   new XAttribute("RoomSeq", i+1),
                                                   new XAttribute("SessionID", NoofRom[i].Attribute("SessionID").Value),
                                                   new XAttribute("MealPlanID", NoofRom[i].Attribute("MealPlanID").Value),                                                 
                                                   new XAttribute("MealPlanName", NoofRom[i].Attribute("MealPlanName").Value),
                                                   new XAttribute("TotalRoomRate", NoofRom[i].Attribute("TotalRoomRate").Value),
                                                    new XAttribute("MealPlanCode", ""),
                                                    new XAttribute("RoomType", NoofRom[i].Attribute("RoomType").Value),
                                                   new XAttribute("MealPlanPrice", ""),
                                                   new XAttribute("OccupancyName", ""),
                                                     new XAttribute("OccupancyID", ""),
                                                      new XAttribute("PerNightRoomRate", NoofRom[i].Attribute("PerNightRoomRate").Value),
                                                         new XAttribute("CancellationDate", ""),
                                                           new XAttribute("CancellationAmount", ""),
                                                           new XAttribute("isAvailable", true),
                                                   new XElement("RequestID", string.Empty),
                                                   new XElement("Offers", ""),
                                                    new XElement("PromotionList", new XElement("Promotions", string.Empty)),
                                                    new XElement("CancellationPolicy", ""),
                                                   new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                    new XElement("Supplements", ""),
                                                   new XElement("Amenities", ""),
                                                   new XElement("PriceBreakups", JacHelper.PriceBreakup(PerN8Price, duration)),
                                                   new XElement("AdultNum", Noofpax[i].Element("Adult").Value),
                                                   new XElement("ChildNum", Noofpax[i].Element("Child").Value)
                                                   //new XElement("CancellationPolicies", BindCXlPolicy(CXLDoc.Descendants(ns + "GetCancelDeadlineResult").FirstOrDefault(), CurrName, ns, Totalrate, duration)));
                                                   );
                rooms.Add(roomdeatil);

            }




            return rooms;
        }


        public List<XElement> PriceBreakup(int PerN8Price, int totalN8)
        {
            List<XElement> Breakup = new List<XElement>();
            for (int i = 0; i < totalN8; i++)
            {
                XElement ele = new XElement("Price",
                    new XAttribute("Night", i + 1),
                    new XAttribute("PriceValue", PerN8Price));
                Breakup.Add(ele);
            }
            return Breakup;
        }

        public bool CancelPolicyChk = true;
        string BindRemarks(XDocument Responce, XNamespace ns)
        {
            string str = string.Empty;
            if (Responce!=null)
            {
                foreach (XElement item in Responce.Descendants(ns + "RemarkHotelInformation"))
                {
                    str =   item.Element(ns + "Remarks").Value + str;
                }
            }
           



            return str;

        }



        IEnumerable<XElement> BindCXlPolicy(XElement CxlColl, string CurrName, XNamespace ns, string TotalRate, int duration)
        {
            //string RTSClientCurrencyCode = ConfigurationManager.AppSettings["RTSClientCurrencyCode"].ToString();
            XElement suppliercred = supplier_Cred.getsupplier_credentials(request.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string RTSClientCurrencyCode = suppliercred.Descendants("RTSClientCurrencyCode").FirstOrDefault().Value;
            if (CancelPolicyChk==true)
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


        double ChangePriceBind(XDocument doc, XNamespace ns, string TotalRoomRate)
        {
            double rate = Convert.ToDouble(TotalRoomRate);
            foreach (XElement item in doc.Descendants(ns + "PriceChangeResult"))
            {
                double ChgPrice = Convert.ToDouble(item.Element(ns+"ReSearchPrice").Value);
                if (rate>ChgPrice)
                {
                    return ChgPrice;
                }
            }
            return rate;
        }

    }
}