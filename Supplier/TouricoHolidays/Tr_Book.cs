using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.TouricoHolidays
{
    public class Tr_Book
    {
        #region Confirm Booking Tourico
        public XElement HotelBooking_Tourico(XElement req)
        {
            #region Credentials
            string userlogin = string.Empty;
            string pwd = string.Empty;
            string version = string.Empty;
            string customeremail = string.Empty;
            //userlogin = credential.Descendants("username").FirstOrDefault().Value;
            //pwd = credential.Descendants("password").FirstOrDefault().Value;
            //version = credential.Descendants("version").FirstOrDefault().Value;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "2");
            userlogin = suppliercred.Descendants("username").FirstOrDefault().Value;
            pwd = suppliercred.Descendants("password").FirstOrDefault().Value;
            version = suppliercred.Descendants("version").FirstOrDefault().Value;
            customeremail = suppliercred.Descendants("customeremail").FirstOrDefault().Value;
            #endregion
            #region Booking Tourico
            #region Header
            Tourico.AuthenticationHeader hd = new Tourico.AuthenticationHeader();
            hd.LoginName = userlogin;// "HOL916";
            hd.Password = pwd;// "111111";
            hd.Version = version;// "5";
            Tourico.Culture di3 = (Tourico.Culture)1;
            hd.Culture = di3;
            #endregion
            #region Booking Request Parameters
            Tourico.HotelFlowClient client = new Tourico.HotelFlowClient();
            Tourico.BookV3Request reqtourico = new Tourico.BookV3Request();
            reqtourico.RecordLocatorId = 0;
            reqtourico.HotelId = Convert.ToInt32(req.Descendants("HotelID").SingleOrDefault().Value);
            reqtourico.HotelRoomTypeId = Convert.ToInt32(req.Descendants("Room").FirstOrDefault().Attributes("RoomTypeID").SingleOrDefault().Value);
            reqtourico.CheckIn = DateTime.ParseExact(req.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            reqtourico.CheckOut = DateTime.ParseExact(req.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            
            #region Room Info
            List<XElement> trum = req.Descendants("RoomPax").ToList();
            List<XElement> tpass = req.Descendants("Room").ToList();
            Tourico.RoomReserveInfo[] roominfo = new Tourico.RoomReserveInfo[trum.Count()];
            for (int i = 0; i < trum.Count(); i++)
            {
                List<XElement> supplements = tpass[i].Descendants("Supplement").ToList();
                List<XElement> suppage = tpass[i].Descendants("SupplementAge").ToList();
                Tourico.SuppAges[] suppages = new Tourico.SuppAges[suppage.Count()];
                Tourico.SupplementInfo[] selsupplements = new Tourico.SupplementInfo[supplements.Count()];
                #region Supplements
                if (supplements.Count() > 0)
                {
                    for (int r = 0; r < supplements.Count(); r++)
                    {
                        string supptype = supplements[r].Attribute("suppType").Value;
                        if (supptype == "PerPersonSupplement")
                        {

                            int[] suplQuat = new int[suppage.Count()];

                            #region Add Supplement Age Group
                            for (int k = 0; k < suppage.Count(); k++)
                            {
                                if (suppage.Count() == 1)
                                {
                                    suppages[k] = new Tourico.SuppAges
                                    {
                                        suppFrom = Convert.ToInt32(suppage[k].Attribute("suppFrom").Value),
                                        suppTo = Convert.ToInt32(suppage[k].Attribute("suppTo").Value),
                                        suppQuantity = Convert.ToInt32(suppage[k].Attribute("suppQuantity").Value),
                                        suppPrice = Convert.ToDecimal(suppage[k].Attribute("suppPrice").Value)
                                    };
                                }
                                else
                                {
                                    int suptotaladult = Convert.ToInt32(trum[i].Element("Adult").Value);
                                    int suptotalchild = Convert.ToInt32(trum[i].Element("Child").Value);

                                    for (int kk = 0; kk < suptotaladult; kk++)
                                    {
                                        int cage = 19;

                                        if (cage >= Convert.ToInt32(suppage[k].Attribute("suppFrom").Value) && cage <= Convert.ToInt32(suppage[k].Attribute("suppTo").Value))
                                        {
                                            ++suplQuat[k];
                                        }
                                    }
                                    for (int kk = 0; kk < suptotalchild; kk++)
                                    {
                                        int cage = Convert.ToInt32(trum[i].Element("ChildAge").Value);

                                        if (cage >= Convert.ToInt32(suppage[k].Attribute("suppFrom").Value) && cage <= Convert.ToInt32(suppage[k].Attribute("suppTo").Value))
                                        {
                                            ++suplQuat[k];
                                        }
                                    }


                                    suppages[k] = new Tourico.SuppAges
                                    {
                                        suppFrom = Convert.ToInt32(suppage[k].Attribute("suppFrom").Value),
                                        suppTo = Convert.ToInt32(suppage[k].Attribute("suppTo").Value),
                                        suppQuantity = Convert.ToInt32(suplQuat[k]),
                                        suppPrice = Convert.ToDecimal(suppage[k].Attribute("suppPrice").Value)
                                    };
                                }
                            }
                            #endregion
                        }
                        #region Add Supplements
                        //for (int m = 0; m < supplements.Count(); m++)
                        {
                            selsupplements[r] = new Tourico.SupplementInfo
                            {
                                suppId = Convert.ToInt32(supplements[r].Attribute("suppId").Value),
                                supTotalPrice = Convert.ToDecimal(supplements[r].Attribute("suppPrice").Value),
                                suppType = Convert.ToInt32(supplements[r].Attribute("supptType").Value),
                                SupAgeGroup = suppages
                            };
                        }
                    }
                        #endregion
                }
                #endregion
                #region Child Age
                int childcount = Convert.ToInt32(trum[i].Element("Child").Value);
                Tourico.ChildAge[] chdage = new Tourico.ChildAge[childcount];
                List<XElement> chldcnt = trum[i].Descendants("ChildAge").ToList();
                for (int k = 0; k < childcount; k++)
                {
                    chdage[k] = new Tourico.ChildAge
                    {
                        age = Convert.ToInt32(chldcnt[k].Value)
                    };
                }
                #endregion
                #region Add Room Info
                #region No Board Base
                if ((Convert.ToString(tpass[i].Attribute("MealPlanID").Value) == "") && (Convert.ToString(tpass[i].Attribute("MealPlanPrice").Value) == ""))
                {
                    roominfo[i] = new Tourico.RoomReserveInfo
                    {
                        //RoomId = Convert.ToInt32(req.Descendants("Room").FirstOrDefault().Attributes("SessionID").SingleOrDefault().Value),
                        RoomId = Convert.ToInt32(i + 1),
                        ContactPassenger = new Tourico.ContactPassenger
                        {
                            FirstName = tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("Title").SingleOrDefault().Value + " " + tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("FirstName").SingleOrDefault().Value,
                            MiddleName = tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("MiddleName").SingleOrDefault().Value,
                            LastName = tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("LastName").SingleOrDefault().Value,
                            HomePhone = "",
                            MobilePhone = ""
                        },
                        SelectedSupplements = selsupplements,
                        Bedding = Convert.ToString(tpass[i].Attribute("OccupancyID").Value),
                        Note = req.Descendants("SpecialRemarks").Single().Value,
                        AdultNum = Convert.ToInt32(trum[i].Element("Adult").Value),
                        ChildNum = Convert.ToInt32(trum[i].Element("Child").Value),
                        ChildAges = chdage
                    };
                }
                #endregion
                #region Board Base (Added)
                else
                {
                    roominfo[i] = new Tourico.RoomReserveInfo
                    {
                        //RoomId = Convert.ToInt32(req.Descendants("Room").FirstOrDefault().Attributes("SessionID").SingleOrDefault().Value),
                        RoomId = Convert.ToInt32(i + 1),
                        ContactPassenger = new Tourico.ContactPassenger
                        {
                            FirstName = tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("Title").SingleOrDefault().Value + " " + tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("FirstName").SingleOrDefault().Value,
                            MiddleName = tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("MiddleName").SingleOrDefault().Value,
                            LastName = tpass[i].Descendants("PaxInfo").FirstOrDefault().Descendants("LastName").SingleOrDefault().Value,
                            HomePhone = "",
                            MobilePhone = ""
                        },
                        SelectedBoardBase = new Tourico.SelectedBoardBase
                        {
                            Id = Convert.ToInt32(tpass[i].Attribute("MealPlanID").Value),
                            Price = Convert.ToDecimal(tpass[i].Attribute("MealPlanPrice").Value)
                        },
                        SelectedSupplements = selsupplements,
                        Bedding = Convert.ToString(tpass[i].Attribute("OccupancyID").Value),
                        Note = req.Descendants("SpecialRemarks").Single().Value,
                        AdultNum = Convert.ToInt32(trum[i].Element("Adult").Value),
                        ChildNum = Convert.ToInt32(trum[i].Element("Child").Value),
                        ChildAges = chdage
                    };
                }
                #endregion
                #endregion
            }
            #endregion
            reqtourico.RoomsInfo = roominfo;
            reqtourico.PaymentType = (Tourico.PaymentTypes)1;
            reqtourico.AgentRefNumber = req.Descendants("TransID").Single().Value;
            reqtourico.ContactInfo = "";
            reqtourico.RequestedPrice = (decimal)(Convert.ToDecimal(req.Descendants("TotalAmount").Single().Value));
            //reqtourico.RequestedPrice = 100;
            decimal deltamt = Convert.ToDecimal(req.Descendants("TotalAmount").Single().Value) * Convert.ToDecimal(0.25) / 100;
            reqtourico.DeltaPrice = (decimal)deltamt;
            reqtourico.Currency = req.Descendants("CurrencyCode").Single().Value;
            reqtourico.IsOnlyAvailable = true;
            reqtourico.ConfirmationEmail = customeremail;
            reqtourico.ConfirmationLogo = "";
            #region Features (eg. pass OriginalImageSize into name and true in value to get original images)
            Tourico.Feature[] fch = new Tourico.Feature[1];
            for (int i = 0; i < 1; i++)
            {
                fch[i] = new Tourico.Feature { name = "", value = "" };
            }
            #endregion
            #endregion
            #region supplier Request Log Prior to booking
            string touricologreqpr = "";
            try
            {
                XmlSerializer serializer1 = new XmlSerializer(typeof(Tourico.BookV3Request));

                using (StringWriter writer = new StringWriter())
                {
                    serializer1.Serialize(writer, reqtourico);
                    touricologreqpr = writer.ToString();
                }
            }
            catch { touricologreqpr = req.ToString(); }
            try
            {
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(req.Descendants("CustomerID").FirstOrDefault().Value);
                log.TrackNumber = req.Descendants("TransactionID").FirstOrDefault().Value;
                log.LogTypeID = 5;
                log.LogType = "Book";
                log.SupplierID = 2;
                log.logrequestXML = touricologreqpr.ToString();
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBooking_Tourico";
                ex1.PageName = "Tr_Book";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
            }
            #endregion
            #region Call Booking Function
            Tourico.RGInfoResults result = client.BookHotelV3(hd, reqtourico, fch);
            #endregion
            #region Log Save
            try
            {
                #region supplier Request Log
                //string touricologreq = "";
                //try
                //{
                //    XmlSerializer serializer1 = new XmlSerializer(typeof(Tourico.BookV3Request));

                //    using (StringWriter writer = new StringWriter())
                //    {
                //        serializer1.Serialize(writer, reqtourico);
                //        touricologreq = writer.ToString();
                //    }
                //}
                //catch { touricologreq = req.ToString(); }
                #endregion

                XmlSerializer serializer = new XmlSerializer(typeof(Tourico.RGInfoResults));
                string touricologres = "";
                using (StringWriter writer = new StringWriter())
                {
                    serializer.Serialize(writer, result);
                    touricologres = writer.ToString();
                }
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("TransactionID").FirstOrDefault().Value;
                    log.LogTypeID = 5;
                    log.LogType = "Book";
                    log.SupplierID = 2;
                    log.logrequestXML = touricologreqpr.ToString();
                    log.logresponseXML = touricologres.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelBooking_Tourico";
                    ex1.PageName = "Tr_Book";
                    ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("TransactionID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
            }
            catch (Exception ee)
            {
                CustomException ex1 = new CustomException(ee);
                ex1.MethodName = "HotelBooking_Tourico";
                ex1.PageName = "Tr_Book";
                ex1.CustomerID = req.Descendants("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransactionID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
            }

            #endregion

            #region Hotel Info and Booking Status
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

            int totalreservation = result.ResGroup.Reservations.Count();
            int totaladult = 0;
            int totalchild = 0;
            decimal totalamount = 0;
            for (int i = 0; i < totalreservation; i++)
            {
                totaladult = totaladult + result.ResGroup.Reservations[i].numOfAdults;
                totalchild = totalchild + result.ResGroup.Reservations[i].numOfChildren;
                totalamount = totalamount + result.ResGroup.Reservations[i].publishPrice;
            }

            IEnumerable<XElement> request = req.Descendants("HotelBookingRequest").ToList();
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            Tourico.Passenger passengers = ((Tourico.HotelInfo)(hotelinfo)).Passenger;
            #region Board Base
            string bbid = string.Empty;
            string bbname = string.Empty;
            if ((((Tourico.HotelInfo)(hotelinfo))).RoomExtraInfo.BoardBase != null)
            {
                bbid = Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).RoomExtraInfo.BoardBase.bbId);
                bbname = ((Tourico.HotelInfo)(hotelinfo)).RoomExtraInfo.BoardBase.bbName;
            }
            #endregion
            #endregion
            #region XML OUT
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;

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
                                       new XElement("HotelBookingResponse",
                                           new XElement("Hotels",
                                               new XElement("HotelID", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).hotelId)),
                                               new XElement("HotelName", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).name)),
                                               new XElement("FromDate", Convert.ToString(Convert.ToDateTime(result.ResGroup.Reservations[0].fromDate).ToString("dd/MM/yyyy"))),
                                               new XElement("ToDate", Convert.ToString(Convert.ToDateTime(result.ResGroup.Reservations[0].toDate).ToString("dd/MM/yyyy"))),
                                               new XElement("AdultPax", Convert.ToString(totaladult)),
                                               new XElement("ChildPax", Convert.ToString(totalchild)),
                                               new XElement("TotalPrice", Convert.ToString(totalamount)),
                                               new XElement("CurrencyID", Convert.ToString("")),
                                               new XElement("CurrencyCode", Convert.ToString(result.ResGroup.Reservations[0].currency)),
                                               new XElement("MarketID", Convert.ToString("")),
                                               new XElement("MarketName", Convert.ToString("")),
                                               new XElement("HotelImgSmall", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).thumb)),
                                               new XElement("HotelImgLarge", Convert.ToString(((Tourico.HotelInfo)(hotelinfo)).thumb)),
                                               new XElement("MapLink", ""),
                                               new XElement("VoucherRemark", ""),
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
        #endregion
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
                                                                      new XAttribute("RoomStatus", Convert.ToString(roomlist[i].status)),
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
                                                                      new XAttribute("RoomStatus", Convert.ToString(roomlist[i].status)),
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

            Parallel.For(0, supplements.Count(), i =>
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
    }
}