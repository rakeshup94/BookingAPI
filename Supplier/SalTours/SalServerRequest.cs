using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.SalTours;

namespace TravillioXMLOutService.Supplier.SalTours
{
    public class SalServerRequest
    {
        public XDocument  SalRequest(XDocument Req, string action, SalTours_Logs model, string customerID)
        {
            HttpWebResponse myhttpresponse = null;
            string URL = action;
            SalServices sser = new SalServices();
            XElement response = null;
            XDocument responsexml = new XDocument();
            DateTime starttime = DateTime.Now;
            string request = Req.ToString();
            try
            {
                
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(URL);
                myhttprequest.Method = "POST";
                //myhttprequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                byte[] data = Encoding.ASCII.GetBytes(request);
                myhttprequest.ContentType = "text/xml";
                myhttprequest.ContentLength = data.Length;
                myhttprequest.KeepAlive = true;
                Stream requestStream = myhttprequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                myhttpresponse = (HttpWebResponse)myhttprequest.GetResponse();
                Stream responseStream = myhttpresponse.GetResponseStream();
                StreamReader myReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myReader.ReadToEnd();
                myReader.Close();
                responseStream.Close();
                myhttpresponse.Close();
                responsexml = XDocument.Parse(pageContent);
                response = sser.removeAllNamespaces(responsexml.Root);
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = model.CustomerID;
                    log.LogTypeID = model.LogtypeID;
                    log.LogType = model.Logtype;
                    log.SupplierID = 19;
                    log.TrackNumber = model.TrackNo;
                    log.logrequestXML = sser.removeAllNamespaces(Req.Root).ToString();
                    log.logresponseXML = response.ToString();
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    APILog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException custEx = new CustomException(ex);
                    custEx.MethodName = model.Logtype;
                    custEx.PageName = "GetResponse";
                    custEx.CustomerID = model.CustomerID.ToString();
                    custEx.TranID = model.TrackNo;
                    APILog.SendCustomExcepToDB(custEx);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return responsexml;
        }
    }
}