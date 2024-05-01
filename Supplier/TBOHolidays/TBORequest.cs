using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.TBO;

namespace TravillioXMLOutService.Supplier.TBOHolidays
{
    public class TBORequest
    {
        public XDocument Request(XDocument req, string url, string action, Log_Model model)
        {
            //string url = "http://api.tbotechnology.in/hotelapi_v7/hotelservice.svc";
            XDocument response = new XDocument();
            APILogDetail log = new APILogDetail
            {
                customerID = model.CustomerID,
                logrequestXML = req.ToString(),
                StartTime = DateTime.Now,
                SupplierID = model.Supl_Id,
                TrackNumber = model.TrackNo,
                LogType = model.Logtype,
                LogTypeID = model.LogtypeID
            };
            try
            {
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myhttprequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                       SecurityProtocolType.Tls11 |
                                       SecurityProtocolType.Tls12;
                myhttprequest.Method = "POST";
                myhttprequest.Headers.Add(@"SOAPAction: " + action);
                myhttprequest.ContentType = "application/soap+xml; charset=UTF-8";
                byte[] data = Encoding.ASCII.GetBytes(req.ToString());

                //------------------------------------CONNECTION/XML REQUEST
                Stream requestStream = myhttprequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myhttpresponse = (HttpWebResponse)myhttprequest.GetResponse();

                //-------------------------------------CONNECTION/XML RESPONSE
                Stream responseStream = myhttpresponse.GetResponseStream();
                StreamReader myReader = new StreamReader(responseStream, Encoding.Default);
                string pageContent = myReader.ReadToEnd();
                myReader.Close();
                responseStream.Close();
                myhttpresponse.Close();
                response = XDocument.Parse(pageContent);
                log.logresponseXML = removeAllNamespaces(response.Root).ToString();
                log.EndTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                log.logMsg = ex.Message;
                log.EndTime = DateTime.Now;
                response.Add(new XElement("Data", new XElement("Exception", ex.Message)));
            }
            finally
            {
                try
                {
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch(Exception ex)
                {
                    CustomException custEx = new CustomException(ex);
                    custEx.MethodName = model.Logtype;
                    custEx.PageName = "TBORequest";
                    custEx.CustomerID = model.CustomerID.ToString();
                    custEx.TranID = model.TrackNo;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(custEx);
                }
            }
            return response;
        }

        public static XElement removeAllNamespaces(XElement e)
        {
            return new XElement(e.Name.LocalName,
              (from n in e.Nodes()
               select ((n is XElement) ? removeAllNamespaces(n as XElement) : n)),
                  (e.HasAttributes) ?
                    (from a in e.Attributes()
                     where (!a.IsNamespaceDeclaration)
                     select new XAttribute(a.Name.LocalName, a.Value)) : null);
        }

        public XDocument Httppostbookrequest(XDocument req, string url, string action, string customerID, string trackNumber)
        {
            //string url = "http://api.tbotechnology.in/hotelapi_v7/hotelservice.svc";
            XDocument response = new XDocument();
            try
            {
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myhttprequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                       SecurityProtocolType.Tls11 |
                                       SecurityProtocolType.Tls12;
                myhttprequest.Method = "POST";
                myhttprequest.Headers.Add(@"SOAPAction: " + action);
                myhttprequest.ContentType = "application/soap+xml; charset=UTF-8";
                byte[] data = Encoding.ASCII.GetBytes(req.ToString());
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
                XElement doc = null;
                try
                {
                    XElement hoteldetailsresponse = XElement.Parse(pageContent.ToString());
                    doc = RemoveAllNamespaces(hoteldetailsresponse);
                }
                catch
                {

                }
                try
                {
                    APILogDetail logreq = new APILogDetail();
                    logreq.customerID = Convert.ToInt64(customerID);
                    logreq.TrackNumber = trackNumber;
                    logreq.LogTypeID = 5;
                    logreq.LogType = "Book";
                    logreq.SupplierID = 21;
                    logreq.logrequestXML = req.ToString();
                    logreq.logresponseXML = doc.ToString();
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SaveAPILogs(logreq);
                }
                catch(Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "Httppostbookrequest";
                    ex1.PageName = "TBORequest";
                    ex1.CustomerID = customerID;
                    ex1.TranID = trackNumber;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }
                response = XDocument.Parse(pageContent);
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HttppostbookrequestO";
                ex1.PageName = "TBORequest";
                ex1.CustomerID = customerID;
                ex1.TranID = trackNumber;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
                XElement exresp = new XElement(new XElement("Data", new XElement("Exception", ex.Message)));
                response = XDocument.Parse(exresp.ToString()); 
            }            
            return response;
        }
        #region Remove Namespaces
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            XElement xmlDocumentWithoutNs = rremoveAllNamespaces(xmlDocument);
            return xmlDocumentWithoutNs;
        }

        private static XElement rremoveAllNamespaces(XElement xmlDocument)
        {
            var stripped = new XElement(xmlDocument.Name.LocalName);
            foreach (var attribute in
                    xmlDocument.Attributes().Where(
                    attribute =>
                        !attribute.IsNamespaceDeclaration &&
                        String.IsNullOrEmpty(attribute.Name.NamespaceName)))
            {
                stripped.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
            }
            if (!xmlDocument.HasElements)
            {
                stripped.Value = xmlDocument.Value;
                return stripped;
            }
            stripped.Add(xmlDocument.Elements().Select(
                el =>
                    RemoveAllNamespaces(el)));
            return stripped;
        }
        #endregion
    }
}