using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.Restel;

namespace TravillioXMLOutService.Supplier.Restel
{
    public class RestelServerRequest
    {
        public XDocument RestelResponse(XDocument reqxml, string customerid, string trckID)
        {
            RestelCredentials rc = new RestelCredentials();
            XDocument responsexml = new XDocument();
            DateTime starttime = DateTime.Now;
            string pageContent = string.Empty;
            //APILogDetail log = new APILogDetail
            //{
            //    customerID = model.CustomerID,
            //    logrequestXML = reqxml.ToString(),
            //    StartTime = DateTime.Now,
            //    SupplierID = model.Supl_Id,
            //    TrackNumber = model.TrackNo,
            //    LogType = model.Logtype,
            //    LogTypeID = model.LogtypeID
            //};
            try
            {
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "13");
                string codusu = suppliercred.Descendants("codusu").FirstOrDefault().Value;
                string secacc = suppliercred.Descendants("secacc").FirstOrDefault().Value;
                string codigousu = suppliercred.Descendants("codigousu").FirstOrDefault().Value;
                string affiliacion = suppliercred.Descendants("affiliation").FirstOrDefault().Value;
                string clausu = suppliercred.Descendants("clausu").FirstOrDefault().Value;
                string host = suppliercred.Descendants("host").FirstOrDefault().Value;

                string request = reqxml.ToString();

                string svrCredentials = "codusu=" + codusu + "&secacc=" + secacc + "&afiliacio=" + affiliacion + "&codigousu=" + codigousu + "&clausu=" + clausu;
                string peticion = "&xml=" + HttpUtility.UrlEncode(request);
                string url = host + svrCredentials + peticion;
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myhttprequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(request);

                myhttprequest.ContentType = "application/x-www-form-urlencoded";
                myhttprequest.ContentLength = data.Length;
                myhttprequest.KeepAlive = true;
                Stream requestStream = myhttprequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myhttpresponse = (HttpWebResponse)myhttprequest.GetResponse();
                Stream responseStream = myhttpresponse.GetResponseStream();
                StreamReader myReader = new StreamReader(responseStream, Encoding.Default);
                pageContent = myReader.ReadToEnd();
                myReader.Close();
                responseStream.Close();
                myhttpresponse.Close();
                responsexml = XDocument.Parse(pageContent);
                //log.logresponseXML = removecdata(responsexml.Root).ToString();
                //log.EndTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = "Search";
                custEx.PageName = "RestelServerRequest";
                custEx.CustomerID = customerid;
                custEx.TranID = trckID;
                APILog.SendCustomExcepToDB(custEx);
                //log.logMsg = ex.Message;
                //log.EndTime = DateTime.Now;
                try
                {
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt32(customerid);
                    log.TrackNumber = trckID;
                    log.SupplierID = 13;
                    log.logrequestXML = reqxml.ToString();
                    log.logresponseXML = pageContent;
                    log.LogType = "Search";
                    log.logMsg = ex.Message.ToString();
                    log.LogTypeID = 1;
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogwithResponseError(log);
                }
                catch (Exception exApi)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(exApi);
                    ex1.MethodName = "Search";
                    ex1.PageName = "RestelResponseSearch";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trckID;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion

                }
            }
            finally
            {
                //try
                //{
                //    APILog.SaveAPILogs(log);
                //}
                //catch (Exception ex)
                //{
                //    CustomException custEx = new CustomException(ex);
                //    custEx.MethodName = model.Logtype;
                //    custEx.PageName = "RestelServerRequest";
                //    custEx.CustomerID = model.CustomerID.ToString();
                //    custEx.TranID = model.TrackNo;
                //    APILog.SendCustomExcepToDB(custEx);
                //}
            }
            return responsexml;
        }
        public XDocument RestelResponseSearch(XDocument reqxml, string customerid, int timeout, string trckID)
        {
            RestelCredentials rc = new RestelCredentials();
            XDocument responsexml = new XDocument();
            DateTime starttime = DateTime.Now;
            string pageContent = string.Empty;
            //APILogDetail log = new APILogDetail
            //{
            //    customerID = model.CustomerID,
            //    logrequestXML = reqxml.ToString(),
            //    StartTime = DateTime.Now,
            //    SupplierID = model.Supl_Id,
            //    TrackNumber = model.TrackNo,
            //    LogType = model.Logtype,
            //    LogTypeID = model.LogtypeID
            //};
            try
            {
                XElement suppliercred = supplier_Cred.getsupplier_credentials(customerid, "13");
                string codusu = suppliercred.Descendants("codusu").FirstOrDefault().Value;
                string secacc = suppliercred.Descendants("secacc").FirstOrDefault().Value;
                string codigousu = suppliercred.Descendants("codigousu").FirstOrDefault().Value;
                string affiliacion = suppliercred.Descendants("affiliation").FirstOrDefault().Value;
                string clausu = suppliercred.Descendants("clausu").FirstOrDefault().Value;
                string host = suppliercred.Descendants("host").FirstOrDefault().Value;

                string request = reqxml.ToString();

                string svrCredentials = "codusu=" + codusu + "&secacc=" + secacc + "&afiliacio=" + affiliacion + "&codigousu=" + codigousu + "&clausu=" + clausu;
                string peticion = "&xml=" + HttpUtility.UrlEncode(request);
                string url = host + svrCredentials + peticion;
                HttpWebRequest myhttprequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myhttprequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(request);
                myhttprequest.Timeout = timeout;
                myhttprequest.ContentType = "application/x-www-form-urlencoded";
                myhttprequest.ContentLength = data.Length;
                myhttprequest.KeepAlive = true;
                Stream requestStream = myhttprequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myhttpresponse = (HttpWebResponse)myhttprequest.GetResponse();
                Stream responseStream = myhttpresponse.GetResponseStream();
                StreamReader myReader = new StreamReader(responseStream, Encoding.Default);
                pageContent = myReader.ReadToEnd();
                myReader.Close();
                responseStream.Close();
                myhttpresponse.Close();
                responsexml = XDocument.Parse(pageContent);
                //log.logresponseXML = removecdata(responsexml.Root).ToString();
                //log.EndTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                //log.EndTime = DateTime.Now;
                //CustomException custEx = new CustomException(ex);
                //custEx.MethodName = "Search";
                //custEx.PageName = "RestelServerRequest";
                //custEx.CustomerID = customerid;
                //custEx.TranID = trckID;
                //APILog.SendCustomExcepToDB(custEx);
                try
                {                    
                    APILogDetail log = new APILogDetail();
                    log.customerID = Convert.ToInt32(customerid);
                    log.TrackNumber = trckID;
                    log.SupplierID = 13;
                    log.logrequestXML = reqxml.ToString();
                    log.logresponseXML = pageContent;
                    log.LogType = "Search";
                    log.logMsg = ex.Message.ToString();
                    log.LogTypeID = 1;
                    log.StartTime = starttime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogwithResponseError(log);
                }
                catch (Exception exApi)
                {
                    #region Exception
                    CustomException ex1 = new CustomException(exApi);
                    ex1.MethodName = "Search";
                    ex1.PageName = "RestelResponseSearch";
                    ex1.CustomerID = customerid;
                    ex1.TranID = trckID;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
                    #endregion

                }                
            }
            finally
            {
                //try
                //{
                //    APILog.SaveAPILogs(log);
                //}
                //catch (Exception ex)
                //{
                //    CustomException custEx = new CustomException(ex);
                //    custEx.MethodName = model.Logtype;
                //    custEx.PageName = "RestelServerRequest";
                //    custEx.CustomerID = model.CustomerID.ToString();
                //    custEx.TranID = model.TrackNo;
                //    APILog.SendCustomExcepToDB(custEx);
                //}
            }
            return responsexml;
        }
        public XElement removecdata(XElement e)
        {
            foreach (XElement x in e.Elements())
            {
                if (x.HasElements)
                {
                    removecdata(x);
                }
                else
                {
                    string check = x.Value;
                    check.Replace("![CDATA[", "").Replace("]]", "");
                    x.SetValue(check);
                }
            }
            return e;
        }
        
    }
}