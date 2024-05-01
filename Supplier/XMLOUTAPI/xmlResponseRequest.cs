using Newtonsoft.Json;
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

namespace TravillioXMLOutService.Supplier.XMLOUTAPI
{
    public class xmlResponseRequest : IDisposable
    {
        #region XML OUT API Request/Response
        public string xmloutHTTPResponse(XElement req, string LogType, int LogTypeID, string trackno, string customerid,string supplierID)
        {
            #region API Response (XML OUT)
            var startTime = DateTime.UtcNow;
            try
            {
                string hosturl = string.Empty;
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, supplierID);
                string username = suppliercred.Descendants("username").FirstOrDefault().Value;
                string password = suppliercred.Descendants("password").FirstOrDefault().Value;
                string apiKey = suppliercred.Descendants("apiKey").FirstOrDefault().Value;
                if (LogTypeID == 1)
                {
                    hosturl = suppliercred.Descendants("searchurl").FirstOrDefault().Value;
                }
                else if(LogTypeID == 2)
                {
                    hosturl = suppliercred.Descendants("roomurl").FirstOrDefault().Value;
                }
                else if (LogTypeID == 4 || LogTypeID == 3)
                {
                    hosturl = suppliercred.Descendants("checkrateurl").FirstOrDefault().Value;
                }
                else if (LogTypeID == 5)
                {
                    hosturl = suppliercred.Descendants("bookurl").FirstOrDefault().Value;
                }
                else if (LogTypeID == 6)
                {
                    hosturl = suppliercred.Descendants("cancelurl").FirstOrDefault().Value;
                }
                string url = hosturl;
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(req.ToString());
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                myHttpWebRequest.Headers.Add("ING-Api-Key", apiKey);
                myHttpWebRequest.ContentType = "application/xml";
                myHttpWebRequest.Timeout = 240000;
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
                    log.SupplierID = Convert.ToInt16(supplierID);
                    log.logrequestXML = req.ToString();
                    log.logresponseXML = pageContent.ToString();
                    log.StartTime = startTime;
                    log.EndTime = DateTime.UtcNow;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "xmloutHTTPResponse";
                    ex1.PageName = "xmlResponseRequest";
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
                        if (LogType == "Book")
                        {
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt64(customerid);
                            log.TrackNumber = trackno;
                            log.LogTypeID = LogTypeID;
                            log.LogType = LogType;
                            log.SupplierID = 501;
                            log.logrequestXML = req.ToString();
                            log.logresponseXML = text.ToString();
                            log.StartTime = startTime;
                            log.EndTime = DateTime.UtcNow;
                            SaveAPILog savelog = new SaveAPILog();
                            savelog.SaveAPILogs(log);
                        }
                        CustomException ex1 = new CustomException(webex);
                        ex1.MethodName = "xmloutHTTPResponse";
                        ex1.PageName = "xmlResponseRequest";
                        ex1.CustomerID = customerid;
                        ex1.TranID = trackno;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                }
                catch
                {
                    CustomException ex1 = new CustomException(webex);
                    ex1.MethodName = "xmloutHTTPResponse";
                    ex1.PageName = "xmlResponseRequest";
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
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}