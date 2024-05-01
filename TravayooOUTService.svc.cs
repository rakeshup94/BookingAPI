using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Supplier.XMLOUT;

namespace TravillioXMLOutService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "TravayooOUTService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select TravayooOUTService.svc or TravayooOUTService.svc.cs at the Solution Explorer and start debugging.
    //[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    public class TravayooOUTService : ITravayooOUTService,IDisposable
    {
        #region Hotel Travayoo
        public object HotelAvailability(XElement req)
        {
            //try
            //{
            //    WriteToFile("XML OUT (B2B) for " + req.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
            //}
            //catch { }
            DateTime Reqstattime = DateTime.Now;
            try
            {
                XElement availabilityresponse = null;
                #region Time Start
                try
                {
                    APILogDetail log2 = new APILogDetail();
                    log2.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                    log2.TrackNumber = req.Descendants("TransID").Single().Value;
                    log2.LogTypeID = 0;
                    log2.LogType = "TimeStart";
                    log2.logrequestXML = req.ToString();
                    SaveAPILog savelogt = new SaveAPILog();
                    savelogt.SaveAPILogs(log2);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelAvailability";
                    ex1.PageName = "TravillioService";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                #endregion
                HotelSearch_XMLOUT reqs = new HotelSearch_XMLOUT();
                availabilityresponse = reqs.HotelAvail_XMLOUT(req);
                if (req.Descendants("Response_Type").Single().Value == "JSON")
                {
                    #region JSON Response
                    return JsonConvert.SerializeXNode(availabilityresponse);
                    #endregion
                }
                else
                {
                    #region XML Response
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 1;
                        log.LogType = "Search";
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = availabilityresponse.ToString();
                        log.StartTime = Reqstattime;
                        log.EndTime = DateTime.Now; 
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                        #region Time End
                        try
                        {
                            APILogDetail log3 = new APILogDetail();
                            log3.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                            log3.TrackNumber = req.Descendants("TransID").Single().Value;
                            log3.LogTypeID = 0;
                            log3.LogType = "TimeEnd";
                            SaveAPILog savelog3 = new SaveAPILog();
                            savelog3.SaveAPILogs(log3);
                        }
                        catch (Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "HotelAvailability";
                            ex1.PageName = "TravillioService";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelAvailability";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(availabilityresponse);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelAvailability";
                ex1.PageName = "TravillioService";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").Single().Value;
                string password = req.Descendants("Password").Single().Value;
                string AgentID = req.Descendants("AgentID").Single().Value;
                string ServiceType = req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                       new XElement("searchResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }

        }
        public void WriteToFile(string text)
        {

            string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
            using (StreamWriter writer = new StreamWriter(path, true))
            {


                writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                writer.WriteLine("---------------------------Hotel Logs-----------------------------------------");
                writer.Close();
            }
        }
        public object HotelDetails(XElement request)
        {
            try
            {
                TrvHotelDetails reqs = new TrvHotelDetails();
                XElement htdetails = reqs.CreateHotelDescriptionDetail(request);
                if (request.Descendants("Response_Type").Single().Value == "JSON")
                {

                    return JsonConvert.SerializeXNode(htdetails);
                }
                else
                {
                    #region XML Response
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = request.Descendants("TransID").Single().Value;
                        log.LogTypeID = 102;
                        log.LogType = "HotelDetail";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = htdetails.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);

                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelDetails";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                        ex1.TranID = request.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(htdetails);
                    #endregion
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public object HotelDetailWithCancellations(XElement request)
        {
            try
            {

                TrvHotelDetailsWithCancellation reqs = new TrvHotelDetailsWithCancellation();
                XElement htdetails = reqs.HotelDetailWithCancellations(request);
                if (request.Descendants("Response_Type").Single().Value == "JSON")
                {
                    return JsonConvert.SerializeXNode(htdetails);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = request.Descendants("TransID").Single().Value;
                        log.LogTypeID = 3;
                        log.LogType = "CXLPolicy";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = htdetails.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelDetailWithCancellations";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                        ex1.TranID = request.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    //WriteToFile(htdetails.ToString());
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(htdetails);

                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public object HotelRoomsAvail(XElement req)
        {
            DateTime Reqstattime = DateTime.Now;
            try
            {
                TrvRoomAvailabilityNew reqs = new TrvRoomAvailabilityNew();
                XElement availabilityresponse = reqs.CreateCheckAvailability(req);
                if (req.Descendants("Response_Type").Single().Value == "JSON")
                {
                    return JsonConvert.SerializeXNode(availabilityresponse);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomAvail";
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = availabilityresponse.ToString();
                        log.StartTime = Reqstattime;
                        log.EndTime = DateTime.Now; 
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelRoomsAvail";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }

                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(availabilityresponse);
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelRoomsAvail";
                ex1.PageName = "TravillioService";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").Single().Value;
                string password = req.Descendants("Password").Single().Value;
                string AgentID = req.Descendants("AgentID").Single().Value;
                string ServiceType = req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                       new XElement("searchResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }            

        }
        public object HotelRoomsDesc(XElement req)
        {
            try
            {
                RoomDesc reqs = new RoomDesc();
                XElement availabilityresponse = reqs.roomDescriptionOUT(req);
                if (req.Descendants("Response_Type").Single().Value == "JSON")
                {
                    return JsonConvert.SerializeXNode(availabilityresponse);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 2;
                        log.LogType = "RoomDesc";
                        log.logrequestXML = req.ToString();
                        log.logresponseXML = availabilityresponse.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelRoomsDesc";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(availabilityresponse);
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelRoomsDesc";
                ex1.PageName = "TravillioService";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").Single().Value;
                string password = req.Descendants("Password").Single().Value;
                string AgentID = req.Descendants("AgentID").Single().Value;
                string ServiceType = req.Descendants("ServiceType").Single().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                IEnumerable<XElement> request = req.Descendants("roomDescRequest");
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
                       new XElement("roomDescResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }
        }
        public object HotelPreBooking(XElement request)
        {
            try
            {
                DateTime Reqstattime = DateTime.Now;
                TrvHotelPreBooking reqs = new TrvHotelPreBooking();
                XElement htdetails = reqs.HotelPreBooking(request);
                if (request.Descendants("Response_Type").Single().Value == "JSON")
                {

                    return JsonConvert.SerializeXNode(htdetails);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = request.Descendants("TransID").Single().Value;
                        log.LogTypeID = 4;
                        log.LogType = "PreBook";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = htdetails.ToString();
                        log.StartTime = Reqstattime;
                        log.EndTime = DateTime.Now; 
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelPreBooking";
                        ex1.PageName = "TravayooOUTService";
                        ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                        ex1.TranID = request.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(htdetails);

                }
            }
            catch (Exception ex)
            {
                #region Exception
                try
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "HotelPreBooking";
                    ex1.PageName = "TravayooOUTService";
                    ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                    ex1.TranID = request.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    string username = request.Descendants("UserName").Single().Value;
                    string password = request.Descendants("Password").Single().Value;
                    string AgentID = request.Descendants("AgentID").Single().Value;
                    string ServiceType = request.Descendants("ServiceType").Single().Value;
                    string ServiceVersion = request.Descendants("ServiceVersion").Single().Value;
                    IEnumerable<XElement> req = request.Descendants("HotelPreBookingRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    XElement prebookingdoc = new XElement(
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
                                     new XElement(req.Single()),
                           new XElement("HotelPreBookingResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = request.Descendants("TransID").Single().Value;
                        log.LogTypeID = 4;
                        log.LogType = "PreBook";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = ex.Message.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch { }
                    return prebookingdoc;
                }
                catch { return null; }
                #endregion
            }
        }
        public object HotelBookingConfirmation(XElement request)
        {
            try
            {
                try
                {
                    WriteToFile("Booking for " + request.Descendants("TransactionID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                }
                catch { }
                DateTime Reqstattime = DateTime.Now;
                TrvHotelBooking reqs = new TrvHotelBooking();
                XElement bookingres = reqs.HotelBookingConfirmation(request);
                if (request.Descendants("Response_Type").Single().Value == "JSON")
                {
                    return JsonConvert.SerializeXNode(bookingres);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = request.Descendants("TransactionID").Single().Value;
                        log.LogTypeID = 5;
                        log.LogType = "Book";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = bookingres.ToString();
                        log.StartTime = Reqstattime;
                        log.EndTime = DateTime.Now; 
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelBookingConfirmation";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                        ex1.TranID = request.Descendants("TransactionID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    //WriteToFile(bookingres.ToString());
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(bookingres);

                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelBookingConfirmation";
                ex1.PageName = "TravillioService";
                ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                ex1.TranID = request.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = request.Descendants("UserName").Single().Value;
                string password = request.Descendants("Password").Single().Value;
                string AgentID = request.Descendants("AgentID").Single().Value;
                string ServiceType = request.Descendants("ServiceType").Single().Value;
                string ServiceVersion = request.Descendants("ServiceVersion").Single().Value;
                IEnumerable<XElement> req = request.Descendants("HotelBookingRequest");
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
                                 new XElement(req.Single()),
                       new XElement("HotelBookingResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return bookingdoc;
                #endregion
            }
        }
        public object HotelImportBooking(XElement request)
        {
            try
            {

                TrvHotelImportBooking reqs = new TrvHotelImportBooking();
                XElement bookingres = reqs.HotelImportBooking(request);
                if (request.Descendants("Response_Type").Single().Value == "JSON")
                {
                    return JsonConvert.SerializeXNode(bookingres);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.LogTypeID = 7;
                        log.LogType = "Import";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = bookingres.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelImportBooking";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                        ex1.TranID = request.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(bookingres);

                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public object HotelCancellation(XElement request)
        {
            try
            {
                DateTime Reqstattime = DateTime.Now;
                TrvHotelCancellation reqs = new TrvHotelCancellation();
                XElement cancellationres = reqs.HotelCancellation(request);
                if (request.Descendants("Response_Type").Single().Value == "JSON")
                {
                    return JsonConvert.SerializeXNode(cancellationres);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = request.Descendants("TransID").Single().Value;
                        log.LogTypeID = 6;
                        log.LogType = "Cancel";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = cancellationres.ToString();
                        log.StartTime = Reqstattime;
                        log.EndTime = DateTime.Now; 
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelCancellation";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                        ex1.TranID = request.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(cancellationres);

                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public object HotelCancellationFee(XElement request)
        {
            try
            {

                TrvHotelCancellationFee reqs = new TrvHotelCancellationFee();
                XElement cancellationres = reqs.HotelCancellationFee(request);
                if (request.Descendants("Response_Type").Single().Value == "JSON")
                {
                    return JsonConvert.SerializeXNode(cancellationres);
                }
                else
                {
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(request.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = request.Descendants("TransID").Single().Value;
                        log.LogTypeID = 8;
                        log.LogType = "CancelFee";
                        log.logrequestXML = request.ToString();
                        log.logresponseXML = cancellationres.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "HotelCancellationFee";
                        ex1.PageName = "TravillioService";
                        ex1.CustomerID = request.Descendants("CustomerID").Single().Value;
                        ex1.TranID = request.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    SerializeXMLOut serialization = new SerializeXMLOut();
                    return serialization.Serialize(cancellationres);

                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public void Dispose()
        {
            this.Dispose();
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            GC.SuppressFinalize(this);
        }
        
        #endregion
    }
}
