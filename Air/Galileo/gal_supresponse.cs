using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Galileo
{
    #region Travelport Response
    public class gal_supresponse : IDisposable
    {
        #region Supplier(Travelport) API Response
        public string gal_apiresponse(XElement req,string postData, string LogType, int LogTypeID, string trackno, string customerid)
        {
            #region API Response (Travelport)
            var startTime = DateTime.UtcNow;
            try
            {
                XElement suppliercred = airsupplier_Cred.getgds_credentials(customerid, "50");
                string username = suppliercred.Descendants("username").FirstOrDefault().Value;
                string password = suppliercred.Descendants("password").FirstOrDefault().Value;
                string url = string.Empty;
                if(LogType=="AirURTicket")
                {
                    url = suppliercred.Descendants("urserviceendpoint").FirstOrDefault().Value;
                }
                else
                {
                    url = suppliercred.Descendants("hostendpoint").FirstOrDefault().Value;
                }
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(postData);                
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                myHttpWebRequest.ContentType = "application/xml";
                myHttpWebRequest.ContentLength = data.Length;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12;
                myHttpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                Stream requestStream = myHttpWebRequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Stream responseStream = myHttpWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                responseStream.Close();
                myHttpWebResponse.Close();
                try
                {
                    #region Log
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(customerid);
                    log.TrackNumber = trackno;
                    log.LogTypeID = LogTypeID;
                    log.LogType = LogType;
                    log.SupplierID = 50;
                    log.logrequestXML = postData.ToString();
                    log.logresponseXML = pageContent.ToString();
                    log.StartTime = startTime;
                    log.EndTime = DateTime.UtcNow;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "gal_apiresponse";
                    ex1.PageName = "gal_supresponse";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trackno;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                return pageContent;
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
                            log.SupplierID = 50;
                            log.logrequestXML = postData.ToString();
                            log.logresponseXML = text.ToString();
                            log.StartTime = startTime;
                            log.EndTime = DateTime.UtcNow;
                            SaveAPILog savelog = new SaveAPILog();
                            savelog.SaveAPILogsflt(log);
                        }
                        CustomException ex1 = new CustomException(webex);
                        ex1.MethodName = "gal_apiresponse";
                        ex1.PageName = "gal_supresponse";
                        ex1.CustomerID = customerid;
                        ex1.TranID = trackno;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                }
                catch
                {
                    CustomException ex1 = new CustomException(webex);
                    ex1.MethodName = "gal_apiresponse";
                    ex1.PageName = "gal_supresponse";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trackno;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                return null;
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
        ~gal_supresponse()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}