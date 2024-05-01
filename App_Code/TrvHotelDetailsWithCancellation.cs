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
using TravillioXMLOutService.Supplier.DotW;
using TravillioXMLOutService.Supplier.JacTravel;
using TravillioXMLOutService.Supplier.Extranet;
using TravillioXMLOutService.Supplier.RTS;
using TravillioXMLOutService.Models.Darina;
using TravillioXMLOutService.Supplier.Miki;
using TravillioXMLOutService.Supplier.Restel;
using TravillioXMLOutService.Supplier.Darina;
using TravillioXMLOutService.Supplier.TouricoHolidays;
using TravillioXMLOutService.Supplier.Juniper;
using TravillioXMLOutService.Supplier.Godou;
using TravillioXMLOutService.Supplier.SalTours;
using TravillioXMLOutService.Supplier.SunHotels;
using TravillioXMLOutService.Supplier.Hoojoozat;
using TravillioXMLOutService.Supplier.TravelGate;
using TravillioXMLOutService.Supplier.TBOHolidays;
using TravillioXMLOutService.Supplier.VOT;
using TravillioXMLOutService.Supplier.EBookingCenter;
using TravillioXMLOutService.Supplier.XMLOUTAPI.CXLPolicy;

namespace TravillioXMLOutService.App_Code
{
    public class TrvHotelDetailsWithCancellation:IDisposable
    {
        #region XML OUT for Hotel Cancellation Policies (Travayoo)
        public XElement HotelDetailWithCancellations(XElement req)
        {
            #region XML OUT
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            //string supplierid = req.Descendants("SupplierID").Single().Value;
            string supplierid = req.Descendants("Room").Attributes("SuppliersID").FirstOrDefault().Value;
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                #region XML OUT
                try
                {
                    #region Supplier Credentials
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                    #endregion
                    IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest");
                    #region Darina supplier
                    if (supplierid == "1")
                    {
                        #region XML OUT from Hotel Extranet Supplier
                        dr_CXLpolicy drobj = new dr_CXLpolicy();
                        XElement hotelcxlpolicy = drobj.getCXLpolicy(req);
                        return hotelcxlpolicy;
                        #endregion                       
                    }
                    #endregion
                    #region Tourico supplier
                    if (supplierid == "2")
                    {
                        #region Tourico
                        Tr_CXLPolicy touricoobj = new Tr_CXLPolicy();
                        XElement hotelcxlpolicy = touricoobj.GetCXLPolicyTourico(req);
                        return hotelcxlpolicy;
                        #endregion   
                    }
                    #endregion
                    #region Extranet
                    if (supplierid == "3")
                    {
                        #region XML OUT from Hotel Extranet Supplier
                        ExtCXLPolicy htlextobj = new ExtCXLPolicy();
                        XElement hotelcxlpolicy = htlextobj.GetCXLPolicyExtranet(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region HotelBeds
                    if (supplierid == "4")
                    {
                        #region HotelBeds
                        CancellationPolicyHotelBeds hbreq = new CancellationPolicyHotelBeds();

                        XElement hotelcxlpolicy = hbreq.HotelCXLPolicyHotelBeds(req);

                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region DOTW
                    if (supplierid == "5")
                    {
                        #region DOTW
                        DotwService dotwObj = new DotwService(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement hotelcxlpolicy = dotwObj.CxlReq(req); 
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region HotelsPro
                    if (supplierid == "6")
                    {
                        #region HotelsPro
                        HotelsProCXLPolicy hbreq = new HotelsProCXLPolicy();
                        XElement hotelcxlpolicy = hbreq.HotelCXLPolicyHotelsPro(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Travco
                    if (supplierid == "7")
                    {
                        #region Travco
                        Travco travcoObj = new Travco(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement hotelcxlpolicy = travcoObj.HotelCancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Jac Travel
                    if (supplierid == "8")
                    {
                        #region Jac
                        Jac_HtlCXlPolicy jacObj = new Jac_HtlCXlPolicy();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "8");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelcxlpolicy = jacObj.PreBokngRequest(req,Login, Password, url,"JacTravel");
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region RTS
                    if (supplierid == "9")
                    {
                        foreach (var item in req.Descendants("hotelcancelpolicyrequest"))
                        {
                            string GuestCountyCode = item.Element("PaxNationality_CountryCode").Value != string.Empty ? item.Element("PaxNationality_CountryCode").Value.ToUpper() : string.Empty;
                            RTS_CXlPolicy obj = new RTS_CXlPolicy();
                            XElement hotelcxlpolicy = obj.GetCancelPolicy(req, GuestCountyCode);
                            return hotelcxlpolicy;
                        }
                        return null;  
                    }
                    #endregion
                    #region MIKI
                    if (supplierid == "11")
                    {
                        #region MIKI
                        MikiInternal mk = new MikiInternal();
                        XElement hotelcxlpolicy = mk.cancelltaionPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Restel
                    if (supplierid == "13")
                    {
                        #region Restel
                        RestelServices res = new RestelServices();
                        XElement hotelcxlpolicy = res.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region JuniperW2M
                    if (supplierid == "16")
                    {
                        #region JuniperW2M
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(16, customerid);
                        XElement hotelcxlpolicy = rs.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region EgyptExpress
                    if (supplierid == "17")
                    {
                        #region EgyptExpress
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(17, customerid);
                        XElement hotelcxlpolicy = rs.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Sal Tours
                    if (supplierid == "19")
                    {
                        SalServices sser = new SalServices();
                        XElement CancellationPolicy = sser.CancellationPolicy(req);
                        return CancellationPolicy;
                    }
                    #endregion
                    #region TBO
                    if (supplierid == "21")
                    {
                        TBOServices tbs = new TBOServices();
                        XElement CancellationPolicy = tbs.CancelPolicy(req);
                        return CancellationPolicy;
                    }
                    #endregion
                    #region LOH
                    if (supplierid == "23")
                    {
                        #region LOH
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(23, customerid);
                        XElement hotelcxlpolicy = rs.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Gadou
                    if (supplierid == "31")
                    {
                        //GodouServices gds = new GodouServices();
                        //XElement CancellationPolicy = gds.CancellationPolicy(req);
                        //return CancellationPolicy;
                        #region Gadou
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(31, customerid);
                        XElement hotelcxlpolicy = rs.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region LCI
                    if (supplierid == "35")
                    {
                        #region LCI
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(35, customerid);
                        XElement hotelcxlpolicy = rs.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region SunHotels
                    if (supplierid == "36")
                    {
                        #region SunHotels
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        SunHotelsResponse objRs = new SunHotelsResponse(36, customerid);
                        XElement hotelcxlpolicy = objRs.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Total Stay
                    if (supplierid == "37")
                    {
                        #region Sun Hotels
                        Jac_HtlCXlPolicy jacObj = new Jac_HtlCXlPolicy();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "37");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelcxlpolicy = jacObj.PreBokngRequest(req, Login, Password, url, "TotalStay");
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region SmyRooms
                    if (supplierid == "39")
                    {
                        TGServices tgs = new TGServices(39, req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement CancellationPolicy = tgs.CancellationPolicy(req);
                        return CancellationPolicy;
                    }
                    #endregion
                    #region AlphaTours
                    if (supplierid == "41")
                    {
                        #region AlphaTours
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(41, customerid);
                        XElement hotelcxlpolicy = rs.cancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Hoojoozat
                    if (supplierid == "45")
                    {
                        #region AlphaTours
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        HoojService rs = new HoojService(customerid);
                        XElement hotelcxlpolicy = rs.CancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region VOT
                    if (supplierid == "46")
                    {
                        #region VOT
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        VOTService rs = new VOTService(customerid);
                        XElement hotelcxlpolicy = rs.CancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Ebookingcenter
                    if (supplierid == "47")
                    {
                        #region Ebookingcenter
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        EBookingService rs = new EBookingService(customerid);
                        XElement hotelcxlpolicy = rs.CancellationPolicy(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region Booking Express
                    if (supplierid == "501")
                    {
                        #region Booking Express
                        xmloutCXLPolicy rs = new xmloutCXLPolicy();
                        XElement hotelcxlpolicy = rs.cxlpolicy_beOUT(req);
                        return hotelcxlpolicy;
                        #endregion
                    }
                    #endregion
                    #region No Supplier Found
                    else
                    {
                        #region Server Not Responding
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        XElement searchdoc = new XElement(
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
                               new XElement("HotelDetailwithcancellationResponse",
                                   new XElement("ErrorTxt", "Server is not responding")
                                           )
                                       )
                      ));
                        return searchdoc;
                        #endregion
                    }
                    #endregion
                }
                catch(Exception ex)
                {
                    #region Exception
                    IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    XElement searchdoc = new XElement(
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
                           new XElement("HotelDetailwithcancellationResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    return searchdoc;
                    #endregion
                }
                #endregion
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement searchdoc = new XElement(
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
                       new XElement("HotelDetailwithcancellationResponse",
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }
            #endregion
        }
        #endregion
        #region Hotel Room's Details from Tourico Holidays
        private IEnumerable<XElement> GetHotelRoomsTourico(List<Tourico.CancelPenaltyType1> htlist, string roomid, string checkin, string checkout,string CurrencyName,string PerNightRoomRate,string TotalRoomRate)
        {
            #region Hotel Room's Details from Tourico Holidays
            List<XElement> htrm = new List<XElement>();
            for (int i = 0; i < htlist.Count(); i++)
            {
                htrm.Add(new XElement("Room",
                    new XAttribute("ID", Convert.ToString(roomid)),
                    new XAttribute("RoomType", Convert.ToString("")),
                    new XAttribute("PerNightRoomRate", Convert.ToString(PerNightRoomRate)),
                    new XAttribute("TotalRoomRate", Convert.ToString(TotalRoomRate)),
                    new XAttribute("LastCancellationDate", ""),
                    new XElement("CancellationPolicies",
                         GetRoomCancellationPolicyTourico(htlist, checkin, checkout,CurrencyName,PerNightRoomRate,TotalRoomRate))
                    ));
                break;
            };
            return htrm;
            #endregion
        }
        #endregion
        #region Room's Cancellation Policies from Tourico Holidays
        private IEnumerable<XElement> GetRoomCancellationPolicyTourico(List<Tourico.CancelPenaltyType1> cancellationpolicy, string checkin, string checkout, string CurrencyName, string PerNightRoomRate, string TotalRoomRate)
        {
            #region Room's Cancellation Policies from Tourico Holidays
            List<XElement> htrm = new List<XElement>();
            List<Tourico.CancelPenaltyType1> roomlist = cancellationpolicy;
            TimeSpan t = Convert.ToDateTime(checkout) - Convert.ToDateTime(checkin);
            double NrOfnights = t.TotalDays;
            //PerNightRoomRate = Convert.ToString(Convert.ToDecimal(TotalRoomRate) / Convert.ToDecimal(NrOfnights));
            #region Last Cancellation Date
            try
            {
                string currencycode1 = string.Empty;
                string lastcxl = string.Empty;
                for (int i = 0; i < roomlist.Count(); i++)
                {

                    DateTime checkindt = Convert.ToDateTime(checkin);
                    DateTime checkoutdt = Convert.ToDateTime(checkout);
                    string timein = Convert.ToString(roomlist[i].Deadline.OffsetTimeUnit);
                    double totalhour = Convert.ToDouble(roomlist[i].Deadline.OffsetUnitMultiplier);
                    string arrival = Convert.ToString(roomlist[i].Deadline.OffsetDropTime);
                    TimeSpan days = TimeSpan.FromHours(totalhour);
                    DateTime canceldate = checkindt.AddDays(-days.TotalDays);
                    string amount = Convert.ToString(roomlist[i].AmountPercent.Amount);
                    string basistype = Convert.ToString(roomlist[i].AmountPercent.BasisType);
                    if (roomlist[i].AmountPercent.CurrencyCode != null)
                    {
                        currencycode1 = Convert.ToString(roomlist[i].AmountPercent.CurrencyCode);
                    }
                    else
                    {
                        currencycode1 = CurrencyName;
                    }
                    int numberofngt = Convert.ToInt32(roomlist[i].AmountPercent.NmbrOfNights);
                    int percent = Convert.ToInt32(roomlist[i].AmountPercent.Percent);
                    string amountapplicabl = string.Empty;
                    string bybefore = string.Empty;

                   
                    if (timein == "Hour" && arrival == "AfterBooking" && numberofngt > 0 && basistype == "Nights")
                    {
                        canceldate = System.DateTime.Now;
                    }
                    else if (timein == "Hour" && arrival == "AfterBooking" && percent > 0 && basistype == "FullStay")
                    {
                        canceldate = System.DateTime.Now;
                    }
                    else if (timein == "Hour" && arrival == "AfterBooking" && amount != null && basistype == "FullStay")
                    {
                        canceldate = System.DateTime.Now;
                    }
                    
                    else
                    {
                        
                    }

                    if (i == 0)
                    {
                        lastcxl = canceldate.ToString("dd/MM/yyyy");
                    }
                    if (DateTime.ParseExact(lastcxl, "dd/MM/yyyy", null) <= Convert.ToDateTime(canceldate))
                    {

                    }
                    else
                    {
                        lastcxl = canceldate.ToString("dd/MM/yyyy");
                    }
                }
                lastcxl = Convert.ToString(DateTime.ParseExact(lastcxl, "dd/MM/yyyy", null).AddDays(-1).ToString("dd/MM/yyyy"));
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on before " + lastcxl + "  will apply " + currencycode1 + " 0 Cancellation fee"
                        , new XAttribute("LastCancellationDate", Convert.ToString(lastcxl))
                        , new XAttribute("ApplicableAmount", "0")
                        , new XAttribute("NoShowPolicy", "0")));
            }
            catch(Exception ex)
            {

            }
            #endregion

            #region CXL Policy
            for (int i = 0; i < roomlist.Count(); i++)
            {
                string NoShowPolicy = "0";
                
                string currencycode = string.Empty;
                DateTime checkindt = Convert.ToDateTime(checkin);
                DateTime checkoutdt = Convert.ToDateTime(checkout);
                string timein = Convert.ToString(roomlist[i].Deadline.OffsetTimeUnit);
                double totalhour = Convert.ToDouble(roomlist[i].Deadline.OffsetUnitMultiplier);
                string arrival = Convert.ToString(roomlist[i].Deadline.OffsetDropTime);
                TimeSpan days = TimeSpan.FromHours(totalhour);
                DateTime canceldate = checkindt.AddDays(-days.TotalDays);
                string amount = Convert.ToString(roomlist[i].AmountPercent.Amount);
                string basistype = Convert.ToString(roomlist[i].AmountPercent.BasisType);
                if (roomlist[i].AmountPercent.CurrencyCode != null)
                {
                    currencycode = Convert.ToString(roomlist[i].AmountPercent.CurrencyCode);
                }
                else
                {
                    currencycode = CurrencyName;
                }
                int numberofngt = Convert.ToInt32(roomlist[i].AmountPercent.NmbrOfNights);
                int percent = Convert.ToInt32(roomlist[i].AmountPercent.Percent);
                string amountapplicabl = string.Empty;
                string bybefore = string.Empty;
                if ((roomlist[i].Deadline.OffsetUnitMultiplier == 0) && (arrival == "BeforeArrival"))
                {
                    NoShowPolicy = "1";
                }

                if (timein == "Hour" && arrival == "BeforeArrival" && numberofngt >0 && basistype == "Nights")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(PerNightRoomRate) * numberofngt);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && numberofngt >0 && basistype == "Nights")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(PerNightRoomRate) * numberofngt);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && percent >0 && basistype == "FullStay")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && percent >0 && basistype == "FullStay")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "AfterBooking" && numberofngt >0 && basistype == "Nights")
                {
                    canceldate = System.DateTime.Now;
                    bybefore = "after";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(PerNightRoomRate)*numberofngt);
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && percent >0 && basistype == "FullStay")
                {
                    bybefore = "before";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "AfterBooking" && percent >0 && basistype == "FullStay")
                {
                    canceldate = System.DateTime.Now;
                    bybefore = "after";
                    amountapplicabl = Convert.ToString(Convert.ToDecimal(TotalRoomRate) * percent / 100);
                }
                else if (timein == "Hour" && arrival == "AfterBooking" && amount != null && basistype == "FullStay")
                {
                    canceldate = System.DateTime.Now;
                    bybefore = "after";
                    amountapplicabl = amount;
                }
                else if (timein == "Hour" && arrival == "BeforeArrival" && amount != null && basistype == "FullStay")
                {
                    
                    bybefore = "before";
                    amountapplicabl = amount;
                }
                else
                {
                    bybefore = "by or before";
                    amountapplicabl = TotalRoomRate;
                }
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + canceldate.ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + amountapplicabl + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(canceldate.ToString("dd/MM/yyyy")))
                    , new XAttribute("ApplicableAmount", amountapplicabl)
                    , new XAttribute("NoShowPolicy", NoShowPolicy)));
            };
            return htrm;

            #endregion
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
        #region Merge CXL Policy
        public XElement MergCxlPolicy(List<XElement> rooms, string currencycode)
        {
            List<XElement> policyList = new List<XElement>();



            IEnumerable<XElement> dateLst = rooms.Descendants("CancellationPolicy").
                Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0")).
                GroupBy(r => new { r.Attribute("LastCancellationDate").Value }).Select(y => y.First()).
                OrderBy(p => p.Attribute("LastCancellationDate").Value);
            if (dateLst.Count() > 0)
            {

                foreach (var item in dateLst)
                {
                    string date = item.Attribute("LastCancellationDate").Value;

                    decimal datePrice = 0.0m;
                    foreach (var rm in rooms)
                    {
                        var prItem = rm.Descendants("CancellationPolicy").
                            Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value == date)).
                            FirstOrDefault();
                        if (prItem != null)
                        {
                            var price = prItem.Attribute("ApplicableAmount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            DateTime oDate = date.HotelsDate();

                            var lastItem = rm.Descendants("CancellationPolicy").
                                Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value.HotelsDate() < oDate));

                            if (lastItem.Count() > 0)
                            {
                                var lastDate = lastItem.Max(y => y.Attribute("LastCancellationDate").Value);
                                var lastprice = rm.Descendants("CancellationPolicy").
                        Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0" && pq.Attribute("LastCancellationDate").Value == lastDate)).
                        FirstOrDefault().Attribute("ApplicableAmount").Value;
                                datePrice += Convert.ToDecimal(lastprice);
                            }
                        }
                    }
                    XElement pItem = new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", date), new XAttribute("ApplicableAmount", datePrice), new XAttribute("NoShowPolicy", "0"), "");
                    policyList.Add(pItem);

                }



                policyList = policyList.GroupBy(x => new { x.Attribute("LastCancellationDate").Value.HotelsDate().Date }).
                    Select(y => new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", y.Key.Date.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", y.Max(p => Convert.ToDecimal(p.Attribute("ApplicableAmount").Value))), new XAttribute("NoShowPolicy", "0"), "")).ToList();

            }
            var lastCxlDate = rooms.Descendants("CancellationPolicy").Where(pq => (pq.Attribute("ApplicableAmount").Value != "0.00" && pq.Attribute("NoShowPolicy").Value == "0")).Min(y => y.Attribute("LastCancellationDate").Value.HotelsDate().Date);
            policyList.Insert(0, new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", lastCxlDate.AddDays(-1).ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", "0.00"), new XAttribute("NoShowPolicy", "0"), "Cancellation done on before " + lastCxlDate.ToString("dd/MM/yyyy") + "  will apply " + currencycode + " " + 0 + "  Cancellation fee"));


            var NoShow = rooms.Descendants("CancellationPolicy").Where(pq => pq.Attribute("NoShowPolicy").Value == "1").GroupBy(r => new { r.Attribute("NoShowPolicy").Value }).Select(x => new { date = x.Min(y => y.Attribute("LastCancellationDate").Value.HotelsDate().Date), price = x.Sum(y => Convert.ToDecimal(y.Attribute("ApplicableAmount").Value)) });

            if (NoShow.Count() > 0)
            {
                var showItem = NoShow.FirstOrDefault();
                policyList.Add(new XElement("CancellationPolicy", new XAttribute("LastCancellationDate", showItem.date.ToString("dd/MM/yyyy")), new XAttribute("ApplicableAmount", showItem.price), new XAttribute("NoShowPolicy", "1"), ""));

            }
            XElement cxlItem = new XElement("CancellationPolicies", policyList);
            return cxlItem;
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