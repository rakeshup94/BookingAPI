using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Transfer.HotelBeds;

namespace TravillioXMLOutService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "TransferService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select TransferService.svc or TransferService.svc.cs at the Solution Explorer and start debugging.

    //[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    public class TransferService : ITransferService, IDisposable
    {
        #region Transfer Travayoo
        public object TransferAvailability(XElement req)
        {
            try
            {
                XElement availabilityresponse = null;
                HB_transferAvail reqs = new HB_transferAvail();
                #region Time Start
                try
                {
                    APILogDetail log2 = new APILogDetail();
                    log2.customerID = Convert.ToInt64(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log2.TrackNumber = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                    log2.LogTypeID = 0;
                    log2.LogType = "TimeStart";
                    log2.logrequestXML = req.ToString();
                    APILog.SaveAPILogs(log2);
                }
                catch (Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "TransferAvailability";
                    ex1.PageName = "TransferService";
                    ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                #endregion
                availabilityresponse = reqs.getTransferAvailability(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 1;
                    log.LogType = "Search";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = availabilityresponse.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                    #region Time End
                    try
                    {
                        APILogDetail log3 = new APILogDetail();
                        log3.customerID = Convert.ToInt64(req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value);
                        log3.TrackNumber = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                        log3.LogTypeID = 0;
                        log3.LogType = "TimeEnd";
                        SaveAPILog savelog3 = new SaveAPILog();
                        savelog3.SaveAPILogs(log3);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "TransferAvailability";
                        ex1.PageName = "TransferService";
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
                    ex1.MethodName = "HotelAvailability";
                    ex1.PageName = "TravillioService";
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
                ex1.MethodName = "TransferAvailability";
                ex1.PageName = "TransferService";
                ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object CXLPolicyTransfer(XElement req)
        {
            try
            {
                XElement cxlpolicyresponse = null;
                HB_TrnsfrGetCXLPolicy reqs = new HB_TrnsfrGetCXLPolicy();
                cxlpolicyresponse = reqs.getTransferCXLPolicy(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("TransferCXLPolicyRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("TransferCXLPolicyRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 3;
                    log.LogType = "CXLPolicy";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = cxlpolicyresponse.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CXLPolicyTransfer";
                    ex1.PageName = "TransferService";
                    ex1.CustomerID = req.Descendants("TransferCXLPolicyRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("TransferCXLPolicyRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(cxlpolicyresponse);
                #endregion

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CXLPolicyTransfer";
                ex1.PageName = "TransferService";
                ex1.CustomerID = req.Descendants("TransferCXLPolicyRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransferCXLPolicyRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object PreBookTransfer(XElement req)
        {
            try
            {
                XElement prebookresponse = null;
                HB_TrnsfrGetPreBook reqs = new HB_TrnsfrGetPreBook();
                prebookresponse = reqs.PreBookTransfer(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("PrebookRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("PrebookRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 4;
                    log.LogType = "PreBook";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = prebookresponse.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "PreBookTransfer";
                    ex1.PageName = "TransferService";
                    ex1.CustomerID = req.Descendants("PrebookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("PrebookRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(prebookresponse);
                #endregion

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "PreBookTransfer";
                ex1.PageName = "TransferService";
                ex1.CustomerID = req.Descendants("PrebookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("PrebookRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object ConfirmBookingTransfer(XElement req)
        {
            try
            {
                XElement bookresponse = null;
                #region Booking Request
                try
                {
                    APILogDetail log2 = new APILogDetail();
                    log2.customerID = Convert.ToInt64(req.Descendants("bookRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log2.TrackNumber = req.Descendants("bookRequest").Attributes("TransID").FirstOrDefault().Value;
                    log2.LogTypeID = 5;
                    log2.LogType = "Book";
                    log2.logrequestXML = req.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log2);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "ConfirmBookingTransfer";
                    ex1.PageName = "TransferService";
                    ex1.CustomerID = req.Descendants("bookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("bookRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                #endregion
                HB_TrnsfrBook reqs = new HB_TrnsfrBook();
                bookresponse = reqs.BookTransfer(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("bookRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("bookRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 5;
                    log.LogType = "Book";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = bookresponse.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "ConfirmBookingTransfer";
                    ex1.PageName = "TransferService";
                    ex1.CustomerID = req.Descendants("bookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("bookRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(bookresponse);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "ConfirmBookingTransfer";
                ex1.PageName = "TransferService";
                ex1.CustomerID = req.Descendants("bookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("bookRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
        public object CancelBookingTransfer(XElement req)
        {
            try
            {
                XElement cancelresponse = null;
                HB_TrnsfrCancel reqs = new HB_TrnsfrCancel();
                cancelresponse = reqs.CancelTransfer(req);
                #region XML Response
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(req.Descendants("TransferCancelRequest").Attributes("CustomerID").FirstOrDefault().Value);
                    log.TrackNumber = req.Descendants("TransferCancelRequest").Attributes("TransID").FirstOrDefault().Value;
                    log.LogTypeID = 6;
                    log.LogType = "Cancel";
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = cancelresponse.ToString();
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CancelBookingTransfer";
                    ex1.PageName = "TransferService";
                    ex1.CustomerID = req.Descendants("TransferCancelRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("TransferCancelRequest").Attributes("TransID").FirstOrDefault().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                SerializeXMLOut serialization = new SerializeXMLOut();
                return serialization.Serialize(cancelresponse);
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancelBookingTransfer";
                ex1.PageName = "TransferService";
                ex1.CustomerID = req.Descendants("TransferCancelRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("TransferCancelRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return ex.Message;
                #endregion
            }
        }
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
        #endregion
    }

}