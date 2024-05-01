using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Supplier.JacTravel;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Jac_HtlBokng
    {
        #region BookingRequest
        public XElement BokngRequestXml(string UserName, string Password,string url, XElement myEle)
        {

            try
            {
                string Citymap = string.Empty;
                int value;
                IEnumerable<XElement> Bokngxml = from x in myEle.Descendants("HotelBookingRequest")
                                                 select new XElement("BookRequest",
                                                     new XElement("LoginDetails",
                                                         new XElement("Login", UserName),
                                                         new XElement("Password", Password),
                                                         new XElement("CurrencyID", "2")),
                                                         //new XElement("Currency"),
                                                         new XElement("BookingDetails",
                                                         new XElement("PropertyID", x.Descendants("Room").FirstOrDefault().Attribute("RoomTypeID").Value.Split(new char[]{'_'})[1]),
                                                         new XElement("PreBookingToken", x.Element("PassengersDetail").Elements("Room").FirstOrDefault().Element("RequestID").Value),
                                                          new XElement("ArrivalDate", JacHelper.MyDate(x.Element("FromDate").Value)),
                                                          new XElement("Duration", JacHelper.GetDuration(x.Element("ToDate").Value, x.Element("FromDate").Value, out value)),
                                                          BindLead(x.Element("PassengersDetail")),
                                                               new XElement("ContractSpecialOfferID", "0"),
                                                                new XElement("ContractArrangementID", "0"),
                                                               new XElement("TradeReference", x.Element("TransID").Value),
                                                               new XElement("Request", myEle.Descendants("SpecialRemarks").FirstOrDefault().Value),
                                                                new XElement("CCCardTypeID", "0"),
                                                               new XElement("CCIssueNumber", "0"),
                                                               new XElement("CCAmount", "0"),
                                                               new XElement("RoomBookings",
                                                                   BindRoom(x.Element("Rooms"), x.Element("PassengersDetail")))));

                if (Bokngxml.FirstOrDefault() != null)
                {
                    string Responce = Bokngxml.FirstOrDefault().ToString();
                    if (!string.IsNullOrEmpty(Responce))
                    {
                        int supid = Convert.ToInt32(myEle.Descendants("SuppliersID").FirstOrDefault().Value);
                        RequestClass requobj = new RequestClass();
                        Responce = requobj.HttpPostRequest(url, myEle, Responce, "Book",supid,5);
                        myEle = BokngResponce(Responce, myEle);
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

        List<XElement> BindRoom(XElement romdlt, XElement Pax)
        {
            List<XElement> leadgust = Pax.Elements("Room").ToList();
            List<XElement> rompax = romdlt.Elements("RoomPax").ToList();
            List<XElement> Rombok = new List<XElement>();
            for (int i = 0; i < leadgust.Count; i++)
            {
                string[] id = leadgust[i].Attribute("RoomTypeID").Value.Split(new char[] { '_' });
                XElement Room = new XElement("RoomBooking",
                      new XElement("PropertyRoomTypeID", id[0]),
                      id[0] == "0" ? new XElement("BookingToken", leadgust[i].Attribute("SessionID").Value) : null,
                      new XElement("MealBasisID", leadgust[i].Attribute("MealPlanID").Value),
                      new XElement("Adults", rompax[i].Element("Adult").Value),
                      new XElement("Children", JacHelper.GetChildCount(rompax[i].Elements("ChildAge"))),
                      new XElement("Infants", JacHelper.GetInfantsCount(rompax[i].Elements("ChildAge"))),
                      new XElement("Guests",
                      GetPax(leadgust[i])));
                Rombok.Add(Room);



            }
            return Rombok;
        }

        IEnumerable<XElement> GetPax(XElement Pax)
        {
            var paxcoll = from p in Pax.Elements("PaxInfo")
                          select new XElement("Guest",
                              new XElement("Type", p.Element("GuestType").Value == "Child" ? (Convert.ToInt16(p.Element("Age").Value) > 2 ? "Child" : "Infant") : "Adult"),
                              new XElement("Title", p.Element("Title").Value),
                              new XElement("FirstName", p.Element("FirstName").Value),
                              new XElement("LastName", p.Element("LastName").Value),
                              new XElement("Age", p.Element("Age").Value),
                              new XElement("Nationality", string.Empty));
            return paxcoll;
        }

        List<XElement> BindLead(XElement Pax)
        {
            List<XElement> leadgust = Pax.Elements("Room").ToList();
            List<XElement> lead = new List<XElement>();
            for (int i = 0; i < leadgust.Count; i++)
            {
                if (leadgust[i].Element("PaxInfo").Element("IsLead").Value == "true")
                {
                    lead.Add(new XElement("LeadGuestTitle", leadgust[i].Element("PaxInfo").Element("Title").Value));
                    lead.Add(new XElement("LeadGuestFirstName", leadgust[i].Element("PaxInfo").Element("FirstName").Value));
                    lead.Add(new XElement("LeadGuestLastName", leadgust[i].Element("PaxInfo").Element("LastName").Value));
                    lead.Add(new XElement("LeadGuestAddress1", ""));
                    lead.Add(new XElement("LeadGuestTownCity", ""));
                    lead.Add(new XElement("LeadGuestPostcode", ""));
                    lead.Add(new XElement("LeadGuestPhone", ""));
                    lead.Add(new XElement("LeadGuestEmail", ""));
                    break;
                }

            }
            return lead;

        }

        #endregion


        #region BookingResponse
        public XElement BokngResponce(string Responce, XElement myEle)
        {
         
            

           
            XElement bookingresp = XElement.Parse(Responce);
            string bookingstatus = string.Empty;
            string bookingstats = bookingresp.Descendants("Success").FirstOrDefault().Value;
            XElement Repoele = XElement.Parse(Responce);
            if(bookingstats=="true")
            {
                bookingstatus = "Success";
            }
            else
            {
                bookingstatus = "Failed";
                myEle.Descendants("HotelBookingRequest").FirstOrDefault().AddAfterSelf(new XElement("HotelBookingResponse", new XElement("ErrorTxt",
                                    Repoele.Descendants("Exception").Any() ? Repoele.Descendants("Exception").FirstOrDefault().Value : "Booking Failed")));
                return myEle;

            }
            string staticdata = string.Empty;
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
                              new XElement("VoucherRemark",""),
                               new XElement("TransID", myEle.Element("TransID") == null ? string.Empty : myEle.Element("TransID").Value),
                              new XElement("ConfirmationNumber", Repoele.Element("BookingReference") == null ? string.Empty : Repoele.Element("BookingReference").Value),
                              new XElement("Status", bookingstatus),
                              new XElement("PassengersDetail",
                                  new XElement("GuestDetails",getRomResponce(x.Element("PassengersDetail"), Repoele)))
                              ));


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


        IEnumerable<XElement> getRomResponce(XElement Paxdetail, XElement response)
        {
            //string MEal = string.Empty;
            //using (StreamReader sr = new StreamReader("MealBasis.xml"))
            //{
            //    MEal = sr.ReadToEnd();
            //}
            //XElement mealtype = XElement.Parse(MEal);


            var PassengersDetail = from x in Paxdetail.Descendants("Room")
                                   select new XElement("Room",
                                    new XAttribute("ID", x.Attribute("RoomTypeID").Value),
                                    new XAttribute("RoomType", x.Attribute("RoomType").Value),                                    
                                    new XAttribute("ServiceID", ""),
                                    new XAttribute("RefNo", response!=null? response.Descendants("SupplierReference").Any()? response.Descendants("SupplierReference").FirstOrDefault().Value:string.Empty: string.Empty),
                                     new XAttribute("MealPlanID", x.Attribute("MealPlanID").Value),
                                     new XAttribute("MealPlanName", ""),
                                    new XAttribute("MealPlanCode", ""),
                                    new XAttribute("MealPlanPrice", x.Attribute("MealPlanPrice").Value),
                                    new XAttribute("PerNightRoomRate", ""),
                                    new XAttribute("RoomStatus", "Confirm"),
                                    new XAttribute("TotalRoomRate", ""),
                                    GuestInfo(x.Elements("PaxInfo")),
                                   new XElement("Supplements", ""));
            return PassengersDetail;
        }

        XElement GuestInfo(IEnumerable<XElement> GuestInfo)
        {
           
            foreach (XElement item in GuestInfo)
            {
                if (item.Element("IsLead").Value == "true")
                {
                    XElement guest = new XElement("RoomGuest",
                           new XElement("GuestType", item.Element("GuestType").Value),
                           new XElement("Title", item.Element("Title").Value),
                           new XElement("FirstName", item.Element("FirstName").Value),
                           new XElement("MiddleName", item.Element("MiddleName").Value),
                           new XElement("LastName", item.Element("LastName").Value),
                           new XElement("IsLead", item.Element("IsLead").Value),
                            new XElement("Age", item.Element("Age").Value));
                    return guest;
                }
                
            }
            return null;
        }

        //IEnumerable<XElement> Suppliment(IEnumerable<XElement> GuestInfo)
        //{
        //    List<XElement> Suppcoll = new List<XElement>();
        //    foreach (XElement item in GuestInfo)
        //    {
               
        //            XElement suppli = new XElement("Supplement",
        //                    new XAttribute("suppId", item.Attribute("suppId").Value),
        //                    new XAttribute("suppName", item.Attribute("suppName").Value),
        //                    new XAttribute("supptType", item.Attribute("suppType").Value),
        //                    new XAttribute("suppIsMandatory", item.Attribute("suppIsMandatory").Value),
        //                    new XAttribute("suppChargeType", item.Attribute("suppChargeType").Value),
        //                    new XAttribute("suppPrice", item.Attribute("suppPrice").Value),
        //                    new XAttribute("suppType", item.Attribute("suppType").Value));
        //            Suppcoll.Add(suppli);
                   

        //    }


        //    return Suppcoll;
        //}

        #endregion


    }




}
