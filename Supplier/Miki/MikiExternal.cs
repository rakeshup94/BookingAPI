using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Miki;

namespace TravillioXMLOutService.Supplier.Miki
{
    public class MikiExternal
    {
       
        public XDocument MikiResponse(XDocument reqxml, string action,string customerID, MikiLogSave model) // add a parameter to take the action type which will go as the value in action header
        {

            XElement suppliercred = supplier_Cred.getsupplier_credentials(customerID, "11");
            string endpoint = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
            //string host = suppliercred.Descendants("host").FirstOrDefault().Value;
            //if (suppliercred.Attribute("mode").Value.ToUpper().Equals("TEST"))
            //    host = "test.mikinet.co.uk";
            //else
            //    host = "www.mikinet.co.uk";            
            DateTime starttime = DateTime.Now;
            XElement mikireqnns = removeAllNamespaces(reqxml.Root);
            APILogDetail log = new APILogDetail();
            log.customerID = model.CustomerID;
            log.TrackNumber = model.TrackNo;
            log.SupplierID = 11;
            log.logrequestXML = mikireqnns.ToString();
            log.LogType = model.Logtype;
            log.LogTypeID = model.LogtypeID;
            log.StartTime = starttime;            
            XDocument responsexml = new XDocument();
            try
            {
                string request = reqxml.ToString();

                //---------------------------------------HEADER
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(endpoint);
                myhttprequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(request);
                myhttprequest.Headers.Add("Accept-Encoding", "gzip,deflate");                
                myhttprequest.ContentType = "application/soap+xml;charset=UTF-8;action="+action;                
                myhttprequest.KeepAlive = true;                
                //myhttprequest.Host = host;
                myhttprequest.ContentLength = data.Length;
                myhttprequest.UserAgent = "Apache-HttpClient/4.1.1 (java 1.5)";
                myhttprequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

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



                responsexml = XDocument.Parse(pageContent);

            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "MikiResponse";
                ex1.PageName = "MikiExternal";
                ex1.CustomerID = model.CustomerID.ToString();
                ex1.TranID = model.TrackNo;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                #endregion
            }
            finally
            {
                try
                {
                    XElement servrespnns = removeAllNamespaces(responsexml.Root);
                    log.logresponseXML = servrespnns.ToString();
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }              
                  catch (Exception ex)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "MikiResponse";
                    ex1.PageName = "MikiExternal";
                    ex1.CustomerID = model.CustomerID.ToString();
                    ex1.TranID = model.TrackNo;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion
                }               
            }
            return responsexml;
        }
        #region Remove Namespaces
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
        #endregion
    }
}