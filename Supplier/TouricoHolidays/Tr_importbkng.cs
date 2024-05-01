using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.TouricoHolidays
{
    public class Tr_importbkng:IDisposable
    {
        public XElement importbooking_tourico(XElement req)
        {
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            try
            {      
                #region Credentials
                //XElement credential = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\Credential.xml"));
                string userlogin = string.Empty;
                string pwd = string.Empty;
                string version = string.Empty;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "2");
                userlogin = suppliercred.Descendants("username").FirstOrDefault().Value;
                pwd = suppliercred.Descendants("password").FirstOrDefault().Value;
                version = suppliercred.Descendants("version").FirstOrDefault().Value;
                #endregion

                #region Booking Tourico
                #region Header
                Tourico.AuthenticationHeader hd = new Tourico.AuthenticationHeader();
                hd.LoginName = userlogin;// "HOL916";
                hd.Password = pwd;// "111111";
                hd.Version = version;// "5";
                #endregion
                #region Import Booking Request Parameters
                Tourico.HotelFlowClient client = new Tourico.HotelFlowClient();
                Tourico.RGInfoRequest reqtourico = new Tourico.RGInfoRequest();
                reqtourico.RGId = Convert.ToInt32(req.Descendants("ConfirmationNumber").Single().Value);
                #endregion
                #region Call Import Booking Function
                Tourico.RGInfoResults result = client.GetRGInfo(hd, reqtourico);
                #endregion
                #region Hotel Info
                Tourico.ProductInfo hotelinfo = result.ResGroup.Reservations[0].ProductInfo;
                List<Tourico.Reservation> htreservationlist = result.ResGroup.Reservations.ToList();
                string status = string.Empty;
                status = result.ResGroup.Reservations[0].status;
                if (status == "Confirm")
                {
                    status = "Success";
                }
                else
                {
                    status = result.ResGroup.Reservations[0].status;
                }
                IEnumerable<XElement> request = req.Descendants("HotelImportBookingRequest").ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                #endregion
                #region XML OUT
                XElement bookingdoc = new XElement(
                  new XElement(soapenv + "Envelope",
                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            new XElement(soapenv + "Header",
                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                             new XElement("Authentication",
                                 new XElement("AgentID", AgentID),
                                 new XElement("UserName", username),
                                 new XElement("Password", password),
                                 new XElement("ServiceType", ServiceType),
                                 new XElement("ServiceVersion", ServiceVersion))),
                                    new XElement(soapenv + "Body",
                                        new XElement(request.Single()),
                                           new XElement("HotelImportBookingResponse",
                                               new XElement("Hotels",
                                                   new XElement("HotelID", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).hotelId)),
                                                   new XElement("HotelName", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).name)),
                                                   new XElement("FromDate", Convert.ToString(Convert.ToDateTime(result.ResGroup.Reservations[0].fromDate).ToString("dd/MM/yyyy"))),
                                                   new XElement("ToDate", Convert.ToString(Convert.ToDateTime(result.ResGroup.Reservations[0].toDate).ToString("dd/MM/yyyy"))),
                                                   new XElement("AdultPax", Convert.ToString(result.ResGroup.Reservations[0].numOfAdults)),
                                                   new XElement("ChildPax", Convert.ToString(result.ResGroup.Reservations[0].numOfChildren)),
                                                   new XElement("TotalPrice", Convert.ToString(result.ResGroup.totalPrice)),
                                                   new XElement("CurrencyID", Convert.ToString("")),
                                                   new XElement("CurrencyCode", Convert.ToString(result.ResGroup.currency)),
                                                   new XElement("MarketID", Convert.ToString("")),
                                                   new XElement("MarketName", Convert.ToString("")),
                                                   new XElement("HotelImgSmall", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).thumb)),
                                                   new XElement("HotelImgLarge", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).thumb)),
                                                   new XElement("MapLink", ""),
                                                   new XElement("TransID", Convert.ToString(req.Descendants("TransID").Single().Value)),
                                                   new XElement("ConfirmationNumber", Convert.ToString(result.ResGroup.rgId)),
                                                   new XElement("Status", Convert.ToString(status)),
                                                   new XElement("PassengersDetail",
                                                       new XElement("GuestDetails",
                                                          GetHotelRoomsInfoTourico(htreservationlist)
                                                                 )
                                                       )

                                                   )
                                  ))));
                return bookingdoc;
                #endregion
                #endregion
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "importbooking_tourico";
                ex1.PageName = "Tr_importbkng";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);

                IEnumerable<XElement> request = req.Descendants("HotelImportBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement bookingdoc = new XElement(
                  new XElement(soapenv + "Envelope",
                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            new XElement(soapenv + "Header",
                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                             new XElement("Authentication",
                                 new XElement("AgentID", AgentID),
                                 new XElement("UserName", username),
                                 new XElement("Password", password),
                                 new XElement("ServiceType", ServiceType),
                                 new XElement("ServiceVersion", ServiceVersion))),
                             new XElement(soapenv + "Body",
                                 new XElement(request.Single()),
                       new XElement("HotelImportBookingResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return bookingdoc;
                #endregion
            }
        }
        #region Tourico Hotel Room's Info
        private IEnumerable<XElement> GetHotelRoomsInfoTourico(List<Tourico.Reservation> roomlist)
        {
            #region Room's info (Tourico)

            List<XElement> str = new List<XElement>();
            for (int i = 0; i < roomlist.Count(); i++)
            {
                Tourico.Passenger passengers = ((Tourico.HotelInfo)(roomlist[i].ProductInfo)).Passenger;
                List<Tourico.Supplement> supplements = ((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.SelectedSupplements.ToList();

                if (((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase == null)
                {

                    str.Add(new XElement("Room",
                         new XAttribute("ID", Convert.ToString((((Tourico.HotelInfo)(roomlist[i].ProductInfo))).RoomExtraInfo.hotelRoomTypeId)),
                                                                      new XAttribute("RoomType", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).roomTypeCategory)),
                                                                      new XAttribute("ServiceID", Convert.ToString(roomlist[i].reservationId)),
                                                                      new XAttribute("MealPlanID", Convert.ToString("")),
                                                                      new XAttribute("MealPlanName", Convert.ToString("")),
                                                                      new XAttribute("MealPlanCode", Convert.ToString("")),
                                                                      new XAttribute("MealPlanPrice", Convert.ToString("")),
                                                                      new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                      new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].price)),
                         GetHotelPassengersInfoTourico(passengers),
                         new XElement("Supplements",
                             GetRoomsSupplementTourico(supplements)
                             )

                         ));
                }
                else
                {

                    str.Add(new XElement("Room",
                         new XAttribute("ID", Convert.ToString((((Tourico.HotelInfo)(roomlist[i].ProductInfo))).RoomExtraInfo.hotelRoomTypeId)),
                                                                      new XAttribute("RoomType", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).roomTypeCategory)),
                                                                      new XAttribute("ServiceID", Convert.ToString(roomlist[i].reservationId)),
                                                                      new XAttribute("MealPlanID", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase.bbId)),
                                                                      new XAttribute("MealPlanName", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase.bbName)),
                                                                      new XAttribute("MealPlanCode", Convert.ToString("")),
                                                                      new XAttribute("MealPlanPrice", Convert.ToString(((Tourico.HotelInfo)(roomlist[i].ProductInfo)).RoomExtraInfo.BoardBase.bbPublishPrice)),
                                                                      new XAttribute("PerNightRoomRate", Convert.ToString("")),
                                                                      new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].publishPrice)),
                         GetHotelPassengersInfoTourico(passengers),
                         new XElement("Supplements",
                             GetRoomsSupplementTourico(supplements)
                             )
                         ));
                }

            };
            return str;



            #endregion
        }
        #endregion
        #region Tourico Room's Supplements
        private IEnumerable<XElement> GetRoomsSupplementTourico(List<Tourico.Supplement> supplements)
        {
            #region Tourico Supplements
            List<XElement> str = new List<XElement>();

            //Parallel.For(0, supplements.Count(), i =>
            for (int i = 0; i < supplements.Count(); i++)
            {

                Tourico.Supplement ss = supplements[i];
                if (ss != null)
                {
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
                            new XAttribute("suppPrice", Convert.ToString(supplements[i].price)),
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
                            new XAttribute("suppPrice", Convert.ToString(supplements[i].price)),
                            new XAttribute("suppType", Convert.ToString(type.Value)))
                         );
                    }
                }
            }

            return str;
            #endregion
        }
        #endregion
        #region Tourico Room Supplement's Age Group
        private IEnumerable<XElement> GetRoomsSupplementAgeGroupTourico(List<XElement> supplementagegroup)
        {
            #region Tourico Supplements Age Group
            List<XElement> str = new List<XElement>();

            //Parallel.For(0, supplementagegroup.Count(), i =>
            for (int i = 0; i < supplementagegroup.Count();i++)
            {
                str.Add(new XElement("SupplementAge",
                       new XAttribute("suppFrom", Convert.ToString(supplementagegroup[i].Attribute("suppFrom").Value)),
                       new XAttribute("suppTo", Convert.ToString(supplementagegroup[i].Attribute("suppTo").Value)),
                       new XAttribute("suppQuantity", Convert.ToString(supplementagegroup[i].Attribute("suppQuantity").Value)),
                       new XAttribute("suppPrice", Convert.ToString(supplementagegroup[i].Attribute("suppPrice").Value)))
                );
            }
            return str;
            #endregion
        }
        #endregion
        #region Tourico Hotel Passenger's Info
        private IEnumerable<XElement> GetHotelPassengersInfoTourico(Tourico.Passenger htpassengersinfo)
        {
            #region Passenger's info (Tourico)


            List<XElement> pssngrlst = new List<XElement>();
            pssngrlst.Add(new XElement("RoomGuest",
                new XElement("GuestType", Convert.ToString("Adult")),
                new XElement("Title", ""),
                new XElement("FirstName", Convert.ToString(htpassengersinfo.firstName + " " + htpassengersinfo.middleName + " " + htpassengersinfo.lastName)),
                new XElement("MiddleName", ""),
                new XElement("LastName", ""),
                new XElement("IsLead", Convert.ToString("true")),
                new XElement("Age", Convert.ToString(""))
                ));
            return pssngrlst;
            #endregion
        }
        #endregion        
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}