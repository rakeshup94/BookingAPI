using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.TBO
{
    #region TBO Response
    public class tboair_supresponse : IDisposable
    {
        #region Supplier(TBO) API Response
        public string tbo_supresponse(XElement req, string postData, string LogType, int LogTypeID, string trackno, string customerid,string preID)
        {
            #region API Response (TBO)
            var startTime = DateTime.UtcNow;
            string responseXML = string.Empty;
            try
            {
                try
                {
                    XElement suppliercred = airsupplier_Cred.getgds_credentials(customerid, "51");
                    string username = suppliercred.Descendants("UserName").FirstOrDefault().Value;
                    string password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                    string url = string.Empty;
                    if (LogType == "AirSearch")
                    {
                        url = suppliercred.Descendants("AirSearch").FirstOrDefault().Value;
                    }
                    else if (LogType == "AirFareRules")
                    {
                        url = suppliercred.Descendants("FareRule").FirstOrDefault().Value;
                    }
                    else if (LogType == "AirPriceCheck")
                    {
                        url = suppliercred.Descendants("FareQuote").FirstOrDefault().Value;
                    }
                    else if (LogType == "AirBook")
                    {
                        url = suppliercred.Descendants("Book").FirstOrDefault().Value;
                    }
                    else
                    {
                        url = suppliercred.Descendants("AirSearch").FirstOrDefault().Value;
                    }
                    System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient { Timeout = new TimeSpan(0, 0, 500) };
                    httpClient.BaseAddress = new Uri(url);
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    System.Net.Http.StringContent content = new System.Net.Http.StringContent(postData, System.Text.Encoding.UTF8, "application/json");
                    System.Net.Http.HttpResponseMessage response = httpClient.PostAsync(url, content).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        responseXML = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        responseXML = response.Content.ReadAsStringAsync().Result;
                    }
                    try
                    {
                        #region Log
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = trackno;
                        log.preID = preID;
                        log.LogTypeID = LogTypeID;
                        log.LogType = LogType;
                        log.SupplierID = 51;
                        log.logrequestXML = postData.ToString();
                        log.logresponseXML = responseXML.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.UtcNow;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogsflt(log);
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        try
                        {
                            var xml = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(responseXML), new XmlDictionaryReaderQuotas()));
                            APILogDetail logexc = new APILogDetail();
                            logexc.customerID = Convert.ToInt64(customerid);
                            logexc.TrackNumber = trackno;
                            logexc.LogTypeID = LogTypeID;
                            logexc.LogType = LogType;
                            logexc.SupplierID = 51;
                            logexc.logrequestXML = postData.ToString();
                            logexc.logresponseXML = xml.ToString();
                            logexc.StartTime = startTime;
                            logexc.EndTime = DateTime.UtcNow;
                            SaveAPILog savelogexc = new SaveAPILog();
                            savelogexc.SaveAPILogsflt(logexc);
                        }
                        catch { }
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "tbo_apiresponse";
                        ex1.PageName = "tbo_supresponse";
                        ex1.CustomerID = customerid;
                        ex1.TranID = trackno;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    return responseXML;
                }
                catch (WebException webex)
                {
                    #region Exception
                    try
                    {
                        WebResponse errResp = webex.Response;
                        using (Stream respStream = errResp.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(respStream);
                            string text = reader.ReadToEnd();
                            if (LogType == "AirBook")
                            {
                                APILogDetail log = new APILogDetail();
                                log.customerID = Convert.ToInt64(customerid);
                                log.TrackNumber = trackno;
                                log.LogTypeID = LogTypeID;
                                log.LogType = LogType;
                                log.SupplierID = 51;
                                log.logrequestXML = postData.ToString();
                                log.logresponseXML = text.ToString();
                                log.StartTime = startTime;
                                log.EndTime = DateTime.UtcNow;
                                SaveAPILog savelog = new SaveAPILog();
                                savelog.SaveAPILogsflt(log);
                            }
                            CustomException ex1 = new CustomException(webex);
                            ex1.MethodName = "tbo_apiresponse";
                            ex1.PageName = "tbo_supresponse";
                            ex1.CustomerID = customerid;
                            ex1.TranID = trackno;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                    }
                    catch
                    {
                        CustomException ex1 = new CustomException(webex);
                        ex1.MethodName = "tbo_apiresponse";
                        ex1.PageName = "tbo_supresponse";
                        ex1.CustomerID = customerid;
                        ex1.TranID = trackno;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    return "";
                    #endregion
                }
            }
            catch (Exception ex)
            {
                #region Exception
                try
                {
                    if (LogType == "AirBook")
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(customerid);
                        log.TrackNumber = trackno;
                        log.LogTypeID = LogTypeID;
                        log.LogType = LogType;
                        log.SupplierID = 51;
                        log.logrequestXML = postData.ToString();
                        log.logresponseXML = ex.Message.ToString();
                        log.StartTime = startTime;
                        log.EndTime = DateTime.UtcNow;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogsflt(log);
                    }
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "tbo_apiresponse";
                    ex1.PageName = "tbo_supresponse";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trackno;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                catch
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "tbo_apiresponse";
                    ex1.PageName = "tbo_supresponse";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trackno;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                return "";
                #endregion
            }
            #endregion
        }
        #endregion
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                // Free any other managed objects here.
            }
            disposed = true;
        }
        ~tboair_supresponse()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}