using System;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.EBookingCenter;
using TravillioXMLOutService.Supplier.EBookingCenter;


namespace TravillioXMLOutService.Supplier.EBookingCenter
{
    public class EBookingRequest
    {
        //string url = "https://www.ebookingcenter.com/tbs/reseller/ws/";
        string responseXML;

        public XDocument ServerRequest(string req, string url, LogModel logmodel)
        {
            DateTime startime = DateTime.Now; 
            XDocument response = new XDocument();
            try
            {
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myhttprequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                myhttprequest.Method = "POST";
                myhttprequest.Headers.Add(@"SOAPAction: ");
                myhttprequest.ContentType = "application/soap+xml; charset=UTF-8";
                byte[] data = Encoding.ASCII.GetBytes(req);

                Stream requestStream = myhttprequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myhttpresponse = (HttpWebResponse)myhttprequest.GetResponse();

                Stream responseStream = myhttpresponse.GetResponseStream();
                StreamReader myReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myReader.ReadToEnd();
                myReader.Close();
                responseStream.Close();
                myhttpresponse.Close();
                if (logmodel.LogTypeID == 1)
                {
                    XElement srvRes = XElement.Parse(pageContent);
                    XElement resp = srvRes.RemoveXmlns();
                    if (resp.Descendants("Error").Count() > 0)
                    {
                        response = new XDocument(resp);
                    }
                    else
                    {
                        XElement newResp = resp.Descendants("AvailHotelsXML").FirstOrDefault();
                        XElement respHTL = XElement.Parse(newResp.Value);
                        response = new XDocument(respHTL.RemoveXmlns());
                    }
                }
                else
                {
                    response = XDocument.Parse(pageContent);
                }
            }
            catch (WebException ex)
            {
                response.Add(new XElement("Data", new XElement("Exception", ex.Message)));
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "ServerRequest";
                custEx.PageName = "EBookingCenter";
                custEx.CustomerID = logmodel.CustomerID.ToString();
                custEx.TranID = logmodel.TrackNo;
                SaveAPILog apilog = new SaveAPILog();
                apilog.SendCustomExcepToDB(custEx);
            }
            finally
            {
                #region Save Logs
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = logmodel.CustomerID;
                    log.LogTypeID = logmodel.LogTypeID;
                    log.LogType = logmodel.LogType;
                    log.SupplierID = logmodel.SuplId;
                    log.TrackNumber = logmodel.TrackNo;
                    log.logrequestXML = req;
                    log.logresponseXML = response == null ? null : response.ToString();
                    log.StartTime = startime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog apilog = new SaveAPILog();
                    apilog.SaveAPILogs(log);
                    
                }
                catch (Exception ex)
                {
                    CustomException custEx = new CustomException(ex);
                    custEx.MethodName = "ServerRequest";
                    custEx.PageName = "EBookingRequest";
                    custEx.CustomerID = logmodel.CustomerID.ToString();
                    custEx.TranID = logmodel.TrackNo;
                    SaveAPILog apilog = new SaveAPILog();
                    apilog.SendCustomExcepToDB(custEx);
                }
                #endregion
            }
            return response;
        }
        public XDocument HotelDetailRequest(string req, string action)
        {
            XDocument response = new XDocument();
            try
            {
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create("https://www.ebookingcenter.com/tbs/reseller/ws/");
                myhttprequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                myhttprequest.Method = "POST";
                myhttprequest.Headers.Add(@"SOAPAction: " + action);
                myhttprequest.ContentType = "application/soap+xml; charset=UTF-8";
                byte[] data = Encoding.ASCII.GetBytes(req);

                Stream requestStream = myhttprequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myhttpresponse = (HttpWebResponse)myhttprequest.GetResponse();

                Stream responseStream = myhttpresponse.GetResponseStream();
                StreamReader myReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myReader.ReadToEnd();
                myReader.Close();
                responseStream.Close();
                myhttpresponse.Close();
                response = XDocument.Parse(pageContent);
            }
            catch (WebException ex)
            {
                string message = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                response.Add(new XElement("Data", new XElement("Exception", ex.Message)));
            }
            return response;
        }

      

    }
}