using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Jac_HtlCXlPolicy
    {
       




        #region CreatePreBookingRequest
        // Create Supplier PreBooking Request
       public XElement PreBokngRequest(XElement myEle, string user, string Password,string url, string suppliername)
        {
            try
            {
                string Responce = string.Empty;

                int value = 0;
                IEnumerable<XElement> CountryList = from htl in myEle.Descendants("hotelcancelpolicyrequest")
                                                    select new XElement("PreBookRequest",
                                                          new XElement("LoginDetails", new XElement("Login", user),
                                                              new XElement("Password", Password),
                                                              new XElement("Locale", ""),
                                                               new XElement("CurrencyID", "2"),
                                                               new XElement("AgentReference", "")),
                                                          new XElement("BookingDetails",
                                                          new XElement("PropertyID", myEle.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value),
                                                          new XElement("ArrivalDate", JacHelper.MyDate(htl.Element("FromDate").Value)),
                                                          new XElement("Duration", JacHelper.GetDuration(htl.Element("ToDate").Value, htl.Element("FromDate").Value, out value)),
                                                          new XElement("RoomBookings", CreateRoom(htl.Element("Rooms")))));


                if (CountryList.FirstOrDefault() != null)
                {
                   string Request = CountryList.FirstOrDefault().ToString();
                    RequestClass requobj = new RequestClass();
                    int supid = Convert.ToInt32(myEle.Descendants("SupplierID").FirstOrDefault().Value);
                    Responce = requobj.HttpPostRequest(url, myEle, Request, "CXLPolicy",supid,3);                  

                   
                    if (Responce != "An error occurred: The operation has timed out")
                    {
                        myEle = PreBokngResponce(Responce, myEle, value,suppliername);
                    }
                    else
                    {
                        XElement rep = new XElement("HotelPreBookingResponse",
                                                            new XElement("Status", false),
                                                             new XElement("ErrorTxt", "An error occurred: The operation has timed out"));
                        IEnumerable<XElement> Descoll = myEle.Descendants("HotelPreBookingRequest");
                        foreach (XElement item in Descoll)
                        {
                            item.AddAfterSelf(rep);
                        }
                    }

                }
                else if (CountryList.FirstOrDefault() == null)
                {
                    XElement rep = new XElement("HotelPreBookingResponse",
                                                           new XElement("Status", false),
                                                            new XElement("ErrorTxt", "XMl Is not Correct Format"));
                    IEnumerable<XElement> Descoll = myEle.Descendants("HotelPreBookingRequest");
                    foreach (XElement item in Descoll)
                    {
                        item.AddAfterSelf(rep);
                    }
                }




                return myEle;
            }
            catch
            {

                return null;
            }
            return null;
        }

        // Add Room for Supplier PreBooking Request
        IEnumerable<XElement> CreateRoom(XElement lst)
        {
            List<XElement> roomlst = new List<XElement>();
            List<XElement> Paxlst = lst.Elements("RoomPax").ToList();
            List<XElement> romlst = lst.Element("RoomTypes").Elements("Room").ToList();
            for (int i = 0; i < romlst.Count; i++)
            {
                XElement tombok = new XElement("RoomBooking",
                                         new XElement("PropertyRoomTypeID", romlst[i].Attribute("ID").Value),
                                        romlst[i].Attribute("SessionID").Value != string.Empty ? new XElement("BookingToken", romlst[i].Attribute("SessionID").Value) : null,
                                          new XElement("MealBasisID", romlst[i].Attribute("MealPlanID").Value),
                                          new XElement("Adults", Paxlst[i].Element("Adult").Value),
                                          new XElement("Children", JacHelper.GetChildCount(Paxlst[i].Elements("ChildAge"))),
                                          JacHelper.BindChild(Paxlst[i].Elements("ChildAge")),
                                           new XElement("Infants", JacHelper.GetInfantsCount(Paxlst[i].Elements("ChildAge"))));

                roomlst.Add(tombok);
            }




            return roomlst;
        }
        #endregion




        #region PreBokngResponce
        // create for client PreBooking Response
        XElement PreBokngResponce(string Responce, XElement myEle, int duration,string suppliername)
        {




            XElement PreRepo = XElement.Parse(Responce);
            string status = (String)PreRepo.Element("ReturnStatus").Element("Success");
            if (status == "true")
            {

                IEnumerable<XElement> CountryList = from htl in myEle.Descendants("hotelcancelpolicyrequest")
                                                    select new XElement("HotelDetailwithcancellationResponse",
                                                        new XElement("NewPrice", Convert.ToDecimal(htl.Element("TotalRoomRate").Value) < Convert.ToDecimal(PreRepo.Element("TotalPrice").Value) ? PreRepo.Element("TotalPrice").Value : string.Empty),

                                                         new XElement("Hotels",
                                                          new XElement("Hotel",
                                                           new XElement("HotelID", htl.Element("HotelID").Value),
                                                            new XElement("HotelName", htl.Element("HotelName").Value),
                                                             new XElement("Status", true),
                                                              new XElement("TermCondition", ""),
                                                               new XElement("HotelImgLarge", string.Empty),
                                                               new XElement("HotelImgSmall", string.Empty),
                                                                new XElement("MapLink", ""),
                                                                 new XElement("DMC", suppliername),
                                                                 new XElement("Currency", htl.Element("CurrencyName").Value),
                                                                 new XElement("Offers", ""),
                                                                 new XElement("Rooms",
                                                                   new XElement("RoomTypes",
                                                                      new XAttribute("Index", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("Index").Value),
                                                                        new XAttribute("TotalRate", htl.Descendants("RoomTypes").FirstOrDefault().Attribute("TotalRate").Value),
                                                                        CreateRoomTag(htl.Element("Rooms"), PreRepo.Element("PreBookingToken"), duration, PreRepo.Element("Cancellations"), htl.Element("CurrencyName").Value))))));




                IEnumerable<XElement> Descoll = myEle.Descendants("hotelcancelpolicyrequest");
                foreach (XElement item in Descoll)
                {
                    item.AddAfterSelf(CountryList);
                }

            }
            else if (status == "false")
            {
                XElement CountryList = new XElement("HotelPreBookingResponse",
                                                          new XElement("Status", false),
                                                           new XElement("ErrorTxt", PreRepo.Element("ReturnStatus").Element("Exception").Value));

            }
            return myEle;
        }



        // Add Room for client PreBooking Response
        public List<XElement> CreateRoomTag(XElement rmlst, XElement Prebooktoken, int duration,XElement cxl,string Currname)
        {
            List<XElement> Noofpax = rmlst.Elements("RoomPax").ToList();
            List<XElement> NoofRom = rmlst.Element("RoomTypes").Elements("Room").ToList();
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
                                                    new XAttribute("RoomType", NoofRom[i].Attribute("RoomType").Value),
                                                   new XAttribute("MealPlanPrice", ""),
                                                   new XAttribute("OccupancyName", ""),
                                                     new XAttribute("OccupancyID", ""),
                                                      new XAttribute("PerNightRoomRate", NoofRom[i].Attribute("PerNightRoomRate").Value),
                                                         new XAttribute("CancellationDate", ""),
                                                           new XAttribute("CancellationAmount", ""),
                                                           new XAttribute("isavailable", ""),
                                                   new XElement("RequestID", Prebooktoken.Value),
                                                   new XElement("Offers", ""),
                                                    new XElement("PromotionList", new XElement("Promotions", string.Empty)),
                                                   new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                    new XElement("Supplements", ""),
                                                   new XElement("Amenities", ""),

                                                   new XElement("PriceBreakups", JacHelper.PreiceBkp(NoofRom[i].Attribute("TotalRoomRate").Value, duration)),
                                                   new XElement("AdultNum", Noofpax[i].Element("Adult").Value),
                                                   new XElement("ChildNum", Noofpax[i].Element("Child").Value),
                                                   new XElement("CancellationPolicies", BindCXlPolicy(cxl,Currname)));
                rooms.Add(roomdeatil);

            }




            return rooms;
        }
        #endregion



        IEnumerable<XElement> BindCXlPolicy(XElement CxlColl, string CurrName)
        {



            foreach (var item in CxlColl.Descendants("Cancellation"))
            {
                decimal price = Convert.ToDecimal(item.Element("Penalty").Value);

                if (price > 0)
                {
                    DateTime dt = Convert.ToDateTime(item.Element("StartDate").Value.Replace("T00:00:00", ""));
                    // DateTime date = DateTime.ParseExact(JacHelper.DtFormat(item.Element("StartDate").Value.Replace("T00:00:00", "")), "dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    DateTime newdate = dt.AddDays(-1);
                    XElement ele = new XElement("Cancellation", new XElement("StartDate", newdate.Date.ToString("yyyy-MM-dd")),
                                              new XElement("EndDate", newdate.Date.ToString("yyyy-MM-dd")),
                              new XElement("Penalty", 0.00));
                    CxlColl.Add(ele);


                }
                break;
            }






            var data = from cxl in CxlColl.Elements("Cancellation")
                       select new XElement("CancellationPolicy",
                              new XAttribute("LastCancellationDate", Convert.ToDecimal(cxl.Element("Penalty").Value) == 0 ? JacHelper.DtFormat(cxl.Element("EndDate").Value.Replace("T00:00:00", "")) : JacHelper.DtFormat(cxl.Element("StartDate").Value.Replace("T00:00:00", ""))),
                                  new XAttribute("ApplicableAmount", cxl.Element("Penalty").Value),
                                  new XAttribute("NoShowPolicy", "0"),
                                  "Cancellation done on after " + (Convert.ToDecimal(cxl.Element("Penalty").Value) == 0 ? JacHelper.DtFormat(cxl.Element("StartDate").Value.Replace("T00:00:00", "")) : JacHelper.DtFormat(cxl.Element("StartDate").Value.Replace("T00:00:00", ""))) + " will apply " + "USD" + " " + cxl.Element("Penalty").Value + " Cancellation fee");



            return data;


        }
     
    }
}