using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Xml;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TravillioXMLOutService.App_Code
{
    public class TouricoRoomAvail
    {
        #region Tourico Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingTourico(Tourico.RoomType roomlist, XElement reqTravillio)
        {
            List<XElement> str = new List<XElement>();
            List<Tourico.Occupancy> roomList1 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList2 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList3 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList4 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList5 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList6 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList7 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList8 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList9 = new List<Tourico.Occupancy>();
            int totalroom = Convert.ToInt32(reqTravillio.Descendants("RoomPax").Count());

            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();

                for (int m = 0; m < roomList1.Count(); m++)
                {
                    // add room 1

                    #region room's group

                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                    string promotion = string.Empty;
                    if (roomlist.Discount != null)
                    {
                        #region Discount (Tourico) Amount already subtracted
                        try
                        {
                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                            XmlDocument doc3 = new XmlDocument();
                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                            XmlWriter writer3 = XmlWriter.Create(sww3);
                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                            var typxsd = XDocument.Parse(sww3.ToString());
                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                            var distype = typxsd.Root.Attribute(disprefix + "type");
                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                            {
                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                            }
                            else
                            {
                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                        #endregion
                    }
                    if (bb == 0)
                    {
                        group++;
                        #region No Board Bases
                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPublishPrice),
                        new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                             new XAttribute("SuppliersID", "2"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                             new XAttribute("MealPlanID", Convert.ToString("")),
                             new XAttribute("MealPlanName", Convert.ToString("")),
                             new XAttribute("MealPlanCode", ""),
                             new XAttribute("MealPlanPrice", ""),
                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                             new XAttribute("isAvailable", roomlist.isAvailable),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                             new XElement("PromotionList",
                             new XElement("Promotions", Convert.ToString(promotion))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements",
                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                             )));
                        #endregion
                    }
                    else
                    {
                        bool RO = false;
                        #region Board Bases >0
                        Parallel.For(0, bb, j =>
                        //for (int j = 0; j < bb; j++)
                        {
                            int countpaidnight1 = 0;
                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                            {
                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                {
                                    countpaidnight1 = countpaidnight1 + 1;
                                }
                            });
                            if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                            { RO = true; }
                            group++;
                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;

                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalamt),

                            new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                             new XAttribute("SuppliersID", "2"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                             new XAttribute("MealPlanCode", ""),
                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                              new XAttribute("isAvailable", roomlist.isAvailable),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                              new XElement("PromotionList",
                             new XElement("Promotions", Convert.ToString(promotion))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements",
                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                             )));


                        });

                        #region Room Only
                        if (RO == true)
                        {
                            int countpaidnight1 = 0;
                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                            {
                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                {
                                    countpaidnight1 = countpaidnight1 + 1;
                                }
                            });
                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPublishPrice),

                       new XElement("Room",
                        new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                        new XAttribute("SuppliersID", "2"),
                        new XAttribute("RoomSeq", "1"),
                        new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                        new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                        new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                        new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                        new XAttribute("MealPlanID", Convert.ToString("")),
                        new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                        new XAttribute("MealPlanCode", ""),
                        new XAttribute("MealPlanPrice", Convert.ToString("")),
                        new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].occupPublishPrice / countpaidnight1)),
                        new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                        new XAttribute("CancellationDate", ""),
                        new XAttribute("CancellationAmount", ""),
                         new XAttribute("isAvailable", roomlist.isAvailable),
                        new XElement("RequestID", Convert.ToString("")),
                        new XElement("Offers", ""),
                         new XElement("PromotionList",
                        new XElement("Promotions", Convert.ToString(promotion))),
                        new XElement("CancellationPolicy", ""),
                        new XElement("Amenities", new XElement("Amenity", "")),
                        new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                        new XElement("Supplements",
                            GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                            ),
                            new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                            new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                            new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                        )));
                        }
                        #endregion

                        #endregion
                    }

                    //return str;
                    #endregion

                }
                return str;
            }
            #endregion
            #region Room Count 2
            if (totalroom == 2)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {

                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();
                //List<Tourico.Occupancy> roomList2 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 2).ToList();
                Parallel.For(0, roomList1.Count(), m =>
                //for (int m = 0; m < roomList1.Count(); m++)
                {
                    Parallel.For(0, roomList2.Count(), n =>
                    //for (int n = 0; n < roomList2.Count(); n++)
                    {

                        // add room 1, 2

                        #region room's group

                        int bb = roomlist.Occupancies[m].BoardBases.Count();
                        List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                        List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                        string promotion = string.Empty;
                        if (roomlist.Discount != null)
                        {
                            #region Discount (Tourico) Amount already subtracted
                            try
                            {
                                XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                XmlDocument doc3 = new XmlDocument();
                                System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                XmlWriter writer3 = XmlWriter.Create(sww3);
                                xsSubmit3.Serialize(writer3, roomlist.Discount);
                                var typxsd = XDocument.Parse(sww3.ToString());
                                var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                var distype = typxsd.Root.Attribute(disprefix + "type");
                                if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                {
                                    promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                }
                                else
                                {
                                    promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                            #endregion
                        }

                        if (bb == 0)
                        {
                            group++;
                            decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice;
                            #region No Board Bases
                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                            new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", ""),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                 ),

                            new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "2"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", ""),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                 )));
                            #endregion
                        }
                        else
                        {
                            bool RO = false;
                            #region Board Bases >0
                            Parallel.For(0, bb, j =>
                            //for (int j = 0; j < bb; j++)
                            {
                                int countpaidnight1 = 0;
                                Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight1 = countpaidnight1 + 1;
                                    }
                                });
                                int countpaidnight2 = 0;
                                Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight2 = countpaidnight2 + 1;
                                    }
                                });
                                if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                { RO = true; }
                                if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                { RO = true; }
                                group++;
                                decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                decimal totalp = totalamt + totalamt2;
                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                 ),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "2"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                 )));



                            });
                            #region RO
                            if (RO == true)
                            {
                                int countpaidnight1 = 0;
                                Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight1 = countpaidnight1 + 1;
                                    }
                                });
                                int countpaidnight2 = 0;
                                Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                {
                                    if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                    {
                                        countpaidnight2 = countpaidnight2 + 1;
                                    }
                                });

                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "1"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].occupPublishPrice / countpaidnight1)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                 ),

                                new XElement("Room",
                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                 new XAttribute("SuppliersID", "2"),
                                 new XAttribute("RoomSeq", "2"),
                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                 new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                 new XAttribute("MealPlanCode", ""),
                                 new XAttribute("MealPlanPrice", Convert.ToString("")),
                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].occupPublishPrice / countpaidnight2)),
                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                 new XAttribute("CancellationDate", ""),
                                 new XAttribute("CancellationAmount", ""),
                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                 new XElement("RequestID", Convert.ToString("")),
                                 new XElement("Offers", ""),
                                  new XElement("PromotionList",
                                 new XElement("Promotions", Convert.ToString(promotion))),
                                 new XElement("CancellationPolicy", ""),
                                 new XElement("Amenities", new XElement("Amenity", "")),
                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                 new XElement("Supplements",
                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                     ),
                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                 )));
                            }
                            #endregion
                            #endregion
                        }

                        //return str;
                        #endregion

                    });
                });
                return str;
            }
            #endregion
            #region Room Count 3
            if (totalroom == 3)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {

                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {

                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();
                //List<Tourico.Occupancy> roomList2 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 2).ToList();
                //List<Tourico.Occupancy> roomList3 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 3).ToList();
                #region Room 3
                Parallel.For(0, roomList1.Count(), m =>
                //for (int m = 0; m < roomList1.Count(); m++)
                {
                    Parallel.For(0, roomList2.Count(), n =>
                    //for (int n = 0; n < roomList2.Count(); n++)
                    {
                        Parallel.For(0, roomList3.Count(), o =>
                        //for (int o = 0; o < roomList3.Count(); o++)
                        {
                            // add room 1, 2, 3

                            #region room's group

                            int bb = roomlist.Occupancies[m].BoardBases.Count();
                            List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                            List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                            string promotion = string.Empty;
                            if (roomlist.Discount != null)
                            {
                                #region Discount (Tourico) Amount already subtracted
                                try
                                {
                                    XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                    XmlDocument doc3 = new XmlDocument();
                                    System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                    XmlWriter writer3 = XmlWriter.Create(sww3);
                                    xsSubmit3.Serialize(writer3, roomlist.Discount);
                                    var typxsd = XDocument.Parse(sww3.ToString());
                                    var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                    var distype = typxsd.Root.Attribute(disprefix + "type");
                                    if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                    {
                                        promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                    }
                                    else
                                    {
                                        promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                                #endregion
                            }

                            if (bb == 0)
                            {
                                group++;
                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice;
                                #region No Board Bases
                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                     ),

                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                     ),

                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "3"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                     )));
                                #endregion
                            }
                            else
                            {
                                bool RO = false;
                                #region Board Bases >0
                                Parallel.For(0, bb, j =>
                                //for (int j = 0; j < bb; j++)
                                {
                                    int countpaidnight1 = 0;
                                    Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight1 = countpaidnight1 + 1;
                                        }
                                    });
                                    int countpaidnight2 = 0;
                                    Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight2 = countpaidnight2 + 1;
                                        }
                                    });
                                    int countpaidnight3 = 0;
                                    Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight3 = countpaidnight3 + 1;
                                        }
                                    });
                                    if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                    { RO = true; }
                                    if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                    { RO = true; }
                                    if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                    { RO = true; }
                                    group++;
                                    decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                    decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                    decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                    decimal totalp = totalamt + totalamt2 + totalamt3;

                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "3"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                     )));
                                });
                                #endregion
                                #region RO
                                if (RO == true)
                                {
                                    int countpaidnight1 = 0;
                                    Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight1 = countpaidnight1 + 1;
                                        }
                                    });
                                    int countpaidnight2 = 0;
                                    Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight2 = countpaidnight2 + 1;
                                        }
                                    });
                                    int countpaidnight3 = 0;
                                    Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight3 = countpaidnight3 + 1;
                                        }
                                    });
                                    decimal totalamt = roomList1[m].occupPublishPrice;
                                    decimal totalamt2 = roomList2[n].occupPublishPrice;
                                    decimal totalamt3 = roomList3[o].occupPublishPrice;
                                    decimal totalp = totalamt + totalamt2 + totalamt3;

                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                                                        new XElement("Room",
                                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                                         new XAttribute("SuppliersID", "2"),
                                                                         new XAttribute("RoomSeq", "1"),
                                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                                         new XAttribute("MealPlanCode", ""),
                                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                                         new XAttribute("CancellationDate", ""),
                                                                         new XAttribute("CancellationAmount", ""),
                                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                                         new XElement("RequestID", Convert.ToString("")),
                                                                         new XElement("Offers", ""),
                                                                          new XElement("PromotionList",
                                                                         new XElement("Promotions", Convert.ToString(promotion))),
                                                                         new XElement("CancellationPolicy", ""),
                                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                         new XElement("Supplements",
                                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                             ),
                                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                                         ),

                                                                        new XElement("Room",
                                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                                         new XAttribute("SuppliersID", "2"),
                                                                         new XAttribute("RoomSeq", "2"),
                                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                                         new XAttribute("MealPlanCode", ""),
                                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                                         new XAttribute("CancellationDate", ""),
                                                                         new XAttribute("CancellationAmount", ""),
                                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                                         new XElement("RequestID", Convert.ToString("")),
                                                                         new XElement("Offers", ""),
                                                                          new XElement("PromotionList",
                                                                         new XElement("Promotions", Convert.ToString(promotion))),
                                                                         new XElement("CancellationPolicy", ""),
                                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                         new XElement("Supplements",
                                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                             ),
                                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                                         ),

                                                                        new XElement("Room",
                                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                                         new XAttribute("SuppliersID", "2"),
                                                                         new XAttribute("RoomSeq", "3"),
                                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                                                         new XAttribute("MealPlanCode", ""),
                                                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                                         new XAttribute("CancellationDate", ""),
                                                                         new XAttribute("CancellationAmount", ""),
                                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                                         new XElement("RequestID", Convert.ToString("")),
                                                                         new XElement("Offers", ""),
                                                                          new XElement("PromotionList",
                                                                         new XElement("Promotions", Convert.ToString(promotion))),
                                                                         new XElement("CancellationPolicy", ""),
                                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                                         new XElement("Supplements",
                                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                             ),
                                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                                         )));
                                }
                                #endregion
                            }

                            //return str;
                            #endregion
                        });
                    });
                });
                return str;
                #endregion

            }
            #endregion
            #region Room Count 4
            if (totalroom == 4)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {

                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {

                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {

                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 4
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                // add room 1, 2, 3, 4

                                #region room's group

                                int bb = roomlist.Occupancies[m].BoardBases.Count();
                                List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                string promotion = string.Empty;
                                if (roomlist.Discount != null)
                                {
                                    #region Discount (Tourico) Amount already subtracted
                                    try
                                    {
                                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                        XmlDocument doc3 = new XmlDocument();
                                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                        XmlWriter writer3 = XmlWriter.Create(sww3);
                                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                                        var typxsd = XDocument.Parse(sww3.ToString());
                                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                        var distype = typxsd.Root.Attribute(disprefix + "type");
                                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                        {
                                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                        }
                                        else
                                        {
                                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    #endregion
                                }

                                if (bb == 0)
                                {
                                    group++;
                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice;

                                    #region No Board Bases
                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    #endregion
                                }
                                else
                                {
                                    bool RO = false;
                                    #region Board Bases >0
                                    for (int j = 0; j < bb; j++)
                                    {
                                        int countpaidnight1 = 0;
                                        Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight1 = countpaidnight1 + 1;
                                            }
                                        });
                                        int countpaidnight2 = 0;
                                        Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight2 = countpaidnight2 + 1;
                                            }
                                        });
                                        int countpaidnight3 = 0;
                                        Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight3 = countpaidnight3 + 1;
                                            }
                                        });
                                        int countpaidnight4 = 0;
                                        Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight4 = countpaidnight4 + 1;
                                            }
                                        });
                                        if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                        { RO = true; }
                                        group++;
                                        decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                        decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                        decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                        decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4;

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4 / countpaidnight4)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    }
                                    #endregion
                                    #region RO
                                    if (RO == true)
                                    {
                                        int countpaidnight1 = 0;
                                        Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight1 = countpaidnight1 + 1;
                                            }
                                        });
                                        int countpaidnight2 = 0;
                                        Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight2 = countpaidnight2 + 1;
                                            }
                                        });
                                        int countpaidnight3 = 0;
                                        Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight3 = countpaidnight3 + 1;
                                            }
                                        });
                                        int countpaidnight4 = 0;
                                        Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight4 = countpaidnight4 + 1;
                                            }
                                        });

                                        group++;
                                        decimal totalamt = roomList1[m].occupPublishPrice;
                                        decimal totalamt2 = roomList2[n].occupPublishPrice;
                                        decimal totalamt3 = roomList3[o].occupPublishPrice;
                                        decimal totalamt4 = roomList4[p].occupPublishPrice;
                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4;

                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString("")),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4 / countpaidnight4)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                        }
                    }
                }
                return str;
                #endregion

            }
            #endregion

            #region Room Count 5
            if (totalroom == 5)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {

                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {

                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {

                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {

                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {

                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 5
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {

                                    #region room's group

                                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                    string promotion = string.Empty;
                                    if (roomlist.Discount != null)
                                    {
                                        #region Discount (Tourico) Amount already subtracted
                                        try
                                        {
                                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                            XmlDocument doc3 = new XmlDocument();
                                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                            XmlWriter writer3 = XmlWriter.Create(sww3);
                                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                                            var typxsd = XDocument.Parse(sww3.ToString());
                                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                            var distype = typxsd.Root.Attribute(disprefix + "type");
                                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                            {
                                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                            }
                                            else
                                            {
                                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                            }
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                        #endregion
                                    }

                                    if (bb == 0)
                                    {
                                        group++;
                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice;

                                        #region No Board Bases
                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "5"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        #endregion
                                    }
                                    else
                                    {
                                        bool RO = false;
                                        #region Board Bases >0
                                        for (int j = 0; j < bb; j++)
                                        {
                                            int countpaidnight1 = 0;
                                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight1 = countpaidnight1 + 1;
                                                }
                                            });
                                            int countpaidnight2 = 0;
                                            Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight2 = countpaidnight2 + 1;
                                                }
                                            });
                                            int countpaidnight3 = 0;
                                            Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight3 = countpaidnight3 + 1;
                                                }
                                            });
                                            int countpaidnight4 = 0;
                                            Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight4 = countpaidnight4 + 1;
                                                }
                                            });
                                            int countpaidnight5 = 0;
                                            Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList5[q].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight5 = countpaidnight5 + 1;
                                                }
                                            });
                                            if (roomList1[m].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList2[n].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList3[o].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList4[p].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            if (roomList5[q].BoardBases[j].bbPublishPrice > 0)
                                            { RO = true; }
                                            group++;
                                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                            decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                            decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                            decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                            decimal totalamt5 = roomList5[q].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4+totalamt5;

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4 / countpaidnight4)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "5"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt5 / countpaidnight5)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        }
                                        #endregion
                                        #region RO
                                        if (RO == true)
                                        {
                                            int countpaidnight1 = 0;
                                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight1 = countpaidnight1 + 1;
                                                }
                                            });
                                            int countpaidnight2 = 0;
                                            Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight2 = countpaidnight2 + 1;
                                                }
                                            });
                                            int countpaidnight3 = 0;
                                            Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight3 = countpaidnight3 + 1;
                                                }
                                            });
                                            int countpaidnight4 = 0;
                                            Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight4 = countpaidnight4 + 1;
                                                }
                                            });
                                            int countpaidnight5 = 0;
                                            Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList5[q].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight5 = countpaidnight5 + 1;
                                                }
                                            });

                                            group++;
                                            decimal totalamt = roomList1[m].occupPublishPrice;
                                            decimal totalamt2 = roomList2[n].occupPublishPrice;
                                            decimal totalamt3 = roomList3[o].occupPublishPrice;
                                            decimal totalamt4 = roomList4[p].occupPublishPrice;
                                            decimal totalamt5 = roomList5[q].occupPublishPrice;
                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5;

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt / countpaidnight1)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2 / countpaidnight2)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3 / countpaidnight3)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4 / countpaidnight4)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "5"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("Room Only")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString("")),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt5 / countpaidnight5)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                         new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        }
                                        #endregion
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion

            }
            #endregion

            
            #region Room Count 6
            if (totalroom == 6)
            {

                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 6
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        // add room 1, 2, 3, 4, 5, 6

                                        #region room's group

                                        int bb = roomlist.Occupancies[m].BoardBases.Count();
                                        List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                        List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                        string promotion = string.Empty;
                                        if (roomlist.Discount != null)
                                        {
                                            #region Discount (Tourico) Amount already subtracted
                                            try
                                            {
                                                XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                XmlDocument doc3 = new XmlDocument();
                                                System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                XmlWriter writer3 = XmlWriter.Create(sww3);
                                                xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                var typxsd = XDocument.Parse(sww3.ToString());
                                                var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                var distype = typxsd.Root.Attribute(disprefix + "type");
                                                if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                {
                                                    promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                }
                                                else
                                                {
                                                    promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                }
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                            #endregion
                                        }

                                        if (bb == 0)
                                        {
                                            group++;
                                            decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice;

                                            #region No Board Bases
                                            str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                 new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                 new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "6"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                 )));
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Board Bases >0
                                            for (int j = 0; j < bb; j++)
                                            {
                                                group++;
                                                decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6;

                                                str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "6"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                 )));
                                            }
                                            #endregion
                                        }
                                        #endregion

                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 7
            if (totalroom == 7)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                    });

                });


                #region Room 7
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            // add room 1, 2, 3, 4, 5, 6, 7

                                            #region room's group

                                            int bb = roomlist.Occupancies[m].BoardBases.Count();
                                            List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                            List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                            string promotion = string.Empty;
                                            if (roomlist.Discount != null)
                                            {
                                                #region Discount (Tourico) Amount already subtracted
                                                try
                                                {
                                                    XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                    XmlDocument doc3 = new XmlDocument();
                                                    System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                    XmlWriter writer3 = XmlWriter.Create(sww3);
                                                    xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                    var typxsd = XDocument.Parse(sww3.ToString());
                                                    var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                    var distype = typxsd.Root.Attribute(disprefix + "type");
                                                    if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                    {
                                                        promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                    }
                                                    else
                                                    {
                                                        promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                    }
                                                }
                                                catch (Exception ex)
                                                {

                                                }
                                                #endregion
                                            }

                                            if (bb == 0)
                                            {
                                                group++;
                                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice;

                                                #region No Board Bases
                                                str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "7"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), 0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                     )));
                                                #endregion
                                            }
                                            else
                                            {
                                                #region Board Bases >0
                                                for (int j = 0; j < bb; j++)
                                                {
                                                    group++;
                                                    decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                    decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7;

                                                    str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "7"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                     )));
                                                }
                                                #endregion
                                            }
                                            #endregion
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 8
            if (totalroom == 8)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 8)
                        {
                            roomList8.Add(roomlist.Occupancies[i]);
                        }
                    });

                });


                #region Room 8
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                // add room 1, 2, 3, 4, 5, 6, 7, 8

                                                #region room's group

                                                int bb = roomlist.Occupancies[m].BoardBases.Count();
                                                List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                                List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                                string promotion = string.Empty;
                                                if (roomlist.Discount != null)
                                                {
                                                    #region Discount (Tourico) Amount already subtracted
                                                    try
                                                    {
                                                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                        XmlDocument doc3 = new XmlDocument();
                                                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                        XmlWriter writer3 = XmlWriter.Create(sww3);
                                                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                        var typxsd = XDocument.Parse(sww3.ToString());
                                                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                        var distype = typxsd.Root.Attribute(disprefix + "type");
                                                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                        {
                                                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                        }
                                                        else
                                                        {
                                                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {

                                                    }
                                                    #endregion
                                                }

                                                if (bb == 0)
                                                {
                                                    group++;
                                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice;

                                                    #region No Board Bases
                                                    str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "1"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "2"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "3"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "4"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "5"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "6"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "7"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "8"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), 0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                         )));
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region Board Bases >0
                                                    for (int j = 0; j < bb; j++)
                                                    {
                                                        group++;
                                                        decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt8 = roomList8[t].occupPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice;
                                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8;

                                                        str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "1"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "2"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "3"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "4"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "5"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "6"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "7"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "8"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList8[t].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList8[t].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt8)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                         )));
                                                    }
                                                    #endregion
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 9
            if (totalroom == 9)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 8)
                        {
                            roomList8.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 9)
                        {
                            roomList9.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 9
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                for (int u = 0; u < roomList8.Count(); u++)
                                                {
                                                    // add room 1, 2, 3, 4, 5, 6, 7, 8, 9

                                                    #region room's group

                                                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                                                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                                    string promotion = string.Empty;
                                                    if (roomlist.Discount != null)
                                                    {
                                                        #region Discount (Tourico) Amount already subtracted
                                                        try
                                                        {
                                                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                            XmlDocument doc3 = new XmlDocument();
                                                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                            XmlWriter writer3 = XmlWriter.Create(sww3);
                                                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                            var typxsd = XDocument.Parse(sww3.ToString());
                                                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                            var distype = typxsd.Root.Attribute(disprefix + "type");
                                                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                            {
                                                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                            }
                                                            else
                                                            {
                                                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {

                                                        }
                                                        #endregion
                                                    }

                                                    if (bb == 0)
                                                    {
                                                        group++;
                                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice + roomList9[u].occupPublishPrice;

                                                        #region No Board Bases
                                                        str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "1"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "2"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "3"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "4"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "5"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "6"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "7"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "8"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "9"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList9[u].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList9[u].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList9[u].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(), 0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList9[u].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList9[u].Rooms[0].ChildNum))
                                                             )));
                                                        #endregion
                                                    }
                                                    else
                                                    {
                                                        #region Board Bases >0
                                                        for (int j = 0; j < bb; j++)
                                                        {
                                                            group++;
                                                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt8 = roomList8[t].occupPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt9 = roomList9[u].occupPublishPrice + roomList9[u].BoardBases[j].bbPublishPrice;
                                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8 + totalamt9;

                                                            str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "1"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "2"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "3"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "4"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "5"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "6"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "7"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "8"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList8[t].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList8[t].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt8)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "9"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList9[u].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList9[u].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList9[u].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList9[u].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].avrNightPublishPrice + roomList9[u].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt9)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList9[u].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(), roomList9[u].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList9[u].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList9[u].Rooms[0].ChildNum))
                                                             )));
                                                        }
                                                        #endregion
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            return str;
        }
        #endregion
        #region Tourico Room's Supplements
        private IEnumerable<XElement> GetRoomsSupplementTourico(List<Tourico.Supplement> supplements)
        {
            #region Tourico Supplements
            List<XElement> str = new List<XElement>();

            Parallel.For(0, supplements.Count(), i =>
            {

                Tourico.Supplement ss = supplements[i];

                XmlSerializer xsSubmit = new XmlSerializer(typeof(Tourico.Supplement));
                XmlDocument doc = new XmlDocument();
                System.IO.StringWriter sww = new System.IO.StringWriter();
                XmlWriter writer = XmlWriter.Create(sww);
                xsSubmit.Serialize(writer, ss);
                var xsd = XDocument.Parse(sww.ToString());
                var prefix = xsd.Root.GetNamespaceOfPrefix("xsi");
                var type = xsd.Root.Attribute(prefix + "type");
                if (Convert.ToString(type.Value) == "q1:PerPersonSupplement")
                {
                    var agegroup = XDocument.Parse(sww.ToString());

                    var prefixage = agegroup.Root.GetNamespaceOfPrefix("q1");

                    List<XElement> totalsupp = agegroup.Root.Descendants(prefixage + "SupplementAge").ToList();

                    str.Add(new XElement("Supplement",
                        new XAttribute("suppId", Convert.ToString(supplements[i].suppId)),
                        new XAttribute("suppName", Convert.ToString(supplements[i].suppName)),
                        new XAttribute("supptType", Convert.ToString(supplements[i].supptType)),
                        new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].suppIsMandatory)),
                        new XAttribute("suppChargeType", Convert.ToString(supplements[i].suppChargeType)),
                        new XAttribute("suppPrice", Convert.ToString(supplements[i].publishPrice)),
                        new XAttribute("suppType", Convert.ToString(type.Value)),
                        new XElement("SuppAgeGroup",
                            GetRoomsSupplementAgeGroupTourico(totalsupp)
                            )
                        ));
                }
                else
                {
                    str.Add(new XElement("Supplement",
                        new XAttribute("suppId", Convert.ToString(supplements[i].suppId)),
                        new XAttribute("suppName", Convert.ToString(supplements[i].suppName)),
                        new XAttribute("supptType", Convert.ToString(supplements[i].supptType)),
                        new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].suppIsMandatory)),
                        new XAttribute("suppChargeType", Convert.ToString(supplements[i].suppChargeType)),
                        new XAttribute("suppPrice", Convert.ToString(supplements[i].publishPrice)),
                        new XAttribute("suppType", Convert.ToString(type.Value)))
                     );
                }
            });

            return str;
            #endregion
        }
        #endregion
        #region Tourico Room Supplement's Age Group
        private IEnumerable<XElement> GetRoomsSupplementAgeGroupTourico(List<XElement> supplementagegroup)
        {
            #region Tourico Supplements Age Group
            List<XElement> str = new List<XElement>();

            Parallel.For(0, supplementagegroup.Count(), i =>
            {
                str.Add(new XElement("SupplementAge",
                       new XAttribute("suppFrom", Convert.ToString(supplementagegroup[i].Attribute("suppFrom").Value)),
                       new XAttribute("suppTo", Convert.ToString(supplementagegroup[i].Attribute("suppTo").Value)),
                       new XAttribute("suppQuantity", Convert.ToString(supplementagegroup[i].Attribute("suppQuantity").Value)),
                       new XAttribute("suppPrice", Convert.ToString(supplementagegroup[i].Attribute("suppPrice").Value)))
                );
            });
            return str;
            #endregion
        }
        #endregion
        #region Tourico Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupTourico(List<Tourico.Price> pricebreakups, decimal mealprice)
        {
            #region Tourico Room's Price Breakups
            List<XElement> str = new List<XElement>();
            int countpaidnight = 0;
            Parallel.For(0, pricebreakups.Count(), j =>
            {
                if (pricebreakups[j].valuePublish != 0)
                {
                    countpaidnight = countpaidnight + 1;
                }
            });
            decimal mealpricepernight = mealprice / countpaidnight;

            Parallel.For(0, pricebreakups.Count(), i =>
            {
                if (pricebreakups[i].valuePublish != 0)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].valuePublish + mealpricepernight)))
                    );
                }
                else
                {
                    str.Add(new XElement("Price",
                          new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                          new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].valuePublish)))
                   );
                }
            });
            return str;
            #endregion
        }
        #endregion
    }
}