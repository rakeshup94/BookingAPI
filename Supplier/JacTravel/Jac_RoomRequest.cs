using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.JacTravel;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Jac_RoomRequest
    {
        string dmc = string.Empty;
        string hotelid = string.Empty;
        int supplierid = 0;
        string customerid = string.Empty;
        public List<XElement> getroomavailability_jactotal(XElement req, int supid)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == Convert.ToString(supid)).ToList();
                for (int i = 0; i < htlele.Count(); i++)
                {
                    string custID = string.Empty;
                    string custName = string.Empty;
                    string grequestid = string.Empty;
                    string htlid = htlele[i].Attribute("GHtlID").Value;
                    string xmlout = string.Empty;
                    try
                    {
                        xmlout = htlele[i].Attribute("xmlout").Value;
                        grequestid = htlele[i].Attribute("GRequestID").Value;
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
                        if(supid==8)
                        {
                            dmc = "JacTravels";
                        }
                        else if (supid == 37)
                        {
                            dmc = "TotalStay";
                        }
                    }
                    Jac_RoomRequest hbreq = new Jac_RoomRequest();
                    IEnumerable<XElement> getrom = hbreq.RoomRequest(req, dmc, supid, htlid, customerid);
                    roomavailabilityresponse.Add(getrom.Descendants("Rooms").FirstOrDefault());
                }
                #endregion
                return roomavailabilityresponse;
            }
            catch { return null; }
        }
        public IEnumerable<XElement> RoomRequest(XElement Req,string xtype,int supid,string htlid,string custoid)
        {
            try
            {
                customerid = custoid;
                DataTable CityMapping = jac_staticdata.CityMapping(Req.Descendants("CityID").FirstOrDefault().Value, Convert.ToString(supid));
                int RegionID = Convert.ToInt32(CityMapping.Rows[0]["SupCityId"].ToString());
                string jacpath = ConfigurationManager.AppSettings["JacPath"];
                XElement JacHtl = XElement.Load(Path.Combine(HttpRuntime.AppDomainAppPath, jacpath + @"Property\" + RegionID + ".xml"));
                XElement Jac_Meal = XElement.Load(Path.Combine(HttpRuntime.AppDomainAppPath, jacpath + @"MealBasis.xml"));
                hotelid = htlid;
                dmc = xtype;
                supplierid = supid;
                HTlStaticData obj = new HTlStaticData();
                IEnumerable<XElement> doc = obj.GetJacRoomAvailable(Req, supid,hotelid);
                if (doc != null)
                {
                    int value = 0;
                    foreach (var item in Req.Descendants("searchRequest"))
                    {
                        JacHelper.GetDuration(item.Element("ToDate").Value, item.Element("FromDate").Value, out value);
                    }

                    return RomResponce(doc, value, JacHtl, Jac_Meal, Req, RegionID);

                }


            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "RoomRequest";
                ex1.PageName = "Jac_RoomRequest";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null; 
            }

            return null;
        }



        int totalrooms;
        public IEnumerable<XElement> RomResponce(IEnumerable<XElement> doc, int duration, XElement myEle, XElement mealtype, XElement Req, int RegionID)
        {

            totalrooms = Req.Descendants("RoomPax").Count();
            Regex reg = new Regex("\"");
            #region Add Sequence in B2B request
            int seq = 1;
            foreach(XElement room in Req.Descendants("RoomPax"))
            {
                room.Add(new XElement("Seq", seq));
                seq++;
            }
            #endregion

            Req = Req.Descendants("searchRequest").FirstOrDefault();
            IEnumerable<XElement> CountryList = from htl in doc
                                                join htldesc in myEle.Descendants("Property")
                                                on htl.Element("PropertyReferenceID").Value equals htldesc.Element("PropertyReferenceID").Value
                                                select new XElement("Hotel",
                                                            new XElement("HotelID", htl.Element("PropertyID").Value),
                                                            //new XElement("HotelName", reg.Replace(htldesc.Element("PropertyName").Value, string.Empty)),
                                                            //new XElement("PropertyTypeName", reg.Replace(htldesc.Element("PropertyType").Value, string.Empty)),
                                                            //new XElement("CountryID", Req.Element("CountryID").Value),
                                                            // new XElement("RequestID", htl.Element("PropertyReferenceID").Value),
                                                            //new XElement("CountryCode", string.Empty),
                                                            //new XElement("CountryName", reg.Replace(htldesc.Element("Country").Value, string.Empty)),
                                                            //new XElement("CityID", Req.Element("CityID").Value == null ? string.Empty : Req.Element("CityID").Value),
                                                            //new XElement("CityCode", Req.Element("CityCode").Value == null ? string.Empty : Req.Element("CityCode").Value),
                                                            //new XElement("CityName", htldesc.Element("Region").Value == null ? string.Empty : htldesc.Element("Region").Value),
                                                            //new XElement("AreaName", reg.Replace(htldesc.Element("Resort").Value, string.Empty)),
                                                            //new XElement("AreaId", RegionID),
                                                            //new XElement("Address", reg.Replace(htldesc.Element("Address1").Value, string.Empty)),
                                                            //new XElement("Location", htldesc.Element("TownCity").Value),
                                                            //new XElement("Description", string.Empty),
                                                            //new XElement("StarRating", htldesc.Element("Rating").Value),
                                                            //new XElement("MinRate", null),
                                                            //new XElement("HotelImgSmall", htldesc.Element("ThumbnailURL").Value),
                                                            //new XElement("HotelImgLarge", htldesc.Element("Image1URL").Value),
                                                            //new XElement("MapLink", ""),
                                                            //new XElement("Longitude", htldesc.Element("Latitude").Value),
                                                            //new XElement("Latitude", htldesc.Element("Longitude").Value),
                                                            new XElement("DMC", dmc),
                                                            new XElement("SupplierID", supplierid),
                                                            new XElement("Currency", "USD"),
                                                            new XElement("Offers", ""),
                                                            //new XElement("Facilities", new XElement("Facility", "No Facility Available")),
                                                            new XElement("Rooms", Test(htl.Descendants("RoomTypes").ToList(), duration, mealtype, htl.Element("PropertyID").Value, Req.Descendants("Rooms").FirstOrDefault())));

            XElement elem = CountryList.FirstOrDefault();
            return CountryList;



        }

        //public IEnumerable<XElement> CreateRoomTag(List<XElement> rmlst, int duration, XElement mealtype, string hotelcode)
        //{

        //    List<XElement> str = new List<XElement>();
        //    List<XElement> roomList1 = new List<XElement>();
        //    List<XElement> roomList2 = new List<XElement>();
        //    List<XElement> roomList3 = new List<XElement>();
        //    List<XElement> roomList4 = new List<XElement>();
        //    List<XElement> roomList5 = new List<XElement>();


        //    #region Room 1
        //    if (totalrooms == 1 || totalrooms > 1)
        //    {
        //        roomList1 = rmlst.Elements("RoomType").Where(el => el.Descendants("Seq").FirstOrDefault().Value == "1").ToList();
        //        roomList1 = GroupOfRoom(roomList1, duration);

        //        if (totalrooms > 1)
        //        {
        //            roomList2 = rmlst.Elements("RoomType").Where(el => el.Descendants("Seq").FirstOrDefault().Value == "2").ToList();
        //            roomList2 = GroupOfRoom(roomList2, duration);
        //        }

        //        if (totalrooms > 2)
        //        {
        //            roomList3 = rmlst.Elements("RoomType").Where(el => el.Descendants("Seq").FirstOrDefault().Value == "3").ToList();
        //            roomList3 = GroupOfRoom(roomList3, duration);
        //        }
        //        int count = 0;
        //        foreach (XElement item in roomList1)
        //        {
        //            decimal totalrate = Convert.ToDecimal(item.Attribute("TotalRoomRate").Value);

        //            if (roomList2.Count > 0)
        //            {
        //                foreach (XElement item2 in roomList2)
        //                {
        //                    if (item2.Attribute("MealPlanID").Value == item.Attribute("MealPlanID").Value)
        //                    {
        //                        totalrate = Convert.ToDecimal(item.Attribute("TotalRoomRate").Value);
        //                        totalrate = totalrate + Convert.ToDecimal(item2.Attribute("TotalRoomRate").Value);

        //                        if (roomList3.Count > 0)
        //                        {
        //                            foreach (var item3 in roomList3)
        //                            {
        //                                if (item3.Attribute("MealPlanID").Value == item.Attribute("MealPlanID").Value)
        //                                {
        //                                    count++;
        //                                    str.Add(new XElement("RoomTypes",
        //                                  new XAttribute("Index", count),
        //                                  new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"),
        //                                  new XAttribute("TotalRate", totalrate + Convert.ToDecimal(item3.Attribute("TotalRoomRate").Value)),
        //                                  item,
        //                                  item2,
        //                                  item3));
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            count++;
        //                            str.Add(new XElement("RoomTypes",
        //                          new XAttribute("Index", count),
        //                           new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"),
        //                          new XAttribute("TotalRate", totalrate),
        //                          item,
        //                          item2));
        //                        }
        //                    }

        //                }
        //            }
        //            else
        //            {
        //                count++;
        //                str.Add(new XElement("RoomTypes",
        //                    new XAttribute("Index", count),
        //                     new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"),
        //                    new XAttribute("TotalRate", totalrate), item));
        //            }

        //        }

        //        return str;
        //    }
        //    #endregion


        //    return str;
        //}



        public IEnumerable<XElement> Test(List<XElement> rmlst, int duration, XElement mealtype, string hotelcode, XElement Rooms)
        {
            Dictionary<int, List<XElement>> dic = new Dictionary<int, List<XElement>>();
            for (int i = 1; i <= totalrooms; i++)
            {
                List<XElement> test = rmlst.Elements("RoomType").Where(el => Convert.ToInt16(el.Descendants("Seq").FirstOrDefault().Value) == i).ToList();
                if (test != null)
                {
                    test = GroupOfRoom(test, duration,Rooms );
                    dic.Add(i, test);
                }
            }
            List<XElement> lst = null;
            for (int i = totalrooms; i >= 1; i--)
            {
                List<XElement> item = dic[i];
                lst = Getitem(i, item, lst, hotelcode);
            }
            return lst;
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
                        if (item1.Attribute("MealPlanID").Value == item2.Descendants("Room").FirstOrDefault().Attribute("MealPlanID").Value)
                        {
                            IEnumerable<XElement> romcoll = item2.Descendants("Room");
                            testlst.Add(new XElement("RoomTypes",
                            new XAttribute("TotalRate", totalrate),
                            new XAttribute("Index", testlst.Count + 1),
                                 new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"), new XAttribute("DMCType", dmc),new XAttribute("CUID",customerid),
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
                             new XAttribute("HtlCode", hotelcode), new XAttribute("CrncyCode", "USD"), new XAttribute("DMCType", dmc),
                        item1));
                }
            }
            try
            {
                var rsult = testlst.GroupBy(x => x.Attribute("TotalRate").Value).Select(y => y.First()).OrderBy(z => z.Attribute("TotalRate").Value).ToList().Take(200);
                IEnumerable<XElement> rooms = rsult;
                testlst = rooms.ToList();
            }
            catch { return null; }
            return testlst;
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


        List<XElement> GroupOfRoom(IEnumerable<XElement> ele, int duration, XElement Rooms)
        {
            IEnumerable<XElement> Room = from x in ele                                         
                                         join room in Rooms.Descendants("RoomPax") on x.Element("Seq").Value equals room.Element("Seq").Value
                                         select new XElement("Room",
                                                    new XAttribute("ID", x.Element("PropertyRoomTypeID").Value),
                                                    new XAttribute("SuppliersID", supplierid),
                                                    new XAttribute("RoomSeq", x.Element("Seq").Value),
                                                    new XAttribute("SessionID", x.Element("PropertyRoomTypeID").Value == "0" ? x.Element("BookingToken").Value : ""),
                                                    new XAttribute("RoomType", x.Element("RoomType").Value),
                                                    new XAttribute("OccupancyID", ""),
                                                    new XAttribute("OccupancyName", ""),
                                                    new XAttribute("MealPlanID", x.Element("MealBasisID").Value),
                                                    new XAttribute("MealPlanName", x.Element("MealBasis").Value),

                                                    new XAttribute("MealPlanCode", MPC(x.Element("MealBasisID").Value)),
                                                    new XAttribute("MealPlanPrice", ""),
                                                    new XAttribute("PerNightRoomRate", JacHelper.PerNightPrice(x.Descendants("RSP").Any()? x.Descendants("RSP").FirstOrDefault().Value: x.Element("Total").Value, duration)),
                                                    new XAttribute("TotalRoomRate", x.Descendants("RSP").Any() ? x.Descendants("RSP").FirstOrDefault().Value : x.Element("Total").Value),
                                                    new XAttribute("CancellationDate", ""),
                                                    new XAttribute("CancellationAmount", x.Element("Errata").Value),
                                                    new XAttribute("isAvailable", true),
                                                    new XElement("RequestID", ""),
                                                    new XElement("Offers", ""),
                                                     new XElement("PromotionList", new XElement("Promotions", x.Element("SpecialOfferApplied") != null ? x.Element("SpecialOfferApplied").Value : string.Empty)),
                                                     new XElement("CancellationPolicy", ""),
                                                      new XElement("PriceBreakups", JacHelper.PreiceBkp(x.Descendants("RSP").Any() ? x.Descendants("RSP").FirstOrDefault().Value : x.Element("Total").Value, duration)),
                                                      BindAmenity(null),
                                                       new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                        new XElement("Supplements", x.Element("OptionalSupplements").Value),
                                                    new XElement("AdultNum", room.Element("Adult").Value),
                                                    new XElement("ChildNum", room.Element("Child").Value));

            return Room.OrderBy(x=>x.Attribute("RoomSeq").Value).ToList();
        }
        string MPC(string MP_Id)
        {
            string code = string.Empty;
            XElement MP_List = XElement.Load(Path.Combine(HttpRuntime.AppDomainAppPath, ConfigurationManager.AppSettings["JacPath"] + @"MealBasis.xml"));
            List<string> BBList = new List<string>() { "137","130","141","25","3","8","129","13","14","135","9","131","138","134","27","17" };
            List<string> ROList = new List<string>() { "1", "133", "139","136"};
            List<string> HBList = new List<string>() {"10","4","7","140" };
            List<string> FBList = new List<string>() { "5","24"};
            List<string> AIList = new List<string>() { "2"};
            if (ROList.Contains(MP_Id))
                code = "RO";
            if (BBList.Contains(MP_Id))
                code = "BB";
            if (AIList.Contains(MP_Id))
                return "AI";
            if (HBList.Contains(MP_Id))
                code = "HB";
            if (FBList.Contains(MP_Id))
                code = "FB";
            return code;
        }
    }
}