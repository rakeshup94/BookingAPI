using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Common.DotW;
using System.Text.RegularExpressions;
using TravillioXMLOutService.Common.JacTravel;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Jac_HotelAvail
    {
        int totalrooms;
        public List<XElement> GetSerachResponce(XElement doc, int duration, XElement myEle, XElement mealtype, int room, XElement Facility, int RegionID, XElement Req, string dmc, int supid,string custID)
        {
            totalrooms = room;
            int minStar = Convert.ToInt32(Req.Descendants("MinStarRating").FirstOrDefault().Value), maxStar = Convert.ToInt32(Req.Descendants("MaxStarRating").FirstOrDefault().Value);
            string status = (String)doc.Element("ReturnStatus").Element("Success");
            if (status == "true")
            {

                Regex reg = new Regex("\"");
                var hh = doc.Descendants("PropertyResult");
                Req = Req.Descendants("searchRequest").FirstOrDefault();

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
                
                List<XElement> HotelList = new List<XElement>();
                foreach (XElement htl in doc.Descendants("PropertyResult"))
                {
                    if (Convert.ToInt32(htl.Element("Rating").Value.Substring(0,1)) >= minStar && Convert.ToInt32(htl.Element("Rating").Value.Substring(0,1)) <= maxStar)
                    {
                        try
                        {
                            XElement htldesc = myEle.Descendants("Property").Where(x => x.Element("PropertyReferenceID").Value.Equals(htl.Element("PropertyReferenceID").Value)).FirstOrDefault();
                            HotelList.Add(new XElement("Hotel",
                                                new XElement("HotelID", htl.Element("PropertyReferenceID").Value),
                                                new XElement("HotelName", reg.Replace(htldesc.Element("PropertyName").Value, string.Empty)),
                                                new XElement("PropertyTypeName", reg.Replace(htldesc.Element("PropertyType").Value, string.Empty)),
                                                new XElement("CountryID", Req.Element("CountryID").Value),
                                                    new XElement("RequestID", htl.Element("PropertyReferenceID").Value),
                                                new XElement("CountryCode", Req.Element("CountryCode").Value),
                                                new XElement("CountryName", reg.Replace(htldesc.Element("Country").Value, string.Empty)),
                                                new XElement("CityID", Req.Element("CityID").Value == null ? string.Empty : Req.Element("CityID").Value),
                                                new XElement("CityCode", Req.Element("CityCode").Value == null ? string.Empty : Req.Element("CityCode").Value),
                                                new XElement("CityName", htldesc.Element("Region").Value == null ? string.Empty : htldesc.Element("Region").Value),
                                                new XElement("AreaName", reg.Replace(htldesc.Element("Resort").Value, string.Empty)),
                                                new XElement("AreaId", RegionID),
                                                new XElement("Address", reg.Replace(htldesc.Element("Address1").Value, string.Empty)),
                                                new XElement("Location", htldesc.Element("TownCity").Value),
                                                new XElement("Description", string.Empty),
                                                new XElement("StarRating", htldesc.Element("Rating").Value),
                                                new XElement("MinRate", minratesup(htl)),
                                                new XElement("HotelImgSmall", htldesc.Element("ThumbnailURL").Value),
                                                new XElement("HotelImgLarge", htldesc.Element("Image1URL").Value),
                                                new XElement("MapLink", ""),
                                                new XElement("Longitude", htldesc.Element("Longitude").Value),
                                                new XElement("Latitude", htldesc.Element("Latitude").Value),
                                                new XElement("xmloutcustid", custID),
                                                new XElement("xmlouttype", xmlouttype),
                                                new XElement("DMC", dmc),
                                                new XElement("SupplierID", supid),
                                                new XElement("Currency", "USD"),
                                                new XElement("Offers", ""),
                                                new XElement("Facilities", null)
                                //AddFacilityTag(Facility.Descendants("Facility"), htldesc))
                                                , new XElement("Rooms", "")));
                        }
                        catch { } 
                    }
                }
                return HotelList;
            }
            else if (status == "false")
            {

                return null;
            }
            return null;
        }
        private decimal minratesup(XElement rooms)
        {
            decimal minrt = 0;
            try
            {
                if (rooms.Descendants("RoomTypes").Descendants("RSP").Any())
                {
                    List<decimal> roomRates = rooms.Descendants("RSP").Select(x => Convert.ToDecimal(x.Value)).Distinct().ToList();
                    minrt = roomRates.Min();
                }
                else
                {
                    List<XElement> tttroom = rooms.Descendants("RoomTypes").Elements("RoomType").ToList();
                    for (int i = 0; i < totalrooms; i++)
                    {
                        minrt = tttroom.Where(x => x.Descendants("Seq").FirstOrDefault().Value == Convert.ToString(i + 1)).Min(x => (decimal)x.Element("Total"));
                    }
                }
            }
            catch { }
            return minrt;
        }
        decimal Test(List<XElement> rmlst, int duration, XElement mealtype, string hotelcode)
        {
            Dictionary<int, List<XElement>> dic = new Dictionary<int, List<XElement>>();
            for (int i = 1; i <= totalrooms; i++)
            {
                List<XElement> test = rmlst.Elements("RoomType").Where(el => Convert.ToInt16(el.Descendants("Seq").FirstOrDefault().Value) == i).ToList();               
                if (test!=null)
                {
                    test = GroupOfRoom(test, duration);
                    dic.Add(i, test);
                }               
            }
            decimal minrate = 0;
            List<XElement> lst=null;
            for (int i = 1; i <= totalrooms; i++)
            {
                List<XElement> item = dic[i];               
                lst = Getitem(i, item, lst, hotelcode);
            }
            foreach (XElement item in lst)
            {
                if (minrate==0)
                {
                    minrate = Convert.ToDecimal(item.Attribute("TotalRate").Value);
                }
               else if (Convert.ToDecimal(item.Attribute("TotalRate").Value)<minrate)
                {
                    minrate = Convert.ToDecimal(item.Attribute("TotalRate").Value);
                }
            }
            return minrate;            
        }
        List<XElement> Getitem(int i, List<XElement> item, List<XElement> lst, string hotelcode)
        {
            List<XElement> testlst = new List<XElement>();
            if (lst != null)
            {
                foreach (XElement item1 in item)
                {
                    foreach (XElement item2 in lst)
                    {
                        decimal totalrate = Convert.ToDecimal(item2.Attribute("TotalRate").Value) + Convert.ToDecimal(item1.Attribute("TotalRoomRate").Value);
                        if (item1.Attribute("MealPlanID").Value == item2.Attribute("MealPlanID").Value)
                        {
                            IEnumerable<XElement> romcoll = item2.Descendants("Room");
                            testlst.Add(new XElement("RoomTypes",
                            new XAttribute("TotalRate", totalrate),
                            new XAttribute("Index", testlst.Count + 1),
                                 new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"),
                            item1,
                            romcoll));
                        }
                    }
                }
            }
            else if (lst == null)
            {
                foreach (XElement item1 in item)
                {
                    decimal totalrate = Convert.ToDecimal(item1.Attribute("TotalRoomRate").Value);
                    testlst.Add(new XElement("RoomTypes",
                        new XAttribute("TotalRate", totalrate),
                        new XAttribute("Index", testlst.Count + 1),
                             new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"),
                        item1));
                }
            }
            return testlst;
        }
        public IEnumerable<XElement> AddFacilityTag(IEnumerable<XElement> lst, XElement property)
        {
            string Facilityvalue = property.Element("Facilities") != null ? property.Element("Facilities").Value.Replace("'\'", "") : string.Empty;
            List<string> list = null;
            IEnumerable<XElement> Facilities;
            if (!string.IsNullOrEmpty(Facilityvalue))
            {
                list = Facilityvalue.Split(',').ToList();
                Facilities = from itm in lst
                             join x in list
                             on itm.Element("FacilityID").Value equals x
                             select new XElement("Facility", itm.Element("FacilityName").Value);
            }            
            else
            {
                Facilities = new List<XElement>() { new XElement("Facility", "No Facility Available") };
            }
            return Facilities;
        }



        public IEnumerable<XElement> CreateRoomTag(List<XElement> rmlst, int duration, XElement mealtype)
        {

            List<XElement> str = new List<XElement>();
            List<XElement> roomList1 = new List<XElement>();
            List<XElement> roomList2 = new List<XElement>();
            List<XElement> roomList3 = new List<XElement>();
            List<XElement> roomList4 = new List<XElement>();
            List<XElement> roomList5 = new List<XElement>();


            #region Room 1
            if (totalrooms == 1 || totalrooms > 1)
            {
                roomList1 = rmlst.Elements("RoomType").Where(el => el.Descendants("Seq").FirstOrDefault().Value == "1").ToList();
                roomList1 = GroupOfRoom(roomList1, duration);

                if (totalrooms > 1)
                {
                    roomList2 = rmlst.Elements("RoomType").Where(el => el.Descendants("Seq").FirstOrDefault().Value == "2").ToList();
                    roomList2 = GroupOfRoom(roomList2, duration);
                }

                if (totalrooms > 2)
                {
                    roomList3 = rmlst.Elements("RoomType").Where(el => el.Descendants("Seq").FirstOrDefault().Value == "3").ToList();
                    roomList3 = GroupOfRoom(roomList3, duration);
                }
                int count = 0;
                foreach (XElement item in roomList1)
                {
                    decimal totalrate = Convert.ToDecimal(item.Attribute("TotalRoomRate").Value);

                    if (roomList2.Count > 0)
                    {
                        foreach (XElement item2 in roomList2)
                        {
                            if (item2.Attribute("MealPlanID").Value == item.Attribute("MealPlanID").Value)
                            {
                                totalrate = Convert.ToDecimal(item.Attribute("TotalRoomRate").Value);
                                totalrate = totalrate + Convert.ToDecimal(item2.Attribute("TotalRoomRate").Value);

                                if (roomList3.Count > 0)
                                {
                                    foreach (var item3 in roomList3)
                                    {
                                        if (item3.Attribute("MealPlanID").Value == item.Attribute("MealPlanID").Value)
                                        {
                                            count++;
                                            str.Add(new XElement("RoomTypes",
                                          new XAttribute("Index", count),
                                          new XAttribute("TotalRate", totalrate + Convert.ToDecimal(item3.Attribute("TotalRoomRate").Value)),
                                          item,
                                          item2,
                                          item3));
                                        }
                                    }
                                }
                                else
                                {
                                    count++;
                                    str.Add(new XElement("RoomTypes",
                                  new XAttribute("Index", count),
                                  new XAttribute("TotalRate", totalrate),
                                  item,
                                  item2));
                                }
                            }

                        }
                    }
                    else
                    {
                        count++;
                        str.Add(new XElement("RoomTypes",
                            new XAttribute("Index", count),
                            new XAttribute("TotalRate", totalrate), item));
                    }

                }

                return str;
            }
            #endregion


            return str;
        }




        XElement BindAmenity(IEnumerable<XElement> Amtylst)
        {
            XElement AmtyItem;
            if (Amtylst != null)
            {
                var result = from amty in Amtylst
                             select new XElement("Amenity", amty.Value);
                AmtyItem = new XElement("Amenities", result);
            }
            else
            {
                AmtyItem = new XElement("Amenities", "");

            }
            return AmtyItem;
        }


        List<XElement> GroupOfRoom(IEnumerable<XElement> ele, int duration)
        {
            IEnumerable<XElement> Room = from x in ele
                                         select new XElement("Room",
                                                    new XAttribute("ID", x.Element("PropertyRoomTypeID").Value),
                                                    new XAttribute("SuppliersID", "8"),
                                                    new XAttribute("RoomSeq", x.Element("Seq").Value),
                                                    new XAttribute("SessionID", x.Element("PropertyRoomTypeID").Value == "0" ? x.Element("BookingToken").Value : ""),
                                                    new XAttribute("RoomType", x.Element("RoomType").Value),
                                                    new XAttribute("OccupancyID", ""),
                                                    new XAttribute("OccupancyName", ""),
                                                    new XAttribute("MealPlanID", x.Element("MealBasisID").Value),
                                                    new XAttribute("MealPlanName", x.Element("MealBasis").Value),

                                                    new XAttribute("MealPlanCode", ""),
                                                    new XAttribute("MealPlanPrice", ""),
                                                    new XAttribute("PerNightRoomRate", JacHelper.PerNightPrice(x.Element("Total").Value, duration)),
                                                    new XAttribute("TotalRoomRate", x.Element("Total").Value),
                                                    new XAttribute("CancellationDate", ""),
                                                    new XAttribute("CancellationAmount", x.Element("Errata").Value),
                                                    new XAttribute("isAvailable", true),
                                                    new XElement("RequestID", ""),
                                                    new XElement("Offers", ""),
                                                     new XElement("PromotionList", new XElement("Promotions", x.Element("SpecialOfferApplied") != null ? x.Element("SpecialOfferApplied").Value : string.Empty)),
                                                     new XElement("CancellationPolicy", ""),
                                                      new XElement("PriceBreakups", JacHelper.PreiceBkp(x.Element("Total").Value, duration)),
                                                      BindAmenity(null),
                                                       new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                        new XElement("Supplements", x.Element("OptionalSupplements").Value),
                                                    new XElement("AdultNum", x.Element("Adults").Value),
                                                    new XElement("ChildNum", Convert.ToInt16(x.Element("Children").Value) + Convert.ToInt16(x.Element("Infants").Value)));

            return Room.ToList();
        }



        string GetTermCondition(XElement errata)
        {
            string detail = string.Empty;

            foreach (var item in errata.Elements("Erratum"))
            {
                if (item.Element("Subject") != null)
                {
                    detail = detail + " " + item.Element("Subject").Value;
                }
                else if (item.Element("Description") != null)
                {
                    detail = detail + " " + item.Element("Description").Value;
                }
               
            }
            return detail;
        }
    }
}
