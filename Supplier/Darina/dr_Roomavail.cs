using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Darina;

namespace TravillioXMLOutService.Supplier.Darina
{
    public class dr_Roomavail
    {
        string dmc = "Darina";
        string customerid = string.Empty;
        string availDarina = string.Empty;
        string hoteldetailDarina = string.Empty;
        XElement reqTravillio;
        XElement doccity = null;
        string hotelid = string.Empty;
        XElement docccountry = null;
        #region darina availability
        public List<XElement> GetRoomAvail_DarinaOUT_merge(XElement req, XElement dococcupancy, XElement docmealplan, XElement doccurrency)
        {
            List<XElement> roomavailabilityresponse = new List<XElement>();
            try
            {
                #region changed
                string dmc = string.Empty;
                List<XElement> htlele = req.Descendants("GiataHotelList").Where(x => x.Attribute("GSupID").Value == "1").ToList();             
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
                        dmc = "Darina";
                    }
                    #region GetRoom
                    Darina_Htdetail drgetroom = new Darina_Htdetail();
                    darina_Getrooms getroom = new darina_Getrooms();
                    drgetroom.TransID = req.Descendants("TransID").FirstOrDefault().Value;
                    drgetroom.custID = customerid;
                    DataTable dt = getroom.Getrooms_Darina(drgetroom);
                    #endregion
                    if (dt != null)
                    {
                        availDarina = dt.Rows[0]["logresponseXML"].ToString();
                    }
                    List<XElement> getrom = getroomavaildarina(req, dococcupancy, docmealplan, doccurrency, dmc, htlid, availDarina);
                    roomavailabilityresponse.Add(getrom.Descendants("Rooms").FirstOrDefault());
                }
                #endregion
                return roomavailabilityresponse;
            }
            catch { return null; }
        }
        public List<XElement> getroomavaildarina(XElement req, XElement dococcupancy, XElement docmealplan, XElement doccurrency, string dmcout, string htlid, string availabDarina)
        {
            hotelid = htlid;
            dmc = dmcout;
            List<XElement> results = null;
            reqTravillio = req;
            try
            {
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "1");
                string currencyid = suppliercred.Descendants("currencyid").FirstOrDefault().Value;
                availDarina = availabDarina;
                if (availDarina != "")
                {
                    try
                    {
                        XElement doc = XElement.Parse(availDarina);
                        List<XElement> htlist = doc.Descendants("D").ToList();
                        IEnumerable<XElement> currencycode = doccurrency.Descendants("d0").Where(x => x.Descendants("CurrencyID").Single().Value == currencyid);
                        results = GetHotelList(htlist, docmealplan, dococcupancy, currencycode);
                        return results;
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
        #endregion
       
        #region Darina Holidays
        #region Darina's Hotel Listing
        private List<XElement> GetHotelList(List<XElement> htlist, XElement docmealplan, XElement dococcupancy, IEnumerable<XElement> currencycode)
        {
            #region Darina Hotel List
            List<XElement> hotellst = new List<XElement>();
            try
            {                
                List<XElement> hotellist = htlist.Descendants("Hotels").ToList().Where(x=>x.Descendants("HotelID").FirstOrDefault().Value==hotelid).ToList();
                try
                {
                    for(int i=0;i<1;i++)
                    {
                        List<XElement> roomlist = htlist.Descendants("AccRates").Where(x => x.Descendants("Hotel").Single().Value == hotelid).ToList();
                        string map = Convert.ToString(hotellist[i].Element("Maplink").Value);
                        hotellst.Add(new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(hotelid)),
                                               new XElement("HotelName", Convert.ToString("")),
                                               new XElement("PropertyTypeName", Convert.ToString("")),
                                               new XElement("CountryID", Convert.ToString("")),
                                               new XElement("CountryName", Convert.ToString("")),
                                               new XElement("CountryCode", Convert.ToString("")),
                                               new XElement("CityId", Convert.ToString("")),
                                               new XElement("CityCode", Convert.ToString("")),
                                               new XElement("CityName", Convert.ToString("")),
                                               new XElement("AreaId", Convert.ToString("")),
                                               new XElement("AreaName", Convert.ToString("")),
                                               new XElement("RequestID", Convert.ToString(""))
                                               , new XElement("Address", Convert.ToString("")),
                                               new XElement("Location", Convert.ToString("")),
                                               new XElement("Description", ""),
                                               new XElement("StarRating", Convert.ToString("")),
                                               new XElement("MinRate", Convert.ToString(roomlist.Descendants("RatePerStay").FirstOrDefault().Value))
                                               , new XElement("HotelImgSmall", Convert.ToString(hotellist[i].Element("SmallImgLink").Value)),
                                               new XElement("HotelImgLarge", Convert.ToString(hotellist[i].Element("LargeImgLink").Value)),
                                               new XElement("MapLink", map),
                                               new XElement("Longitude", ""),
                                               new XElement("Latitude", ""),
                                               new XElement("DMC", dmc),
                                               new XElement("SupplierID", "1"),
                                               new XElement("Currency", Convert.ToString(currencycode.Descendants("CurrencyName").Single().Value)),
                                               new XElement("Offers", "")
                                               , new XElement("Facilities","")
                                               , new XElement("Rooms",
                            GetHotelRooms(htlist, roomlist, docmealplan, dococcupancy, hotelid, currencycode.Descendants("CurrencyName").Single().Value)
                                                   )
                        ));
                    }
                }
                catch (Exception ex)
                {
                    return hotellst;
                }
            }
            catch (Exception ex)
            {
                return hotellst;
            }
            return hotellst;
            #endregion
        }
        #endregion
        #region Darina's Hotel Room's Listing
        public List<XElement> GetHotelRooms(List<XElement> htlist, List<XElement> roomlist, XElement docmealplan, XElement dococcupancy, string Hotelcode, string currency)
        {
            #region Darina Hotel's Room List
            List<XElement> str = new List<XElement>();
            List<XElement> roomtypes = htlist;
            DateTime fromDate = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            DateTime toDate = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            int nights = (int)(toDate - fromDate).TotalDays;
            for(int i=0;i<roomlist.Count();i++)
            {

                List<XElement> docmealplandet = docmealplan.Descendants("d0").Where(x => x.Descendants("MealPlanID").Single().Value == roomlist[i].Descendants("MealPlan").Single().Value).ToList();
                List<XElement> dococcupancydet = dococcupancy.Descendants("d0").Where(x => x.Descendants("OccupancyID").Single().Value == roomlist[i].Descendants("Occupancy").Single().Value).ToList();
                List<XElement> room = roomtypes.Descendants("RoomTypes").Where(x => x.Descendants("RoomTypeID").Single().Value == roomlist[i].Descendants("RoomType").Single().Value).ToList();
                string isavailableval = roomlist[i].Descendants("Availability").Single().Value;
                string isavailable = string.Empty;
                if (isavailableval == "1")
                {
                    isavailable = "true";
                }
                else
                {
                    isavailable = "false";
                }
                str.Add(new XElement("RoomTypes", new XAttribute("Index", i + 1), 
                    new XAttribute("TotalRate", roomlist[i].Descendants("RatePerStay").Single().Value), 
                    new XAttribute("HtlCode", Hotelcode), new XAttribute("CrncyCode", currency), 
                    new XAttribute("DMCType", dmc),
                    new XAttribute("CUID",customerid),
                    new XElement("Room",
                         new XAttribute("ID", Convert.ToString(roomlist[i].Descendants("RoomType").Single().Value)),
                         new XAttribute("SuppliersID", "1"),
                         new XAttribute("RoomSeq", "1"),
                         new XAttribute("SessionID", Convert.ToString(roomlist[i].Descendants("Serial").Single().Value)),
                         new XAttribute("RoomType", Convert.ToString(room.Descendants("RoomTypeName").Single().Value)),
                         new XAttribute("OccupancyID", Convert.ToString(roomlist[i].Descendants("Occupancy").Single().Value)),
                         new XAttribute("OccupancyName", Convert.ToString(dococcupancydet.Descendants("OccupancyName").Single().Value)),
                         new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Descendants("MealPlan").Single().Value)),
                         new XAttribute("MealPlanName", Convert.ToString(docmealplandet.Descendants("MealPlanName").Single().Value)),
                         new XAttribute("MealPlanCode", Convert.ToString(docmealplandet.Descendants("MealPlanCode").Single().Value)),
                         new XAttribute("MealPlanPrice", ""),
                         new XAttribute("PerNightRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value)),
                         new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerStay").Single().Value)),
                         new XAttribute("CancellationDate", ""),
                         new XAttribute("CancellationAmount", ""),
                         new XAttribute("isAvailable", isavailable),
                         new XElement("RequestID", Convert.ToString(roomlist[i].Descendants("RequestID").Single().Value)),
                         new XElement("Offers", ""),
                         new XElement("PromotionList",
                         new XElement("Promotions", Convert.ToString(roomlist[i].Descendants("Offers").Single().Value))),
                         new XElement("CancellationPolicy", ""),
                         new XElement("Amenities", new XElement("Amenity", "")),
                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                         new XElement("Supplements",null),
                             new XElement("PriceBreakups",
                                 GetRoomsPriceBreakupDarina(nights, Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value))),
                                 new XElement("AdultNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value)),
                                 new XElement("ChildNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value))
                         )));
            }
            return str;
            #endregion
        }
        #endregion       
        private List<XElement> GetRoomsPriceBreakupDarina(int nights, string pernightprice)
        {
            #region Darina Room's Price Breakups
            List<XElement> str = new List<XElement>();
            try
            {
                for(int i=0;i<nights;i++)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pernightprice)))
                    );
                }
                return str.OrderBy(x => (int)x.Attribute("Night")).ToList();
            }
            catch { return null; }
            #endregion
        }
        #endregion        
        #region Remove Namespaces
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            XElement xmlDocumentWithoutNs = removeAllNamespaces(xmlDocument);
            return xmlDocumentWithoutNs;
        }

        private static XElement removeAllNamespaces(XElement xmlDocument)
        {
            var stripped = new XElement(xmlDocument.Name.LocalName);
            foreach (var attribute in
                    xmlDocument.Attributes().Where(
                    attribute =>
                        !attribute.IsNamespaceDeclaration &&
                        String.IsNullOrEmpty(attribute.Name.NamespaceName)))
            {
                stripped.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
            }
            if (!xmlDocument.HasElements)
            {
                stripped.Value = xmlDocument.Value;
                return stripped;
            }
            stripped.Add(xmlDocument.Elements().Select(
                el =>
                    RemoveAllNamespaces(el)));
            return stripped;
        }
        #endregion
    }
}