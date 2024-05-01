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
using System.Xml.Serialization;
using TravillioXMLOutService.Supplier.DotW;
using TravillioXMLOutService.Supplier.JacTravel;
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
using TravillioXMLOutService.Supplier.XMLOUTAPI.Cancel;

namespace TravillioXMLOutService.App_Code
{
    public class TrvHotelCancellation:IDisposable
    {
        List<XElement> hotelcancellationextranet;
        XElement travayooreq = null;
        string customerid = string.Empty;
        string transid = string.Empty;
        #region Logs
        public void WriteToFile(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Booking Cancellation Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region Hotel Cancellation XML OUT for Travayoo
        public XElement HotelCancellation(XElement req)
        {
            #region XML OUT for Cancellation
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                #region Cancellation
                try
                {
                    #region Supplier Credentials
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                    #endregion
                    travayooreq = req;
                    #region Darina
                    if (supplierid == "1")
                    {
                        #region Darina
                        dr_Cancel darreq = new dr_Cancel();
                        XElement hotelcxlresponse = darreq.bookingcancellationdarina(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Tourico
                    if (supplierid == "2")
                    {
                        #region Tourico
                        Tr_Cancel trreq = new Tr_Cancel();
                        XElement hotelcxlresponse = trreq.CancelBooking_Tourico(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Extranet
                    if (supplierid == "3")
                    {
                        #region Extranet
                        List<XElement> doc1 = new List<XElement>();
                        HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();

                        string requestxml = string.Empty;
                        requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                                      "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                                        "<Authentication>" +
                                          "<AgentID>0</AgentID>" +
                                          "<UserName>Suraj</UserName>" +
                                          "<Password>123#</Password>" +
                                          "<ServiceType>HT_001</ServiceType>" +
                                          "<ServiceVersion>v1.0</ServiceVersion>" +
                                        "</Authentication>" +
                                      "</soapenv:Header>" +
                                      "<soapenv:Body>" +
                                        "<HotelCancellationRequest>" +
                                          "<Response_Type>XML</Response_Type>" +
                                          "<ConfirmationNumber>" + req.Descendants("ConfirmationNumber").Single().Value + "</ConfirmationNumber>" +
                                        "</HotelCancellationRequest>" +
                                      "</soapenv:Body>" +
                                    "</soapenv:Envelope>";

                        try
                        {
                            object result = extclient.MakeCancelBookingRequestByXML(requestxml);
                            if (result != null)
                            {
                                XElement doc = XElement.Parse(result.ToString());
                                try
                                {
                                    APILogDetail log = new APILogDetail();
                                    log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                                    log.TrackNumber = req.Descendants("TransID").Single().Value;
                                    log.LogTypeID = 6;
                                    log.LogType = "Cancel";
                                    log.SupplierID = 3;
                                    log.logrequestXML = requestxml.ToString();
                                    log.logresponseXML = doc.ToString();
                                    APILog.SaveAPILogs(log);
                                }
                                catch (Exception ex)
                                {
                                    APILog.SendExcepToDB(ex);
                                }
                                hotelcancellationextranet = doc.Descendants("Hotel").ToList();
                            }
                            else
                            {

                                hotelcancellationextranet = doc1.Descendants("Hotel").ToList();
                            }
                        }
                        catch (Exception ex)
                        {

                            hotelcancellationextranet = doc1.Descendants("Hotel").ToList();
                        }
                        if (hotelcancellationextranet.Count() > 0)
                        {
                            IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement cancellationdoc = new XElement(
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
                                           new XElement("HotelCancellationResponse",
                                               new XElement("Rooms",
                                                   new XElement("Room",
                                                       new XElement("Cancellation",
                                                           new XElement("Amount", Convert.ToString(hotelcancellationextranet[0].Descendants("CXLAmount").Single().Value)),
                                                           new XElement("Status", Convert.ToString(hotelcancellationextranet[0].Descendants("Status").Single().Value))
                                                           )
                                                       )
                                                   )
                                  ))));
                            return cancellationdoc;
                        }
                        else
                        {
                            #region Exception
                            IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement cancellationdoc = new XElement(
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
                                   new XElement("HotelCancellationResponse",
                                       new XElement("ErrorTxt", "Server not responding")
                                               )
                                           )
                          ));
                            return cancellationdoc;
                            #endregion
                        }
                        #endregion
                    }
                    #endregion
                    #region HotelBeds
                    if (supplierid == "4")
                    {
                        #region HotelBeds
                        HotelCancellationHotelBeds hbreq = new HotelCancellationHotelBeds();
                        XElement hotelcxlresponse = hbreq.HotelroomCancellationHotelBeds(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region DOTW
                    if (supplierid == "5")
                    {
                        #region DOTW
                        DotwService dotwObj = new DotwService(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement hotelcxlresponse = dotwObj.RoomCxlReq(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region HotelsPro
                    if (supplierid == "6")
                    {
                        #region HotelsPro
                        HotelsProCancellation hbreq = new HotelsProCancellation();
                        XElement hotelcxlresponse = hbreq.HotelroomCancellationHotelsPro(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Travco
                    if (supplierid == "7")
                    {
                        #region Travco

                        Travco trvObj = new Travco(req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement hotelcxlresponse = trvObj.BookingCancellation(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region JacTravel
                    if (supplierid == "8")
                    {
                        #region JacTravel
                        Jac_PreCancel jacreq = new Jac_PreCancel();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "8");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelcxlresponse = jacreq.Precancel(req, Login, Password, url);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region RTS
                    if (supplierid == "9")
                    {
                        #region RTS
                        RTS_BookingCXL obj = new RTS_BookingCXL();
                        XElement hotelcxlresponse = obj.HtlBookingCXl(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region MIKI
                    if (supplierid == "11")
                    {
                        #region MIKI
                        MikiInternal mk = new MikiInternal();
                        XElement hotelcxlresponse = mk.MikiBookingCancellation(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Restel
                    if (supplierid == "13")
                    {
                        #region Restel
                        RestelServices rs = new RestelServices();
                        XElement hotelcxlresponse = rs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region W2M
                    if (supplierid == "16")
                    {
                        #region W2M
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(16, customerid);
                        XElement hotelcxlresponse = rs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region EgyptExpress
                    if (supplierid == "17")
                    {
                        #region EgyptExpress
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(17, customerid);
                        XElement hotelcxlresponse = rs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Sal Tours
                    if (supplierid == "19")
                    {
                        SalServices sser = new SalServices();
                        XElement BookingCancellation = sser.Cancellation(req);
                        return BookingCancellation;
                    }
                    #endregion
                    #region Tbo holidays
                    if (supplierid == "21")
                    {
                        TBOServices tbs = new TBOServices();
                        XElement BookingCancellation = tbs.Cancel(req);
                        return BookingCancellation;
                    }
                    #endregion
                    #region LOH
                    if (supplierid == "23")
                    {
                        #region LOH
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(23, customerid);
                        XElement hotelcxlresponse = rs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Gadou
                    if (supplierid == "31")
                    {
                        //GodouServices gds = new GodouServices();
                        //XElement BookingCancellation = gds.CanelBooking(req);
                        //return BookingCancellation;
                        #region Gadou
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(31, customerid);
                        XElement hotelcxlresponse = rs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region LCI
                    if (supplierid == "35")
                    {
                        #region LCI
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(35, customerid);
                        XElement hotelcxlresponse = rs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region SunHotels
                    if (supplierid == "36")
                    {
                        #region SunHotels
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        SunHotelsResponse objRs = new SunHotelsResponse(36, customerid);
                        XElement hotelcxlresponse = objRs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Total Stay
                    if (supplierid == "37")
                    {
                        #region Total Stay
                        Jac_PreCancel jacreq = new Jac_PreCancel();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "37");
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        XElement hotelcxlresponse = jacreq.Precancel(req, Login, Password, url);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region SmyRooms
                    if (supplierid == "39")
                    {
                        TGServices tgs = new TGServices(39, req.Descendants("CustomerID").FirstOrDefault().Value);
                        XElement BookingCancellation = tgs.CancelBooking(req);
                        return BookingCancellation;
                    }
                    #endregion
                    #region AlphaTours
                    if (supplierid == "41")
                    {
                        #region AlphaTours
                        int customerid = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                        JuniperResponses rs = new JuniperResponses(41, customerid);
                        XElement hotelcxlresponse = rs.BookingCancel(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Hoojoozat
                    if (supplierid == "45")
                    {
                        #region Hoojoozat
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        HoojService rs = new HoojService(customerid);
                        XElement hotelcxlresponse = rs.BookingCancellation(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Vot
                    if (supplierid == "46")
                    {
                        #region Vot
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        VOTService rs = new VOTService(customerid);
                        XElement hotelcxlresponse = rs.BookingCancellation(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Ebookingcenter
                    if (supplierid == "47")
                    {
                        #region Ebookingcenter
                        string customerid = req.Descendants("CustomerID").Single().Value;
                        EBookingService rs = new EBookingService(customerid);
                        XElement hotelcxlresponse = rs.BookingCancellation(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region Booking Express
                    if (supplierid == "501")
                    {
                        #region Booking Express
                        xmloutCancel rs = new xmloutCancel();
                        XElement hotelcxlresponse = rs.cancel_bookingexpressOUT(req);
                        return hotelcxlresponse;
                        #endregion
                    }
                    #endregion
                    #region No Supplier Found
                    else
                    {
                        #region No Supplier Found
                        IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        XElement cancellationdoc = new XElement(
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
                               new XElement("HotelCancellationResponse",
                                   new XElement("ErrorTxt", "Supplier doesn't exist")
                                           )
                                       )
                      ));
                        return cancellationdoc;
                        #endregion
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    XElement cancellationdoc = new XElement(
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
                           new XElement("HotelCancellationResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));

                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelCancellation";
                    ex1.PageName = "TrvHotelCancellation";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    //APILog.SendCustomExcepToDB(ex1);
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    return cancellationdoc;
                    #endregion
                }
                #endregion
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement cancellationdoc = new XElement(
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
                       new XElement("HotelCancellationResponse",
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
              ));
                return cancellationdoc;
                #endregion
            }
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