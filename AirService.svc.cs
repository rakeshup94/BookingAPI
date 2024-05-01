using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Common;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "AirService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select AirService.svc or AirService.svc.cs at the Solution Explorer and start debugging.
    //[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    public class AirService : IAirService, IDisposable
    {
        #region Air
        public object AirAvailability(XElement req)
        {
            try
            {
                XElement availabilityresponse = null;
                TrvAirSearch reqs = new TrvAirSearch();
                #region Time Start
                try
                {
                    APILogDetail log2 = new APILogDetail();
                    log2.customerID = Convert.ToInt64(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log2.TrackNumber = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                    log2.LogTypeID = 0;
                    log2.LogType = "TimeStart";
                    log2.logrequestXML = req.ToString();
                    SaveAPILog savelogt = new SaveAPILog();
                    savelogt.SaveAPILogsflt(log2);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "AirAvailability";
                    ex1.PageName = "AirService";
                    ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                #endregion
                availabilityresponse = reqs.CreateAirAvailability(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 1;
                    log.LogType = "AirSearch";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = Convert.ToString(availabilityresponse);
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                    #region Time End
                    try
                    {
                        APILogDetail log3 = new APILogDetail();
                        log3.customerID = Convert.ToInt64(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value);
                        log3.TrackNumber = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                        log3.LogTypeID = 0;
                        log3.LogType = "TimeEnd";
                        SaveAPILog saveloga = new SaveAPILog();
                        saveloga.SaveAPILogsflt(log3);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "AirAvailability";
                        ex1.PageName = "AirService";
                        ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "AirAvailability";
                    ex1.PageName = "AirService";
                    ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(availabilityresponse);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirAvailability";
                ex1.PageName = "AirService";
                ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object AirPriceCheck(XElement req)
        {
            try
            {
                XElement response = null;
                TrvAirPreBook reqs = new TrvAirPreBook();
                response = reqs.CreateAirPreBook(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 4;
                    log.LogType = "PreBook";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = response.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "AirPriceCheck";
                    ex1.PageName = "AirService";
                    ex1.CustomerID = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(response);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirPriceCheck";
                ex1.PageName = "AirService";
                ex1.CustomerID = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object AirFareRules(XElement req)
        {
            try
            {
                XElement response = null;
                TrvAirFareRule reqs = new TrvAirFareRule();
                response = reqs.CreateAirFareRules(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 13;
                    log.LogType = "FareRules";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = response.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "AirFareRules";
                    ex1.PageName = "AirService";
                    ex1.CustomerID = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(response);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirFareRules";
                ex1.PageName = "AirService";
                ex1.CustomerID = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object AirBook(XElement req)
        {
            try
            {
                XElement response = null;
                TrvAirBook reqs = new TrvAirBook();
                response = reqs.CreateAirBook(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 5;
                    log.LogType = "AirBook";
                    log.logrequestXML = req.ToString();
                    //log.logresponseXML = response.ToString();
                    log.logresponseXML = response == null ? "" : response.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "AirBook";
                    ex1.PageName = "AirService";
                    ex1.CustomerID = req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(response);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirBook";
                ex1.PageName = "AirService";
                ex1.CustomerID = req.Descendants("BookingRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("BookingRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object AirTicket(XElement req)
        {
            try
            {
                XElement response = null;
                TrvAirTicket reqs = new TrvAirTicket();
                response = reqs.generateAirTicket(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 12;
                    log.LogType = "AirTicketing";
                    log.logrequestXML = req.ToString();
                    //log.logresponseXML = response.ToString();
                    log.logresponseXML = response == null ? "" : response.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "AirTicket";
                    ex1.PageName = "AirService";
                    ex1.CustomerID = req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(response);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirTicket";
                ex1.PageName = "AirService";
                ex1.CustomerID = req.Descendants("ticketRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("ticketRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object AirCancelTicket(XElement req)
        {
            try
            {
                XElement response = null;
                TrvAirCancel reqs = new TrvAirCancel();
                response = reqs.cancelAirTicket(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("cancelRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("cancelRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 6;
                    log.LogType = "Cancel";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = response.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "AirCancelTicket";
                    ex1.PageName = "AirService";
                    ex1.CustomerID = req.Descendants("cancelRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("cancelRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(response);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirCancelTicket";
                ex1.PageName = "AirService";
                ex1.CustomerID = req.Descendants("cancelRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("cancelRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
