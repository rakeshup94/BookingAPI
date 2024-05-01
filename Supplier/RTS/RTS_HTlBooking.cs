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
    public class RTS_HTlBooking
    {
        XElement request = null;
        #region HTLBookingRequest
        public XElement HtlBooking(XElement req, string GuestCountyCode)
        {
            //string sitecode = ConfigurationManager.AppSettings["RTSitecode"].ToString();
            //string Password = ConfigurationManager.AppSettings["RTSPassword"].ToString();
            //string Requetype = ConfigurationManager.AppSettings["RTSReqType"].ToString();
            //string RTSalesUserNo = ConfigurationManager.AppSettings["RTSalesUserNo"].ToString();
            //string RTSalesSiteCode = ConfigurationManager.AppSettings["RTSalesSiteCode"].ToString();
            //string RTSSalesCompCode = ConfigurationManager.AppSettings["RTSSalesCompCode"].ToString();
            //string RTSAdminCompCode = ConfigurationManager.AppSettings["RTSAdminCompCode"].ToString();
            //string RTSBookingPathCode = ConfigurationManager.AppSettings["RTSBookingPathCode"].ToString();
            
            //string RTSellerMarkup = ConfigurationManager.AppSettings["RTSellerMarkup"].ToString();

            #region Credential
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string sitecode = suppliercred.Descendants("RTSitecode").FirstOrDefault().Value;
            string Password = suppliercred.Descendants("RTSPassword").FirstOrDefault().Value;
            string Requetype = suppliercred.Descendants("RTSReqType").FirstOrDefault().Value;
            string RTSalesUserNo = suppliercred.Descendants("RTSalesUserNo").FirstOrDefault().Value;
            string RTSalesSiteCode = suppliercred.Descendants("RTSalesSiteCode").FirstOrDefault().Value;
            string RTSSalesCompCode = suppliercred.Descendants("RTSSalesCompCode").FirstOrDefault().Value;
            string RTSAdminCompCode = suppliercred.Descendants("RTSAdminCompCode").FirstOrDefault().Value;
            string RTSBookingPathCode = suppliercred.Descendants("RTSBookingPathCode").FirstOrDefault().Value;

            string RTSellerMarkup = suppliercred.Descendants("RTSellerMarkup").FirstOrDefault().Value;

            #endregion
            request = req;
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace rts = "http://www.rts.co.kr/";
            IEnumerable<XElement> lst = from htl in req.Descendants("HotelBookingRequest")
                                        select new XElement(soap + "Envelope",
                                                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                                         new XAttribute(XNamespace.Xmlns + "rts", rts),
                                                                         new XElement(soap + "Header",
                                                                             new XElement(rts + "BaseInfo",
                                                                             new XElement(rts + "SiteCode", sitecode),
                                                                              new XElement(rts + "Password", Password),
                                                                               new XElement(rts + "RequestType", Requetype))),
                                                                               new XElement(soap + "Body",
                                                                                   new XElement(rts + "CreateSystemBookingForGuestCount",
                                                                                       new XElement(rts + "SystemBookingInfoNetForGuestCount",
                                                                                       new XElement(rts + "LanguageCode", "EN"),
                                                                                       new XElement(rts + "ClientCurrencyCode", htl.Element("CurrencyCode").Value),
                                                                                       new XElement(rts + "BookingCode", string.Empty),
                                                                                       new XElement(rts + "AdminCompCode", RTSAdminCompCode),
                                                                                        new XElement(rts + "GroupOrFit", "F"),
                                                                                        new XElement(rts + "NationalityCode", "EU"),
                                                                                        new XElement(rts + "TravelerNationality", GuestCountyCode),
                                                                                        new XElement(rts + "SalesCompCode", RTSSalesCompCode),
                                                                                        new XElement(rts + "SalesSiteCode", RTSalesSiteCode),
                                                                                        new XElement(rts + "SalesUserNo", RTSalesUserNo),
                                                                                        new XElement(rts + "SalesUserId", string.Empty),
                                                                                        new XElement(rts + "SalesUserName", string.Empty),
                                                                                        new XElement(rts + "SalesUserGender", string.Empty),
                                                                                        new XElement(rts + "SalesUserBirthday", string.Empty),
                                                                                        new XElement(rts + "SalesUserHandPhone", string.Empty),
                                                                                        new XElement(rts + "SalesUserCompPhone", string.Empty),
                                                                                        new XElement(rts + "SalesUserHomePhone", string.Empty),
                                                                                        new XElement(rts + "SalesUserEamil", string.Empty),
                                                                                        new XElement(rts + "SalesEmpNo", RTSalesUserNo),
                                                                                        new XElement(rts + "SalesEmpName", string.Empty),
                                                                                        new XElement(rts + "SalesPayStatusCode", string.Empty),
                                                                                        new XElement(rts + "NormalRemarks", string.Empty),
                                                                                        new XElement(rts + "SalesRemarks", string.Empty),
                                                                                        new XElement(rts + "AdminRemarks", string.Empty),
                                                                                        new XElement(rts + "AdminBlockYn", false),
                                                                                        new XElement(rts + "BookingPathCode", RTSBookingPathCode),
                                                                                        new XElement(rts + "CardPaymentYn", false),
                                                                                        new XElement(rts + "CardPaymentAmount", string.Empty),
                                                                                        new XElement(rts + "LastWriterUno", RTSalesUserNo),
                                                                                         new XElement(rts + "CustomerList", CustomerLst(htl.Descendants("Room"), rts)),
                                                                                         new XElement(rts + "BookingHotelList", BokHTlst(htl, rts)),
                                                                                         new XElement(rts + "SellerMarkup", RTSellerMarkup)))));

           
            string RTSBokCXlURL = suppliercred.Descendants("RTSBokCXlURL").FirstOrDefault().Value;
            RequestClass obj = new RequestClass();
            XDocument ele = obj.HttpPostRequest(RTSBokCXlURL, lst.FirstOrDefault().ToString(), req, "Book", 5);            
            IEnumerable<XElement> res = ele.Descendants(rts + "BookingItemInfo");
            foreach (XElement item in res)
            {
                if (item.Element(rts + "ItemStatusCode").Value == "BS02")
                {
                    XDocument Voucher = GetVoucherDoc(ele, req);
                    return BokngResponce(ele, req, Voucher);
                }
                else if (item.Element(rts + "ItemStatusCode").Value == "BS07" || item.Element(rts + "ItemStatusCode").Value == "BS08" || item.Element(rts + "ItemStatusCode").Value == "BS06")
                {

                    IEnumerable<XElement> Descoll = req.Descendants("HotelBookingRequest");
                    foreach (XElement item1 in Descoll)
                    {
                        XElement error = ele.Descendants(rts + "Error").FirstOrDefault();
                        XElement Bokngxml = new XElement("HotelBookingResponse",
                            new XElement("ErrorTxt", error.Element(rts + "Message").Value));
                        item1.AddAfterSelf(Bokngxml);
                        return req;
                    }
                }
                else if (item.Element(rts + "ItemStatusCode").Value == "BS01")
                {
                    //item.Element(rts + "BookingCode").Value
                    XElement bkngcoderes = ele.Descendants(rts + "BookingResult").FirstOrDefault();
                    string bookingcode = bkngcoderes.Descendants(rts + "BookingCode").FirstOrDefault().Value;
                    PendingBokCXL(sitecode, Password, Requetype, RTSalesUserNo, bookingcode, RTSBokCXlURL);
                    IEnumerable<XElement> Descoll = req.Descendants("HotelBookingRequest");
                    foreach (XElement item1 in Descoll)
                    {
                        XElement error = ele.Descendants(rts + "Error").FirstOrDefault();
                        XElement Bokngxml = new XElement("HotelBookingResponse",
                            new XElement("ErrorTxt", "Please Search Again"));
                        item1.AddAfterSelf(Bokngxml);
                        return req;
                    }                    
                }
            }
            return null;
        }
        IEnumerable<XElement> CustomerLst(IEnumerable<XElement> req, XNamespace rts)
        {
            List<XElement> Cuslst = new List<XElement>();
            int i = 1;
            foreach (XElement htl in req.Descendants("PaxInfo"))
            {
                string Gender = "M";
                if (htl.Element("Title").Value == "Mrs" || htl.Element("Title").Value == "Miss" || htl.Element("Title").Value == "Sheikha")
                {
                    Gender = "F";
                }
                XElement ele = new XElement(rts + "CustomerInfo",
                                                      new XElement(rts + "No", i),
                                                      new XElement(rts + "Name", htl.Element("FirstName").Value + " " + htl.Element("LastName").Value),
                                                      new XElement(rts + "LastName", htl.Element("LastName").Value),
                                                      new XElement(rts + "FirstName", htl.Element("FirstName").Value),
                                                      new XElement(rts + "Gender", Gender),
                                                      new XElement(rts + "Age", Convert.ToInt32(htl.Element("Age").Value) < 13 ? htl.Element("Age").Value : string.Empty),
                                                      new XElement(rts + "Country", string.Empty),
                                                      new XElement(rts + "Birthday", string.Empty),
                                                      new XElement(rts + "JuminNo", string.Empty),
                                                       new XElement(rts + "LeadYn", true),
                                                        new XElement(rts + "PassportNo", string.Empty),
                                                         new XElement(rts + "PassportExpiry", string.Empty));
                Cuslst.Add(ele);
                i++;
            }

            return Cuslst;

        }


        XElement BokHTlst(XElement req, XNamespace rts)
        {
            //string RTSBookingPathCode = ConfigurationManager.AppSettings["RTSBookingPathCode"].ToString();
            //string RTSBookerTypeCode = ConfigurationManager.AppSettings["RTSBookerTypeCode"].ToString();

            #region Credential
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string RTSBookingPathCode = suppliercred.Descendants("RTSBookingPathCode").FirstOrDefault().Value;
            string RTSBookerTypeCode = suppliercred.Descendants("RTSBookerTypeCode").FirstOrDefault().Value;
            #endregion

            foreach (XElement item in req.Descendants("PassengersDetail"))
            {
                foreach (XElement item1 in item.Descendants("Room"))
                {
                    XElement Cuslst = new XElement(rts + "BookingHotelInfo",
                                              new XElement(rts + "ItemCode", req.Element("HotelID").Value),
                                              new XElement(rts + "ItemNo", item1.Attribute("SessionID").Value),
                                              new XElement(rts + "AgentBookingReference", string.Empty),
                                              new XElement(rts + "BookerTypeCode", RTSBookerTypeCode),
                                              new XElement(rts + "BookingPathCode", RTSBookingPathCode),
                                              new XElement(rts + "AppliedFromDate", JacHelper.MyDate(req.Element("FromDate").Value)),
                                              new XElement(rts + "AppliedToDate", JacHelper.MyDate(req.Element("ToDate").Value)),
                                              new XElement(rts + "RoomTypeCode", item1.Attribute("RoomTypeID").Value),
                                              new XElement(rts + "FreeBreakfastTypeName", item1.Attribute("MealPlanID").Value),
                                              new XElement(rts + "AddBreakfastTypeName", string.Empty),
                                               new XElement(rts + "VatSheetYn", false),
                                               new XElement(rts + "GuestCountAndGuestList", GuestCount(req, rts)));
                    return Cuslst;

                }



            }
            return null;
        }

        int guestinfotagcount = 1;
        IEnumerable<XElement> GuestCount(XElement req, XNamespace rts)
        {
            List<XElement> Cuslst = new List<XElement>();
            int i = 1;

            foreach (XElement htl in req.Descendants("RoomPax"))
            {
                int totalpax = Convert.ToInt16(htl.Element("Adult").Value) + Convert.ToInt16(htl.Element("Child").Value);
                XElement ele = new XElement(rts + "GuestCountAndGuestInfo",
                                               new XElement(rts + "RoomNo", i),
                                               new XElement(rts + "AdultCount", htl.Element("Adult").Value),
                                               new XElement(rts + "ChildCount", htl.Element("Child").Value),
                                               new XElement(rts + "GuestList", GuestLst(totalpax, rts)));
                Cuslst.Add(ele);
                i++;
            }

            return Cuslst;

        }
        IEnumerable<XElement> GuestLst(int totalpax, XNamespace rts)
        {
            List<XElement> Cuslst = new List<XElement>();

            for (int i = 0; i < totalpax; i++)
            {
                XElement ele = new XElement(rts + "GuestInfo",
                                               new XElement(rts + "GuestNo", guestinfotagcount),
                                               new XElement(rts + "AgeTypeCode", string.Empty),
                                               new XElement(rts + "ProductId", string.Empty));
                guestinfotagcount++;
                Cuslst.Add(ele);

            }


            return Cuslst;





        }
        #endregion


        #region HTLBookingResponse





        public XElement BokngResponce(XDocument Responce, XElement myEle, XDocument Voucher)
        {



            XNamespace ns = "http://www.rts.co.kr/";
            string DisComm = string.Empty;
            string BookingRef = string.Empty;

            foreach (var item in Responce.Descendants(ns + "BookingItemInfo"))
            {
                if (!string.IsNullOrEmpty(item.Element(ns + "VoucherRemarks").Value))
                {
                    DisComm = item.Element(ns + "VoucherRemarks").Value;
                }
            }


            foreach (var item in Voucher.Descendants(ns + "BookingVoucherInfo"))
            {
                
                for (int i = 1; i <= 5; i++)
                {
                    string val = "DisplayComment" + i.ToString();
                    if (!string.IsNullOrEmpty(item.Element(ns + val).Value))
                    {
                        DisComm = DisComm + " " + item.Element(ns + val).Value;
                    }
                }
                if (!string.IsNullOrEmpty(item.Element(ns + "SpecialRemarks").Value))
                {
                    DisComm = DisComm + " " + item.Element(ns + "SpecialRemarks").Value;
                }

                BookingRef = item.Element(ns + "BookingReference").Value;
            }
            if (!string.IsNullOrEmpty(DisComm))
            {
                DisComm = "<ul>" + DisComm.Replace("Booking reserved and payable by RTS", " ").Replace("RTS","") + "</ul>";
            }


            XElement paxdet = myEle.Descendants("PassengersDetail").FirstOrDefault();

            var Bokngxml = from x in myEle.Descendants("HotelBookingRequest")
                           select new XElement("HotelBookingResponse",
                              new XElement("Hotels",
                              new XElement("HotelID", x.Element("HotelID").Value),
                               new XElement("HotelName", x.Element("HotelName").Value),
                              new XElement("FromDate", x.Element("FromDate").Value),
                              new XElement("ToDate", x.Element("ToDate").Value),
                              new XElement("AdultPax", GetNoofPax(x.Element("Rooms"), "Adult")),
                              new XElement("ChildPax", GetNoofPax(x.Element("Rooms"), "Child")),
                              new XElement("TotalPrice", x.Element("TotalAmount").Value),
                              new XElement("CurrencyID", x.Element("CurrencyID").Value),
                             new XElement("CurrencyCode", x.Element("CurrencyCode").Value),
                              new XElement("MarketID", ""),
                              new XElement("MarketName", ""),
                             new XElement("HotelImgSmall", ""),
                              new XElement("HotelImgLarge", ""),
                              new XElement("MapLink", ""),
                              new XElement("VoucherRemark", DisComm),
                               new XElement("TransID", myEle.Element("TransID") == null ? string.Empty : myEle.Element("TransID").Value),
                              new XElement("ConfirmationNumber", BookingRef),
                              new XElement("Status", Responce.Element(ns + "BookingCode") == null ? "Success" : string.Empty),
                              new XElement("PassengersDetail",
                                  new XElement("GuestDetails", getRomResponce(x.Element("PassengersDetail"), Responce.Descendants(ns + "BookingMaster").FirstOrDefault(), ns, x.Element("TotalAmount").Value)))));


            IEnumerable<XElement> Descoll = myEle.Descendants("HotelBookingRequest");
            foreach (XElement item in Descoll)
            {
                item.AddAfterSelf(Bokngxml);
                break;
            }

            return myEle;

        }


        string GetNoofPax(XElement rooms, string type)
        {
            int count = 0;
            List<XElement> roompax = rooms.Elements("RoomPax").ToList();
            foreach (var item in roompax)
            {
                int paxcount = Int32.Parse(item.Element(type).Value);
                count = paxcount + count;

            }

            return count.ToString();
        }

        IEnumerable<XElement> getRomResponce(XElement Paxdetail, XElement myEle, XNamespace ns, string Totalamout)
        {
            decimal totalrate = Convert.ToDecimal(Totalamout);
            int count = Paxdetail.Descendants("Room").Count();
            decimal Price = totalrate / count;
            var PassengersDetail = from x in Paxdetail.Descendants("Room")
                                   select new XElement("Room",
                                    new XAttribute("ID", string.Empty),
                                    new XAttribute("RoomType", x.Attribute("RoomType").Value),
                                    new XAttribute("ServiceID", ""),
                                    new XAttribute("RefNo", myEle.Element(ns + "BookingCode") != null ? myEle.Element(ns + "BookingCode").Value : string.Empty),
                                     new XAttribute("MealPlanID", x.Attribute("MealPlanID").Value),
                                     new XAttribute("MealPlanName", ""),
                                    new XAttribute("MealPlanCode", ""),
                                    new XAttribute("MealPlanPrice", x.Attribute("MealPlanPrice").Value),
                                    new XAttribute("PerNightRoomRate", ""),
                                    new XAttribute("RoomStatus", myEle.Element(ns + "BookingStatusName").Value),
                                    new XAttribute("TotalRoomRate", string.Empty),
                                    GuestInfo(x),
                                   new XElement("Supplements", ""));
            return PassengersDetail;
        }


        IEnumerable<XElement> GuestInfo(XElement GuestInfo)
        {



            IEnumerable<XElement> guest = from x in GuestInfo.Descendants("PaxInfo").Where(x => x.Element("IsLead").Value == "true")
                                          select new XElement("RoomGuest",
                                new XElement("GuestType", x.Element("GuestType").Value),
                                new XElement("Title", x.Element("Title").Value),
                                new XElement("FirstName", x.Element("FirstName").Value),
                                new XElement("MiddleName", x.Element("MiddleName").Value),
                                new XElement("LastName", x.Element("LastName").Value),
                                new XElement("IsLead", x.Element("IsLead").Value),
                                 new XElement("Age", x.Element("Age").Value));
            return guest;




        }
        #endregion


        #region GetVoucherResponce

        XDocument GetVoucherDoc(XDocument Bokresponce, XElement req)
        {
            RTS_VoucherRequest obj = new RTS_VoucherRequest();
            return obj.VoucherRequest(Bokresponce, req);
        }
        #endregion




        void PendingBokCXL(string sitecode, string Password, string Requetype, string RTSalesUserNo, string BookingCode,string url)
        {
            //string RTSCXlReasonCode = ConfigurationManager.AppSettings["RTSCXlReasonCode"].ToString();

            #region Credential
            XElement suppliercred = supplier_Cred.getsupplier_credentials(request.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string RTSCXlReasonCode = suppliercred.Descendants("RTSCXlReasonCode").FirstOrDefault().Value;
            #endregion

            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace rts = "http://www.rts.co.kr/";
            XElement ele = new XElement(soap + "Envelope",
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
                                new XElement(rts + "BookingCode", BookingCode),
                                new XElement(rts + "ItemNo", 0),
                                new XElement(rts + "CancelReasonCode", RTSCXlReasonCode),
                                new XElement(rts + "LastWriterUno", RTSalesUserNo)))));
           

            RequestClass obj = new RequestClass();
            XDocument Responce = obj.HttpPostRequest(url, ele.ToString(), ele, "Pending Booking Cancel", 6);
            IEnumerable<XElement> ele1 = Responce.Descendants(rts + "BookingItemInfo");
            foreach (XElement item in ele1)
            {
                if (item.Element(rts + "ItemStatusCode").Value == "BS05")
                {
                    XElement cancel = Responce.Descendants(rts + "BookingItemInfo").FirstOrDefault();
                   

                }
            }
        }

    }
}