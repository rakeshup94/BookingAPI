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
using System.Data;
using TravillioXMLOutService.Supplier.DotW;
using TravillioXMLOutService.Supplier.JacTravel;
using TravillioXMLOutService.Supplier.Extranet;
using TravillioXMLOutService.Models.Darina;
using TravillioXMLOutService.Supplier.Miki;
using TravillioXMLOutService.Supplier.Restel;
using TravillioXMLOutService.Supplier.Juniper;
using TravillioXMLOutService.Supplier.Godou;
using TravillioXMLOutService.Supplier.SalTours;
using TravillioXMLOutService.Supplier.SunHotels;
using TravillioXMLOutService.Supplier.Hoojoozat;
using TravillioXMLOutService.Supplier.TravelGate;
using TravillioXMLOutService.Supplier.TBOHolidays;
using TravillioXMLOutService.Supplier.VOT;
using TravillioXMLOutService.Supplier.EBookingCenter;

namespace TravillioXMLOutService.App_Code
{
    public class TrvHotelDetails:IDisposable
    {
        public XElement reqTravillio;
        string hotelproperty = string.Empty;
        string hoteldescription = string.Empty;
        string Phone = string.Empty;
        string Fax = string.Empty;
        string checkintime = string.Empty;
        string checkouttime = string.Empty;
        IEnumerable<XElement> image = null;
        IEnumerable<XElement> facility = null;
        IEnumerable<XElement> darinaimage = null;
        IEnumerable<XElement> darinafacility = null;
        #region XML OUT for Hotel Details (Travayoo)
        public XElement CreateHotelDescriptionDetail(XElement req)
        {
            #region XML OUT for Hotel Details
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                #region XML OUT
                try
                {
                    reqTravillio = req;
                    #region Supplier Credentials
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                    #endregion
                    string flag = "htdescription";
                    XNamespace res = "http://travelcontrol.softexsw.us/";
                    XElement dochtd;
                    XElement responsehotels = null;
                    IEnumerable<XElement> htdetail;
                    #region Darina Holidays
                    if (supplierid == "1")
                    {                        
                        //DarinaCredentials _credential = new DarinaCredentials();
                        //var _url = _credential.APIURL;
                        //var _action = "http://travelcontrol.softexsw.us/GetBasicData_PropertyDescription";
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(reqTravillio.Descendants("CustomerID").FirstOrDefault().Value, "1");
                        var _url = suppliercred.Descendants("APIURL").FirstOrDefault().Value;
                        var _action = suppliercred.Descendants("propertydetailActionURL").FirstOrDefault().Value;
                        hotelproperty = CallWebService(req, _url, _action, flag);
                        if(hotelproperty!="" || hotelproperty!="")
                        {
                            dochtd = XElement.Parse(hotelproperty);
                            htdetail = dochtd.Descendants(res + "GetBasicData_PropertyDescriptionResponse").ToList();
                            hoteldescription = htdetail.Descendants(res + "GetBasicData_PropertyDescriptionResult").Single().Value;
                            Darina_Htdetail htldarinastat = new Darina_Htdetail();
                            Darina_HtlDetailStatic htldarinastaticdet = new Darina_HtlDetailStatic();
                            htldarinastat.TransID = req.Descendants("TransID").FirstOrDefault().Value;
                            DataTable dt = htldarinastaticdet.GetHotelDetail_Darina(htldarinastat);
                            if (dt != null)
                            {
                                try
                                {
                                    string staticdata = dt.Rows[0][0].ToString();
                                    XElement sdata = XElement.Parse(staticdata);
                                    List<XElement> drnimglst = sdata.Descendants("Imgs").Where(x => x.Descendants("HotelID").FirstOrDefault().Value == req.Descendants("HotelID").FirstOrDefault().Value).ToList();
                                    List<XElement> imglst = drnimglst.Descendants("LargePic").ToList();
                                    darinaimage = hotelimagesDarina(imglst);
                                    List<XElement> drnfctlst = sdata.Descendants("HFac").Where(x => x.Descendants("HotelID").FirstOrDefault().Value == req.Descendants("HotelID").FirstOrDefault().Value).ToList();
                                    List<XElement> faclst = drnfctlst.Descendants("Facility").ToList();
                                    darinafacility = hotelfacilitiesDarina(faclst);                                    
                                }
                                catch { }
                            }
                        }                        
                    }
                    #endregion
                    #region Tourico Holidays
                    if (supplierid == "2")
                    {                        
                        Tourico_HtDetail htltouricostat = new Tourico_HtDetail();
                        Tourico_HtlDetailStatic htltouricostaticdet = new Tourico_HtlDetailStatic();
                        htltouricostat.HotelID = req.Descendants("HotelID").FirstOrDefault().Value;
                        DataTable dt = htltouricostaticdet.GetHotelDetail_Tourico(htltouricostat);
                        if (dt != null)
                        {
                            try
                            {
                                string staticdata = dt.Rows[0]["HotelInfoRespone"].ToString();
                                XElement sdata = XElement.Parse(staticdata);

                                List<XElement> imglst = sdata.Descendants("Image").ToList();
                                image = hotelimagesTourico(imglst);

                                List<XElement> faclst = sdata.Descendants("Amenity").ToList();
                                facility = hotelfacilitiesTourico(faclst);

                                hoteldescription = sdata.Descendants("FreeTextShortDescription").Attributes("desc").FirstOrDefault().Value;
                                Phone = sdata.Descendants("Hotel").Attributes("hotelPhone").FirstOrDefault().Value;
                                Fax = sdata.Descendants("Hotel").Attributes("hotelFax").FirstOrDefault().Value;
                                checkintime = sdata.Descendants("Hotel").Attributes("checkInTime").FirstOrDefault().Value;
                                checkouttime = sdata.Descendants("Hotel").Attributes("checkOutTime").FirstOrDefault().Value;
                            }
                            catch { }
                        }
                    }
                    #endregion
                    #region Extranet
                    if (supplierid == "3")
                    {                        
                        #region Extranet Request/Response
                        ExtHotelDetail extreq = new ExtHotelDetail();
                        XElement hoteldetails = extreq.HotelDetailExtranet(req);
                        return hoteldetails;
                        #endregion
                    }
                    #endregion
                    #region HotelBeds
                    if (supplierid == "4")
                    {

                        HotelBeds hbreq = new HotelBeds();
                        responsehotels = hbreq.HotelDetailHotelBeds(req);
                        try
                        {
                            hoteldescription = responsehotels.Descendants("description").FirstOrDefault().Value;

                            #region Contact Info
                            List<XElement> phones = responsehotels.Descendants("phone").ToList();
                            for (int i = 0; i < phones.Count();i++ )
                            {
                                string phoneType = phones[i].Descendants("phoneType").FirstOrDefault().Value;
                                if(phoneType=="FAXNUMBER")
                                {
                                    Fax = phones[i].Descendants("phoneNumber").FirstOrDefault().Value;
                                }
                                else
                                {
                                    Phone = Phone + " " + phones[i].Descendants("phoneNumber").FirstOrDefault().Value;
                                }
                            }
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            hoteldescription = "";
                        }
                    }
                    #endregion
                    #region DOTW
                    if (supplierid == "5")
                    {
                        DotwService dotwObj = new DotwService(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement htdetails = dotwObj.HtlDesReq(req);                       
                        return htdetails;
                    }
                    #endregion
                    #region HotelsPro
                    if (supplierid == "6")
                    {
                        HotelsProHtlDetail hpro = new HotelsProHtlDetail();
                        XElement hoteldetails = hpro.HotelDetailHotelsPro(req);
                        return hoteldetails;
                    }
                    #endregion
                    #region Travco
                    if (supplierid == "7")
                    {
                        Travco travcoObj = new Travco(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement htdetails = travcoObj.getHotelDescription(req);
                        return htdetails;
                    }
                    #endregion
                    #region Jac Travel
                    if (supplierid == "8")
                    {
                        int CountryID = 0;
                        foreach (var item in req.Descendants("hoteldescRequest"))
                        {
                            CountryID = Convert.ToInt32(item.Element("AreaId").Value);
                        }
                        string Path = HttpContext.Current.Server.MapPath(@"~\App_Data\JacTravel\Property\" + CountryID + ".xml");
                        if (File.Exists(Path))
                        {
                            XElement JacHtl = XElement.Load(Path);
                            Jac_HtlDetail jacObj = new Jac_HtlDetail();
                            XElement htdetails = jacObj.getHotelDetail(req, JacHtl);
                            return htdetails;
                        }
                    }
                    #endregion
                    #region RTS
                    if (supplierid == "9")
                    {
                        HTlStaticData jacObj = new HTlStaticData();
                        XElement htdetails = jacObj.GetHtlDetail(req.Descendants("hoteldescRequest").FirstOrDefault().Element("HotelID").Value);
                        return htdetails;
                    }
                    #endregion
                    #region Miki
                    if (supplierid == "11")
                    {
                        MikiInternal mik = new MikiInternal();
                        XElement htdetails = mik.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region Restel
                    if (supplierid == "13")
                    {
                        RestelServices rs = new RestelServices();
                        XElement htdetails = rs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region W2M
                    if (supplierid == "16")
                    {
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(16, customerid);
                        XElement htdetails = rs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region EgyptExpress
                    if (supplierid == "17")
                    {
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(17, customerid);
                        XElement htdetails = rs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region Sal Tours
                    if (supplierid == "19")
                    {
                        SalServices sser = new SalServices();
                        XElement hotelDetails = sser.HotelDetails(req);
                        return hotelDetails;
                    }
                    #endregion
                    #region TBO Holidays
                    if (supplierid == "21")
                    {
                        TBOServices tbs = new TBOServices();
                        XElement hotelDetails = tbs.HotelDetail(req);
                        return hotelDetails;
                    }
                    #endregion
                    #region LOH
                    if (supplierid == "23")
                    {
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(23, customerid);
                        XElement htdetails = rs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region Gadou
                    if (supplierid == "31")
                    {
                        //GodouServices gds = new GodouServices();
                        //XElement hotelDetails = gds.HotelDetails(req);
                        //return hotelDetails;
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(31, customerid);
                        XElement htdetails = rs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region LCI
                    if (supplierid == "35")
                    {
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(35, customerid);
                        XElement htdetails = rs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion                   
                    #region SunHotels
                    if (supplierid == "36")
                    {
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        SunHotelsResponse objRs = new SunHotelsResponse(36, customerid);
                        XElement htdetails = objRs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region Total Stay
                    if (supplierid == "37")
                    {
                        int CountryID = 0;
                        foreach (var item in req.Descendants("hoteldescRequest"))
                        {
                            CountryID = Convert.ToInt32(item.Element("AreaId").Value);
                        }
                        string Path = HttpContext.Current.Server.MapPath(@"~\App_Data\JacTravel\Property\" + CountryID + ".xml");
                        if (File.Exists(Path))
                        {
                            XElement JacHtl = XElement.Load(Path);
                            Jac_HtlDetail jacObj = new Jac_HtlDetail();
                            XElement htdetails = jacObj.getHotelDetail(req, JacHtl);
                            return htdetails;
                        }
                        //else
                        //{
                        //    #region File doesn't exist
                        //    IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                        //    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        //    XElement hoteldescdoc = new XElement(
                        //      new XElement(soapenv + "Envelope",
                        //                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                        //                new XElement(soapenv + "Header",
                        //                 new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                        //                 new XElement("Authentication",
                        //                     new XElement("AgentID", AgentID),
                        //                     new XElement("UserName", username),
                        //                     new XElement("Password", password),
                        //                     new XElement("ServiceType", ServiceType),
                        //                     new XElement("ServiceVersion", ServiceVersion))),
                        //                 new XElement(soapenv + "Body",
                        //                     new XElement(request.Single()),
                        //           new XElement("hoteldescResponse",
                        //               new XElement("ErrorTxt", "Invalid area Id.")
                        //                       )
                        //                   )
                        //  ));
                        //    return hoteldescdoc;
                        //    #endregion
                        //}
                    }
                    #endregion
                    #region SmyRooms
                    if (supplierid == "39")
                    {
                        TGServices tgs = new TGServices(39, req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement hotelDetails = tgs.HotelDetail(req);
                        return hotelDetails;
                    }
                    #endregion
                    #region AlphaTours
                    if (supplierid == "41")
                    {
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(41, customerid);
                        XElement htdetails = rs.hoteldetails(req);
                        return htdetails;
                    }
                    #endregion
                    #region Hoojoozat
                    if (supplierid == "45")
                    {
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        HoojService rs = new HoojService(customerid);
                        XElement htdetails = rs.HotelDescription(req);
                        return htdetails;
                    }
                    #endregion
                    #region Vot
                    if (supplierid == "46")
                    {
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        VOTService rs = new VOTService(customerid);
                        XElement htdetails = rs.HotelDescription(req);
                        return htdetails;
                    }
                    #endregion
                    #region Ebookingcenter
                    if (supplierid == "47")
                    {
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        EBookingService rs = new EBookingService(customerid);
                        XElement htdetails = rs.HotelDescription(req);
                        return htdetails;
                    }
                    #endregion
                    #region XML OUT

                    if (hoteldescription != null || hoteldescription!="")
                    {
                        if (supplierid == "4")
                        {
                            #region Hotel Details XML OUT
                            IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement hoteldescdoc = new XElement(
                              new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                                         new XElement(soapenv + "Body",
                                             new XElement(request.Single()),
                                   new XElement("hoteldescResponse",
                                       new XElement("Hotels",
                                           new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(req.Descendants("HotelID").Single().Value)),
                                               new XElement("Description", Convert.ToString(hoteldescription)),
                                               new XElement("Images", hotelimagesHotelBeds(responsehotels.Descendants("image").ToList())),
                                               new XElement("Facilities", hotelfacilitiesHotelBeds(responsehotels.Descendants("facility").ToList())),
                                                new XElement("ContactDetails", new XElement("Phone", Convert.ToString(Phone)), new XElement("Fax", Convert.ToString(Fax))),
                                                new XElement("CheckinTime", Convert.ToString(checkintime)),
                                               new XElement("CheckoutTime", Convert.ToString(checkouttime))
                                               ))))));
                            return hoteldescdoc;
                            #endregion
                        }
                        else if (supplierid == "2")
                        {

                            #region Hotel Details XML OUT
                            IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement hoteldescdoc = new XElement(
                              new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                                         new XElement(soapenv + "Body",
                                             new XElement(request.Single()),
                                   new XElement("hoteldescResponse",
                                       new XElement("Hotels",
                                           new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(req.Descendants("HotelID").Single().Value)),
                                               new XElement("Description", Convert.ToString(hoteldescription)),
                                               new XElement("Images", image),
                                               new XElement("Facilities", facility),
                                                new XElement("ContactDetails", new XElement("Phone", Convert.ToString(Phone)), new XElement("Fax", Convert.ToString(Fax))),
                                                new XElement("CheckinTime", Convert.ToString(checkintime)),
                                               new XElement("CheckoutTime", Convert.ToString(checkouttime))
                                               ))))));
                            return hoteldescdoc;
                            #endregion

                        }
                        else if (supplierid == "1")
                        {
                            #region Hotel Details XML OUT
                            IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement hoteldescdoc = new XElement(
                              new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                                         new XElement(soapenv + "Body",
                                             new XElement(request.Single()),
                                   new XElement("hoteldescResponse",
                                       new XElement("Hotels",
                                           new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(req.Descendants("HotelID").Single().Value)),
                                               new XElement("Description", Convert.ToString(hoteldescription)),
                                               new XElement("ContactDetails", new XElement("Phone", Convert.ToString(Phone)), new XElement("Fax", Convert.ToString(Fax))),
                                               new XElement("CheckinTime", Convert.ToString(checkintime)),
                                               new XElement("CheckoutTime", Convert.ToString(checkouttime)),
                                                new XElement("Images", darinaimage),
                                                new XElement("Facilities", darinafacility)
                                               ))))));
                            return hoteldescdoc;
                            #endregion
                        }
                        else
                        {
                            #region Hotel Details XML OUT
                            IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement hoteldescdoc = new XElement(
                              new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                                         new XElement(soapenv + "Body",
                                             new XElement(request.Single()),
                                   new XElement("hoteldescResponse",
                                       new XElement("Hotels",
                                           new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(req.Descendants("HotelID").Single().Value)),
                                               new XElement("Description", Convert.ToString(hoteldescription)),
                                               new XElement("ContactDetails", new XElement("Phone", Convert.ToString(Phone)), new XElement("Fax", Convert.ToString(Fax))),
                                               new XElement("CheckinTime", Convert.ToString(checkintime)),
                                               new XElement("CheckoutTime", Convert.ToString(checkouttime))
                                               ))))));
                            return hoteldescdoc;
                            #endregion
                        }
                    }
                    #endregion                                       
                    #region Server is not responding
                    else
                    {
                        #region Server is not responding
                        IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        XElement hoteldescdoc = new XElement(
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
                               new XElement("hoteldescResponse",
                                   new XElement("ErrorTxt", "Server is not responding")
                                           )
                                       )
                      ));
                        return hoteldescdoc;
                        #endregion
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    XElement hoteldescdoc = new XElement(
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
                           new XElement("hoteldescResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    return hoteldescdoc;
                    #endregion
                }
                #endregion
            }
            else
            {
                #region Invalid Credentials
                IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement hoteldescdoc = new XElement(
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
                       new XElement("hoteldescResponse",
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
                    ));
                return hoteldescdoc;
                #endregion
            }
            #endregion
        }
        #endregion
        #region Hotel Details (Tourico)
        public string gethoteldescriptionTourico()
        {
            try
            {
                Tourico.AuthenticationHeader hd = new Tourico.AuthenticationHeader();
                hd.LoginName = "HOL916";
                hd.Password = "111111";
                hd.Version = "5";
                Tourico.HotelFlowClient client = new Tourico.HotelFlowClient();
                Tourico.GetHotelDetailsV3Request req = new Tourico.GetHotelDetailsV3Request();
                Tourico.HotelID[] req1 = new Tourico.HotelID[1];
                for (int i = 0; i < 1; i++)
                {
                    req1[i] = new Tourico.HotelID
                    {
                        id = Convert.ToInt32(reqTravillio.Descendants("HotelID").SingleOrDefault().Value)
                    };
                }
                req.HotelIds = req1;
                Tourico.Feature[] fch = new Tourico.Feature[1];
                for (int i = 0; i < 1; i++)
                {
                    fch[i] = new Tourico.Feature { name = "", value = "" };
                }
                req.Features = fch;
                Tourico.TWS_HotelDetailsV3 result1 = client.GetHotelDetailsV3(hd, req1, fch);
                string desc = result1.LongDescription[0].FreeTextLongDescription;
                return desc;
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion
        #region Methods for Darina Holidays
        public string CallWebService(XElement req, string url, string action, string flag)
        {
            try
            {
                var _url = url;
                var _action = action;
                XDocument soapEnvelopeXml = new XDocument();
                if (flag == "htdescription")
                {
                    soapEnvelopeXml = CreateSoapEnvelopehtdescription(req);
                }
                HttpWebRequest webRequest = CreateWebRequest(_url, _action);
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                    return soapResult;
                }
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
        }
        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }
        private static XDocument CreateSoapEnvelopehtdescription(XElement req)
        {
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            //DarinaCredentials _credential = new DarinaCredentials();
            //AccountName = _credential.AccountName;
            //UserName = _credential.UserName;
            //Password = _credential.Password;
            //AgentID = _credential.AgentID;
            //Secret = _credential.Secret;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "1");
            AccountName = suppliercred.Descendants("AccountName").FirstOrDefault().Value;
            UserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
            AgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            Secret = suppliercred.Descendants("SecStr").FirstOrDefault().Value;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soap:Body>" +
                            "<GetBasicData_PropertyDescription xmlns='http://travelcontrol.softexsw.us/'>" +
                               "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                              "<HotelID>" + req.Descendants("HotelID").FirstOrDefault().Value + "</HotelID>" +
                              "<Language>7</Language>" +
                            "</GetBasicData_PropertyDescription>" +
                          "</soap:Body>" +
                        "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static void InsertSoapEnvelopeIntoWebRequest(XDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
        #endregion
        #region Hotel Images HotelBeds
        public IEnumerable<XElement> hotelimagesHotelBeds(List<XElement> images)
        {
            Int32 length = images.Count();
            List<XElement> image = new List<XElement>();
            
            if (length == 0)
            {
                image.Add(new XElement("Image", new XAttribute("Path", ""), new XAttribute("Caption", "")));
            }
            else
            {
                for (int i = 0; i < length; i ++)
                {
                    image.Add(new XElement("Image",
                        new XAttribute("Path", Convert.ToString("http://photos.hotelbeds.com/giata/bigger/" + images[i].Attribute("path").Value)),
                      new XAttribute("Caption", Convert.ToString(""))));
                }
            }
            return image;
        }
        #endregion
        #region Hotel Facilities HotelBeds
        public IEnumerable<XElement> hotelfacilitiesHotelBeds(List<XElement> facility)
        {
            Int32 length = facility.Count();
            List<XElement> fac = new List<XElement>();
            XElement hbfacility = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelBeds\hb_facility.xml"));
            if (length == 0)
            {
                fac.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        string description = string.Empty;
                        XElement descrp = hbfacility.Descendants("facility").Where(x => x.Attribute("code").Value == facility[i].Attribute("facilityCode").Value && x.Attribute("facilityGroupCode").Value == facility[i].Attribute("facilityGroupCode").Value).FirstOrDefault();
                        description = descrp.Descendants("description").FirstOrDefault().Value;
                        string addval = string.Empty;

                        int number = facility[i].Attributes("number").Count();
                        if (number > 0)
                        {
                            addval = addval + description + " - " + facility[i].Attribute("number").Value;
                        }
                        int indYesOrNo = facility[i].Attributes("indYesOrNo").Count();
                        if (indYesOrNo > 0)
                        {
                            string indyes = description + "- No";
                            if (facility[i].Attribute("indYesOrNo").Value == "true")
                            {
                                indyes = description + "- Yes";
                            }
                            addval = addval + " - " + indyes;
                        }
                        int indLogic = facility[i].Attributes("indLogic").Count();
                        if (indLogic > 0)
                        {
                            string indlog = description + " -No";
                            if (facility[i].Attribute("indLogic").Value == "true")
                            {
                                indlog = description + " -Yes";
                            }
                            addval = addval + " - " + indlog;
                        }
                        int distance = facility[i].Attributes("distance").Count();
                        if (distance > 0)
                        {
                            addval = addval + description + " - " + facility[i].Attribute("distance").Value + " meter away";
                        }
                        int indFee = facility[i].Attributes("indFee").Count();
                        if (indFee > 0)
                        {
                            string indfe = description + " is Free";
                            if (facility[i].Attribute("indFee").Value == "true")
                            {
                                indfe = "Chargable";
                            }
                            addval = addval + " - " + indfe;
                        }
                        int ageFrom = facility[i].Attributes("ageFrom").Count();
                        if (ageFrom > 0)
                        {
                            addval = addval + " - " + facility[i].Attribute("ageFrom").Value + " Minimum age to access the facility";
                        }
                        int ageTo = facility[i].Attributes("ageTo").Count();
                        if (ageTo > 0)
                        {
                            addval = addval + " - " + facility[i].Attribute("ageTo").Value + " Maximum age to access the facility";
                        }
                        int textValue = facility[i].Attributes("textValue").Count();
                        if (textValue > 0)
                        {
                            addval = addval + " - " + facility[i].Attribute("textValue").Value;
                        }
                        int dateFrom = facility[i].Attributes("dateFrom").Count();
                        if (dateFrom > 0)
                        {
                            addval = addval + " - Facility is valid from " + facility[i].Attribute("dateFrom").Value;
                        }
                        int dateTo = facility[i].Attributes("dateTo").Count();
                        if (dateTo > 0)
                        {
                            addval = addval + " - Facility is valid to " + facility[i].Attribute("dateTo").Value;
                        }
                        int timeFrom = facility[i].Attributes("timeFrom").Count();
                        if (timeFrom > 0)
                        {
                            addval = addval + " - Facility is valid from " + facility[i].Attribute("timeFrom").Value;
                        }
                        int timeTo = facility[i].Attributes("timeTo").Count();
                        if (timeTo > 0)
                        {
                            addval = addval + " - Facility is valid to " + facility[i].Attribute("timeTo").Value;
                        }
                        int amount = facility[i].Attributes("amount").Count();
                        if (amount > 0)
                        {
                            addval = addval + " - Amount of the facility fee " + facility[i].Attribute("amount").Value;
                        }
                        int currency = facility[i].Attributes("currency").Count();
                        if (currency > 0)
                        {
                            addval = addval + " - Currency of the facility fee " + facility[i].Attribute("currency").Value;
                        }

                        int applicationType = facility[i].Attributes("applicationType").Count();
                        if (applicationType > 0)
                        {
                            if (facility[i].Attribute("applicationType").Value == "PN")
                            {
                                addval = addval + " - Per Person Per Night";
                            }
                            if (facility[i].Attribute("applicationType").Value == "PS")
                            {
                                addval = addval + " - Per Person Per Stay";
                            }
                            if (facility[i].Attribute("applicationType").Value == "UH")
                            {
                                addval = addval + " - Per Unit Per Hour";
                            }
                            if (facility[i].Attribute("applicationType").Value == "UN")
                            {
                                addval = addval + " - Per Unit Per Night";
                            }
                            if (facility[i].Attribute("applicationType").Value == "US")
                            {
                                addval = addval + " - Per Unit Per Stay";
                            }
                        }
                        fac.Add(new XElement("Facility", Convert.ToString(addval)));
                        //fac.Add(new XElement("Facility", Convert.ToString(facility[i].Descendants("description").FirstOrDefault().Value +" ("+ addval+")")));                      

                    }
                }
                catch { }
            }
            return fac;
        }
        #endregion

        #region Hotel Images Tourico
        public IEnumerable<XElement> hotelimagesTourico(List<XElement> images)
        {
            Int32 length = images.Count();
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                image.Add(new XElement("Image", new XAttribute("Path", ""), new XAttribute("Caption", "")));
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    image.Add(new XElement("Image",
                        new XAttribute("Path", Convert.ToString(images[i].Attribute("path").Value)),
                      new XAttribute("Caption", Convert.ToString(""))));

                });
            }
            return image;
        }
        #endregion
        #region Hotel Facilities Tourico
        public IEnumerable<XElement> hotelfacilitiesTourico(List<XElement> facility)
        {
            Int32 length = facility.Count();
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                image.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    image.Add(new XElement("Facility", Convert.ToString(facility[i].Attribute("name").Value)));

                });
            }
            return image;
        }
        #endregion
        #region Hotel Images Darina
        public IEnumerable<XElement> hotelimagesDarina(List<XElement> images)
        {
            Int32 length = images.Count();
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                image.Add(new XElement("Image", new XAttribute("Path", ""), new XAttribute("Caption", "")));
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    image.Add(new XElement("Image",
                        new XAttribute("Path", Convert.ToString(images[i].Value)),
                      new XAttribute("Caption", Convert.ToString(""))));

                });
            }
            return image;
        }
        #endregion
        #region Hotel Facilities Darina
        public IEnumerable<XElement> hotelfacilitiesDarina(List<XElement> facility)
        {
            Int32 length = facility.Count();
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                image.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    image.Add(new XElement("Facility", Convert.ToString(facility[i].Value)));

                });
            }
            return image;
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