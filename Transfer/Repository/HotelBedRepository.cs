using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Transfer.Model;

namespace TravillioXMLOutService.Transfer.Repository
{
    public class HotelBedRepository : IDisposable
    {
        HBCredentials model;
        public HotelBedRepository(HBCredentials _model)
        {
            model = _model;
        }
        public string GetResponse(RequestModel reqModel)
        {
            var startTime = DateTime.Now;
            string soapResult = string.Empty;
            try
            {

                HttpWebRequest request = WebRequest.Create(model.ServiceHost) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "application/xml";
                //  request.KeepAlive = true;
                Byte[] requestData = Encoding.UTF8.GetBytes(reqModel.RequestStr);
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(requestData, 0, requestData.Length);
                requestStream.Close();
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (StreamReader myStreamReader = new StreamReader(response.GetResponseStream()))
                    {
                        soapResult = myStreamReader.ReadToEnd();
                    }
                }
                reqModel.ResponseStr = soapResult;
                reqModel.EndTime = DateTime.Now;
                SaveLog(reqModel);

                return soapResult;
            }
            catch (Exception ex)
            {
                var _exception = new XElement("SearchException",
                    new XElement("Message", ex.Message),
                    new XElement("Source", ex.StackTrace),
                    new XElement("HelpLink", ex.HelpLink));
                reqModel.ResponseStr = _exception.ToString();
                reqModel.EndTime = DateTime.Now;
                SaveLog(reqModel);
                throw ex;
            }
        }

        public void SaveLog(RequestModel _req)
        {
            APILogDetail log = new APILogDetail();
            log.customerID = _req.Customer;
            log.LogTypeID = _req.ActionId;
            log.LogType = _req.Action;
            log.TrackNumber = _req.TrackNo;
            log.SupplierID = model.SupplierId;
            log.logrequestXML = _req.RequestStr;
            log.logresponseXML = _req.ResponseStr;
            log.StartTime = _req.StartTime;
            log.EndTime = _req.EndTime;
            SaveAPILog savelog = new SaveAPILog();
            savelog.SaveAPILogs(log);
        }

        public void SaveException(RequestModel _req, Exception ex)
        {
            CustomException custEx = new CustomException(ex);
            custEx.MethodName = _req.Action;
            custEx.PageName = "Repository";
            custEx.CustomerID = _req.Customer.ToString();
            custEx.TranID = _req.TrackNo;
            SaveAPILog saveex = new SaveAPILog();
            saveex.SendCustomExcepToDB(custEx);
        }


        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                model = null;
                // Free any other managed objects here.
                //
            }

            // Free any unmanaged objects here.
            //
            disposed = true;
        }
        ~HotelBedRepository()
        {
            Dispose(false);
        }






        #endregion

    }
}
