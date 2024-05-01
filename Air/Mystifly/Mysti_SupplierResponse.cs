using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Mystifly
{
    public class Mysti_SupplierResponse
    {
        #region Supplier API Response
        public string supplierresponse_mystifly(string url, string postData, string soapaction, string LogType, int LogTypeID,string trackno,string customerid)
        {
            try
            {
                var startTime = DateTime.Now;
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(postData);
                myHttpWebRequest.Headers.Add("SOAPAction", soapaction);
                myHttpWebRequest.ContentType = "text/xml;charset=UTF-8";
                myHttpWebRequest.ContentLength = data.Length;
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
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt64(customerid);
                    log.TrackNumber = trackno;
                    log.LogTypeID = LogTypeID;
                    log.LogType = LogType;
                    log.SupplierID = 12;
                    log.logrequestXML = postData.ToString();
                    log.logresponseXML = pageContent.ToString();
                    log.StartTime = startTime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogsflt(log);
                }
                catch(Exception ex)
                {
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "supplierresponse_mystifly";
                    ex1.PageName = "Mysti_SupplierResponse";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trackno;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                return pageContent;
            }
            catch (WebException webex)
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
                        log.SupplierID = 12;
                        log.logrequestXML = postData.ToString();
                        log.logresponseXML = text.ToString();
                        log.StartTime = DateTime.Now; ;
                        log.EndTime = DateTime.Now;
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogsflt(log);
                    }
                    CustomException ex1 = new CustomException(webex);
                    ex1.MethodName = "supplierresponse_mystifly";
                    ex1.PageName = "Mysti_SupplierResponse";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trackno;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                }
                return null;
            }
        }
        #endregion
    }
}