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
    public class RTS_RoomAvail
    {

        string hotelcode = string.Empty;
        string customerid = string.Empty;
        #region GetRoomHotelWise
        public List<XElement> getroomavailability_RTS(XDocument doc, XElement req)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "9").ToList();
                for (int i = 0; i < htlele.Count(); i++)
                {
                    string custID = string.Empty;
                    string custName = string.Empty;
                    string htlid = htlele[i].Attribute("GHtlID").Value;
                    string xmlout = string.Empty;
                    try
                    {
                        xmlout = htlele[i].Attribute("xmlout").Value;
                    }
                    catch { xmlout = "false"; }
                    if (xmlout == "true")
                    {
                        try
                        {
                            customerid = htlele[i].Attribute("custID").Value;
                            dmc = htlele[i].Attribute("custName").Value;
                        }
                        catch { custName = "HA"; }
                    }
                    else
                    {
                        try
                        {
                            customerid = htlele[i].Attribute("custID").Value;
                        }
                        catch { }
                        dmc = "RTS";
                    }
                    IEnumerable<XElement> getrom = RoomAvailReq(doc, req, dmc, htlid);
                    roomavailabilityresponse.Add(getrom.Descendants("Rooms").FirstOrDefault());
                }
                #endregion
                return roomavailabilityresponse;
            }
            catch { return null; }
        }
        public IEnumerable<XElement> RoomAvailReq(XDocument responce, XElement Req,string dmc,string hotelid)
        {
            try
            {
                hotelcode = hotelid;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "9");
                XNamespace ns = "http://www.rts.co.kr/";
                foreach (XElement Htlsearchlst in responce.Descendants(ns + "HotelSearchListNetGuestCount"))
                {
                    foreach (XElement ItemCodelst in Htlsearchlst.Descendants(ns + "ItemCodeInfo"))
                    {
                        //ItemCodelst.Element(ns + "ItemCode").Value = Req.Descendants("searchRequest").FirstOrDefault().Element("HotelID").Value;
                        ItemCodelst.Element(ns + "ItemCode").Value = hotelid;
                        if (!string.IsNullOrEmpty(ItemCodelst.Element(ns + "ItemCode").Value))
                        {
                            //string RTSrhhtlURL = ConfigurationManager.AppSettings["RTSrhhtlURL"].ToString();
                            string RTSrhhtlURL = suppliercred.Descendants("RTSrhhtlURL").FirstOrDefault().Value;
                            RequestClass obj = new RequestClass();
                            XDocument ele = obj.HttpPostRequestxmlout(RTSrhhtlURL, responce.ToString(), Req, "RoomAvail", 1,customerid);
                            IEnumerable<XElement> lst = GetRom(ele, Req,dmc);
                            return lst;
                        }                       
                    }
                }
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomAvailReq";
                ex1.PageName = "RTS_RoomAvail";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;                 
            }
           
            return null;
        }


        public IEnumerable<XElement> GetRom(XDocument responce, XElement Req,string dmc)
        {
            XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "9");
            string RTSClientCurrencyCode = suppliercred.Descendants("RTSClientCurrencyCode").FirstOrDefault().Value;
            //string RTSClientCurrencyCode = ConfigurationManager.AppSettings["RTSClientCurrencyCode"].ToString();
            XNamespace ns = "http://www.rts.co.kr/";
            IEnumerable<XElement> Htlst = from Srch in Req.Descendants("searchRequest")
                                          join htl in responce.Descendants(ns + "HotelItemInfo")
             on hotelcode equals htl.Element(ns + "ItemCode").Value
                                          select 
                                              new XElement("Hotel",
                                          new XElement("HotelID", htl.Element(ns + "ItemCode").Value),
                                          new XElement("HotelName", htl.Element(ns + "ItemName").Value),
                                         // new XElement("PropertyTypeName", string.Empty),
                                         // new XElement("CountryID", Srch.Element("CountryID").Value),
                                         //  new XElement("RequestID", string.Empty),
                                         // new XElement("CountryCode", responce.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CountryCode").Value),
                                         // new XElement("CountryName", responce.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CountryName").Value),
                                         // new XElement("CityID", Srch.Element("CityID") == null ? string.Empty : Srch.Element("CityID").Value),
                                         // new XElement("CityCode", responce.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityCode") == null ? string.Empty : responce.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityCode").Value),
                                         // new XElement("CityName", responce.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityName") == null ? string.Empty : responce.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityName").Value),
                                         // new XElement("AreaName", Srch.Element("AreaName").Value),
                                         // new XElement("AreaId", ""),
                                         // new XElement("Address", string.Empty),
                                         // new XElement("Location", ""),
                                         // new XElement("Description", string.Empty),
                                         // new XElement("StarRating", htl.Element(ns + "StarRating").Value),
                                         // new XElement("MinRate", GetMinRate(ns, htl)),
                                         //new XElement("HotelImgSmall",string.Empty),
                                         // new XElement("HotelImgLarge", string.Empty),
                                         // new XElement("MapLink", string.Empty),
                                         // new XElement("Longitude", responce.Element(ns + "GeoCode") != null ? responce.Element(ns + "GeoCode").Element(ns + "Longitude").Value : string.Empty),
                                         // new XElement("Latitude", responce.Element(ns + "GeoCode") != null ? responce.Element(ns + "GeoCode").Element(ns + "Latitude").Value : string.Empty),
                                          new XElement("DMC", dmc),
                                          new XElement("SupplierID", "9"),
                                          new XElement("Currency", RTSClientCurrencyCode),
                                          new XElement("Offers", ""),
                                          new XElement("Facilities", new XElement("Facility", "No Facility Available")),
                                          new XElement("Rooms", AddRom(htl.Descendants(ns + "PriceInfo"), Srch, ns, htl.Element(ns + "ItemCode").Value, RTSClientCurrencyCode, dmc)));


            //XElement ele = Htlst.FirstOrDefault();
            SaveLog(Req, Htlst.FirstOrDefault());
            return Htlst;

        }

        public IEnumerable<XElement> AddRom(IEnumerable<XElement> romcoll, XElement srch, XNamespace ns, string hotelcode, string RTSClientCurrencyCode,string dmc)
        {            
            List<XElement> Romlst = new List<XElement>();
            int index = 1;
            foreach (XElement item in romcoll)
            {
                int value = 0;
                decimal RomPrice = Convert.ToDecimal(item.Element(ns + "SellerNetPrice").Value) / srch.Descendants("RoomPax").Count();
                string duration = JacHelper.GetDuration(srch.Element("ToDate").Value, srch.Element("FromDate").Value, out value);
                decimal PerN8Price = RomPrice / value;
                XElement romtype = new XElement("RoomTypes",
                    new XAttribute("TotalRate", item.Element(ns + "SellerNetPrice").Value),
                    new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", RTSClientCurrencyCode), new XAttribute("DMCType", dmc), new XAttribute("CUID", customerid),
                    new XAttribute("Index", index));
                foreach (XElement item1 in srch.Descendants("RoomPax"))
                {
                    XElement ele = new XElement("Room",
                                                        new XAttribute("ID", item.Element(ns + "RoomTypeCode").Value),
                                                        new XAttribute("SuppliersID", "9"),
                                                        new XAttribute("RoomSeq","1"),                                                        
                                                        new XAttribute("SessionID", item.Element(ns + "ItemNo").Value),
                                                        new XAttribute("RoomType", item.Element(ns + "RoomTypeName").Value),
                                                        new XAttribute("OccupancyID", item.Element(ns + "SellerMarkupPrice").Value),
                                                        new XAttribute("OccupancyName", ""),
                                                        new XAttribute("MealPlanID", item.Element(ns + "BreakfastTypeName").Value != "None" ? item.Element(ns + "BreakfastTypeName").Value : string.Empty),
                                                        new XAttribute("MealPlanName", item.Element(ns + "BreakfastTypeName").Value != "None" ? item.Element(ns + "BreakfastTypeName").Value : string.Empty),
                                                        new XAttribute("MealPlanCode", ""),
                                                        new XAttribute("MealPlanPrice", ""),
                                                        new XAttribute("PerNightRoomRate", PerN8Price),
                                                        new XAttribute("TotalRoomRate", RomPrice),
                                                        new XAttribute("CancellationDate", ""),
                                                        new XAttribute("CancellationAmount", string.Empty),
                                                        new XAttribute("isAvailable", true),
                                                        new XElement("RequestID", ""),
                                                        new XElement("Offers", ""),
                                                         new XElement("PromotionList", item.Element(ns + "SupplierPromotion") != null ? new XElement("Promotions", item.Element(ns + "SupplierPromotion").Element(ns + "PromotionName").Value) : null),
                                                         new XElement("CancellationPolicy", ""),
                                                          new XElement("PriceBreakups",JacHelper.PriceBreakup(PerN8Price, value)),
                                                        new XElement("Amenities", new XElement("Amenity", string.Empty)),
                                                           new XElement("Images", new XElement("Image", new XAttribute("Path", string.Empty))),
                                                            new XElement("Supplements", string.Empty),
                                                        new XElement("AdultNum", item1.Element("Adult").Value),
                                                        new XElement("ChildNum", item1.Element("Child").Value));
                    romtype.Add(ele);

                }
                index++;
                Romlst.Add(romtype);
            }

            return Romlst;
        }

      


        public decimal GetMinRate(XNamespace ns, XElement rom)
        {
            IEnumerable<XElement> lstrom = rom.Descendants(ns + "PriceInfo");
            decimal MinRate = 0;
            foreach (XElement item in lstrom)
            {
                if (Convert.ToDecimal(item.Element(ns + "SellerNetPrice").Value) < MinRate || MinRate == 0)
                {
                    MinRate = Convert.ToDecimal(item.Element(ns + "SellerNetPrice").Value);
                   
                }
            }
            return MinRate;
        }




        #endregion


        void SaveLog(XElement Req,XElement responce)
        {
            APILogDetail log = new APILogDetail();
            log.customerID = Convert.ToInt64(Req.Descendants("CustomerID").Single().Value);
            log.LogTypeID = 2;
            log.LogType = "RoomAvail";
            log.SupplierID = 9;
            log.StartTime = DateTime.Now;
            log.EndTime = DateTime.Now;
            log.TrackNumber =Req.Descendants("TransID").Single().Value;

            log.logrequestXML = Req.ToString();
            log.logresponseXML = Convert.ToString(responce);
            SaveAPILog savelog = new SaveAPILog();
            savelog.SaveAPILogs(log);

        }
    }
}