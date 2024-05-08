using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Transfer.Model;
using TravillioXMLOutService.Transfer.Models.HB;

namespace TravillioXMLOutService.Repository.Transfer
{
    public class HotelBedRepository : IDisposable
    {
        HBCredentials model;
        private static readonly HttpClient _httpClient = new HttpClient();
        public HotelBedRepository(HBCredentials _model)
        {
            model = _model;
            _httpClient.BaseAddress = new Uri(model.ServiceHost);
            _httpClient.Timeout = new TimeSpan(0, 0, 30);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Api-key", model.UserName);
            _httpClient.DefaultRequestHeaders.Add("X-Signature", model.Password);
        }


        public async Task<SearchResponseModel> GetSearchAsync(RequestModel reqModel)
        {
            var startTime = DateTime.Now;
            string stringResponse;
            SearchResponseModel result;
            string soapResult = string.Empty;
            try
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), reqModel.RequestStr))
                {
                    var response = await _httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        stringResponse = await response.Content.ReadAsStringAsync();

                        if(string.IsNullOrEmpty(stringResponse))
                        {
                            result = JsonConvert.DeserializeObject<SearchResponseModel>(stringResponse);
                        }
                        else
                        {
                            result = null;
                        }
                        
                    }
                    else
                    {
                        throw new HttpRequestException(response.ReasonPhrase);
                    }
                }
                reqModel.ResponseStr = stringResponse;
                reqModel.EndTime = DateTime.Now;
                SaveLog(reqModel);
                return result;
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
