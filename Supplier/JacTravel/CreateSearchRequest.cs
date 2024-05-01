using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class CreateSearchRequest
    {


        int totalroom = 1;
        public string HotelSearch(XElement myEle, string Usrname, string Password, XElement cityele, out int CountryId, out int value)  
        {

             value = 0;
             CountryId = 0;
            List<XElement> htllist = null;
            int totalPax = 0;

            int value1 = 0;
           int CountryId1 = 0;


            foreach (XElement item in myEle.Descendants("RoomPax"))
            {
                totalPax = totalPax + Convert.ToInt32(item.Element("Adult").Value);
                totalPax = totalPax + Convert.ToInt32(item.Element("Child").Value);

            }

            if (totalPax > 9)
            {
                return null;
            }
            totalroom = Convert.ToInt32(myEle.Descendants("RoomPax").Count());
            try
            {
              
                var SearchRequest = from htl in myEle.Descendants("searchRequest")
                                    join x in cityele.Descendants("d0")
                                    on htl.Element("CityID").Value equals x.Element("Serial").Value
                                    select new XElement("SearchRequest",
                                             new XElement("LoginDetails",
                                                 new XElement("Login", Usrname),
                                                 new XElement("Password", Password),
                                                 new XElement("Locale", ""),
                                                 new XElement("CurrencyID", "2"),
                                                 new XElement("AgentReference", "")),
                                                 new XElement("SearchDetails",
                                                 new XElement("ArrivalDate", JacHelper.MyDate(htl.Element("FromDate").Value)),
                                                 new XElement("Duration", JacHelper.GetDuration(htl.Element("ToDate").Value, htl.Element("FromDate").Value, out value1)),
                                                 new XElement("RegionID", GetRegionID(x.Elements("Supplier"), out CountryId1)),
                                                 new XElement("MealBasisID", "0"),
                                                 new XElement("MinStarRating", htl.Element("MinStarRating").Value),
                                                 new XElement("ContractSpecialOfferID", "0"),
                                                 new XElement("RoomRequests",
                                                 from room in htl.Element("Rooms").Elements("RoomPax")
                                                 select new XElement("RoomRequest",
                                                     new XElement("Adults", room.Element("Adult").Value),
                                                      new XElement("Children", JacHelper.GetChildCount(room.Elements("ChildAge"))),
                                                      new XElement("Infants", JacHelper.GetInfantsCount(room.Elements("ChildAge"))),
                                                      JacHelper.BindChild(room.Elements("ChildAge"))))));
                string request = SearchRequest.FirstOrDefault().ToString();
                value = value1;
                CountryId = CountryId1;
                return request;
            }
            catch
            {

                return null;
            }




            return null;
        }


        string GetRegionID(IEnumerable<XElement> ele, out int Countyid)
        {
            Countyid = 0;
            string RegionID = string.Empty;
            foreach (XElement item in ele)
            {
                if (Convert.ToInt16(item.Element("SupplierID").Value) == 8)
                {
                    Countyid = Convert.ToInt32(item.Element("localCountyID").Value);
                    RegionID = item.Element("SupplierCityID").Value;
                    return RegionID;
                }
            }
            return RegionID;
        }
    }
}