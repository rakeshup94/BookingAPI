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
using TravillioXMLOutService.Models.Transfer.HB;
using TravillioXMLOutService.Transfer.Model;

namespace TravillioXMLOutService.Repository.Transfer
{
    public class HotelBedRepository : IDisposable
    {
        HBCredentials model;
        public HotelBedRepository(HBCredentials _model)
        {
            model = _model;
        }
        public async Task<string> GetResponse(RequestModel reqModel)
        {
            //var startTime = DateTime.Now;
            //string soapResult = string.Empty;
            //try
            //{
            //    HttpWebRequest request = WebRequest.Create(model.ServiceHost) as HttpWebRequest;
            //    request.Method = "POST";
            //    request.ContentType = "application/xml";
            //    //  request.KeepAlive = true;
            //    Byte[] requestData = Encoding.UTF8.GetBytes(reqModel.RequestStr);
            //    Stream requestStream = request.GetRequestStream();
            //    requestStream.Write(requestData, 0, requestData.Length);
            //    requestStream.Close();
            //    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            //    {
            //        using (StreamReader myStreamReader = new StreamReader(response.GetResponseStream()))
            //        {
            //            soapResult = myStreamReader.ReadToEnd();
            //        }
            //    }
            //    reqModel.ResponseStr = soapResult;
            //    reqModel.EndTime = DateTime.Now;
            //    SaveLog(reqModel);
            //    return soapResult;
            //}
            //catch (Exception ex)
            //{
            //    var _exception = new XElement("SearchException",
            //        new XElement("Message", ex.Message),
            //        new XElement("Source", ex.StackTrace),
            //        new XElement("HelpLink", ex.HelpLink));
            //    reqModel.ResponseStr = _exception.ToString();
            //    reqModel.EndTime = DateTime.Now;
            //    SaveLog(reqModel);
            //    throw ex;
            //}
            try
            {
                SearchModel model = new SearchModel()
                {
                    BaseAddress = "https://api.test.hotelbeds.com/",
                    Key = "1d40ff86837b2abe69efc083c800209f",
                    Secret = "26c4b25bc5",
                    language = "en",
                    ftype = "IATA",
                    fcode = "BCN",
                    ttype = "ATLAS",
                    tcode = "57",
                    //departing = "2024-08-20T12:00:00",
                    adults = 2,
                    children = 0,
                    infants = 0
                };
                using (var httpClient = new HttpClient())
                {
                    httpClient.BaseAddress = new Uri(model.BaseAddress);
                    var reqContent = $"transfer-api/1.0/availability/{model.language}/from/{model.ftype}/" +
                        $"{model.fcode}/to/{model.ttype}/{model.tcode}/{model.departing}/" +
                        $"{model.adults}/{model.children}/{model.infants}";
                    httpClient.DefaultRequestHeaders.Clear();
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Api-key", model.Key);
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Signature", model.Secret);

                    var response = await httpClient.GetAsync(reqContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var stringResponse = await response.Content.ReadAsStringAsync();
                        //var resultObj = JsonSerializer.Deserialize<ServiceResponse<List<ProductViewModel>>>(stringResponse,
                        //       new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                        //if (resultObj.Result)
                        //{
                        //}
                        Console.WriteLine(stringResponse);
                        Console.ReadLine();
                    }
                    else
                    {
                        throw new HttpRequestException(response.ReasonPhrase);
                    }
                }
            }
            catch (Exception ex)
            {
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
