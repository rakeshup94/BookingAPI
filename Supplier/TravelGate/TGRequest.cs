using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.TravelGate;

namespace TravillioXMLOutService.Supplier.TravelGate
{
    public class TGRequest
    {
        public string serverRequest(string json, LogModel model, string URL, string APIKEY)
        {
            string pageContent = string.Empty;
            XElement supplierResponse = null;
            DateTime starttime = DateTime.Now;
            HttpWebResponse myhttpresponse = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //string URL = "https://api.travelgatex.com";
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(URL);
                myhttprequest.Method = "POST";
                myhttprequest.ContentType = "application/json";
                myhttprequest.Headers.Add("Authorization", "Apikey " + APIKEY);//e0a20a49-7581-44e6-6c7c-3a72cc6a3eb9");
                myhttprequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                byte[] data = Encoding.ASCII.GetBytes(json);
                myhttprequest.ContentLength = data.Length;
                myhttprequest.KeepAlive = true;
                Stream requestStream = myhttprequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                myhttpresponse = (HttpWebResponse)myhttprequest.GetResponse();
                Stream responseStream = myhttpresponse.GetResponseStream();
                StreamReader myReader = new StreamReader(responseStream, Encoding.Default);
                pageContent = myReader.ReadToEnd();
                myReader.Close();
                responseStream.Close();
                myhttpresponse.Close();
                var jsonResponse = JsonConvert.DeserializeXmlNode(pageContent, "Response");
                supplierResponse = XElement.Parse(jsonResponse.InnerXml);
                supplierResponse.Add(new XElement("JSON", pageContent));
            }
            catch (Exception ex)
            {
                #region Exception Loggin
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "serverRequest";
                custEx.PageName = "TGRequest";
                custEx.CustomerID = model.CustomerID.ToString();
                custEx.TranID = model.TrackNo;
                APILog.SendCustomExcepToDB(custEx); 
                #endregion
            }
            finally
            {
                #region Save Logs
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = model.CustomerID;
                    log.LogTypeID = model.LogtypeID;
                    log.LogType = model.Logtype;
                    log.SupplierID = model.Supl_Id;
                    log.TrackNumber = model.TrackNo;
                    log.logrequestXML = json;
                    log.logresponseXML = supplierResponse == null? null : supplierResponse.ToString();
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    APILog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException custEx = new CustomException(ex);
                    custEx.MethodName = "serverRequest";
                    custEx.PageName = "TGRequest";
                    custEx.CustomerID = model.CustomerID.ToString();
                    custEx.TranID = model.TrackNo;
                    APILog.SendCustomExcepToDB(custEx);

                } 
                #endregion
            }
            return pageContent;
        }
    }
}