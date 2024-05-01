using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Common.DotW;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.DotW;

namespace TravillioXMLOutService.Supplier.DotW
{
    public class DotwRepository : IDisposable
    {
        string XmlPath = ConfigurationManager.AppSettings["DotWNPath"];
        DotWCredentials _credetials;

        public DotwRepository(string serviceHost, string userName, string password, string id,int currency)
        {
            _credetials = new DotWCredentials(serviceHost, userName, password, id,currency);

        }

        public XDocument GetResponse(XDocument requestXml, Client model)
        {
            var startTime = DateTime.Now;
            string response = string.Empty;
            XDocument Responsexml = new XDocument(new XElement("result", "request Initialize"));
            try
            {
                string request = requestXml.ToString();
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(_credetials.ServiceHost);
                myHttpWebRequest.Method = "POST";
                byte[] data = Encoding.ASCII.GetBytes(request);
                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(_credetials.UserName + ":" + _credetials.Password));
                myHttpWebRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                myHttpWebRequest.ContentType = "application/xml";
                myHttpWebRequest.ContentLength = data.Length;
                Stream requestStream = myHttpWebRequest.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                Stream responseStream = myHttpWebResponse.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(responseStream, Encoding.Default);
                response = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                responseStream.Close();
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = model.Action;
                custEx.PageName = "GetResponse";
                custEx.CustomerID = model.Customer.ToString();
                custEx.TranID = model.TrackNo;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
            }
            finally
            {
                APILogDetail log = new APILogDetail();
                log.customerID = model.Customer;
                log.LogTypeID = model.ActionId;
                log.LogType = model.Action;
                log.SupplierID = 5;
                log.TrackNumber = model.TrackNo;
                log.logrequestXML = requestXml.ToString();

                log.StartTime = startTime;
                log.EndTime = DateTime.Now;
                SaveAPILog savelog = new SaveAPILog();
                try
                {
                    string content = Regex.Replace(response, @"&(?!(?:lt|gt|amp|apos|quot|#\d+|#x[a-f\d]+);)", "&amp;", RegexOptions.IgnoreCase);
                    Responsexml = XDocument.Parse(content);
                    log.logresponseXML = Responsexml.ToString();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException custEx = new CustomException(ex);
                    custEx.MethodName = model.Action;
                    custEx.PageName = "GetResponse";
                    custEx.CustomerID = model.Customer.ToString();
                    custEx.TranID = model.TrackNo;
                    savelog.SendCustomExcepToDB(custEx);
                    log.logMsg = ex.Message.ToString();
                    log.logresponseXML = response;
                    savelog.SaveAPILogwithResponseError(log);
                }


                XElement SearReq = requestXml.Descendants("request").FirstOrDefault();
                SearReq.AddAfterSelf(Responsexml.Root);

            }
            return Responsexml;
        }

        public XDocument GetPreResponse(XDocument requestXml, Client model)
        {
            var startTime = DateTime.Now;

            XDocument Responsexml = new XDocument(new XElement("result", "request Initialize"));
            try
            {
                string request = requestXml.ToString();
                model.Action = "RoomAvail";
                string pageContent = GetXml(model);

                model.Action = "PreBook";
                string content = Regex.Replace(pageContent, @"&(?!(?:lt|gt|amp|apos|quot|#\d+|#x[a-f\d]+);)", "&amp;", RegexOptions.IgnoreCase);

                Responsexml = XDocument.Parse(content);
                try
                {

                    APILogDetail log = new APILogDetail();
                    log.customerID = model.Customer;
                    log.LogTypeID = model.ActionId;
                    log.LogType = model.Action;
                    log.SupplierID = 5;
                    log.TrackNumber = model.TrackNo;
                    log.logrequestXML = requestXml.ToString();
                    log.logresponseXML = Responsexml.ToString();
                    log.StartTime = startTime;
                    log.EndTime = DateTime.Now;
                    SaveAPILog savelog = new SaveAPILog();
                    savelog.SaveAPILogs(log);
                }
                catch (Exception ex)
                {
                    CustomException custEx = new CustomException(ex);
                    custEx.MethodName = model.Action;
                    custEx.PageName = "GetResponse";
                    custEx.CustomerID = model.Customer.ToString();
                    custEx.TranID = model.TrackNo;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(custEx);
                }
            }
            catch (Exception ex)
            {
                CustomException custEx = new CustomException(ex);
                custEx.MethodName = model.Action;
                custEx.PageName = "GetResponse";
                custEx.CustomerID = model.Customer.ToString();
                custEx.TranID = model.TrackNo;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(custEx);
            }
            finally
            {
                XElement SearReq = requestXml.Descendants("request").FirstOrDefault();
                SearReq.AddAfterSelf(Responsexml.Root);
                //requestXml.Save(Filepath);
            }
            return Responsexml;
        }

        public string GetXml(Client model)
        {
            string code = string.Empty;

            SqlParameter[] pList = new SqlParameter[5];
            var flag = new SqlParameter();
            flag.ParameterName = "@flag";
            flag.Direction = ParameterDirection.Input;
            flag.SqlDbType = SqlDbType.Int;
            flag.Value = 5;
            pList[0] = flag;

            var supplier = new SqlParameter();
            supplier.ParameterName = "@SuplId";
            supplier.Direction = ParameterDirection.Input;
            supplier.SqlDbType = SqlDbType.BigInt;
            supplier.Value = 5;
            pList[1] = supplier;

            var logtype = new SqlParameter();
            logtype.ParameterName = "@logTypeId";
            logtype.Direction = ParameterDirection.Input;
            logtype.SqlDbType = SqlDbType.Int;
            logtype.Value = model.LogTypeId;
            pList[2] = logtype;

            var trackno = new SqlParameter();
            trackno.ParameterName = "@TrackNO";
            trackno.Direction = ParameterDirection.Input;
            trackno.SqlDbType = SqlDbType.NVarChar;
            trackno.Value = model.TrackNo;
            pList[3] = trackno;

            var logtypeName = new SqlParameter();
            logtypeName.ParameterName = "@LogType";
            logtypeName.Direction = ParameterDirection.Input;
            logtypeName.SqlDbType = SqlDbType.VarChar;
            logtypeName.Value = model.Action;
            pList[4] = logtypeName;


            DataTable result = DotwDataAcess.Get("dotwProc", pList);
            code = result.Rows[0]["logresponseXML"].ToString().Trim();

            return code;
        }


        public string GetRoomPrice(Client model, string xmlDoc)
        {
            string code = string.Empty;

            SqlParameter[] pList = new SqlParameter[6];
            var flag = new SqlParameter();
            flag.ParameterName = "@flag";
            flag.Direction = ParameterDirection.Input;
            flag.SqlDbType = SqlDbType.Int;
            flag.Value = 6;
            pList[0] = flag;

            var supplier = new SqlParameter();
            supplier.ParameterName = "@SuplId";
            supplier.Direction = ParameterDirection.Input;
            supplier.SqlDbType = SqlDbType.BigInt;
            supplier.Value = 5;
            pList[1] = supplier;

            var logtype = new SqlParameter();
            logtype.ParameterName = "@logTypeId";
            logtype.Direction = ParameterDirection.Input;
            logtype.SqlDbType = SqlDbType.Int;
            logtype.Value = model.LogTypeId;
            pList[2] = logtype;

            var trackno = new SqlParameter();
            trackno.ParameterName = "@TrackNO";
            trackno.Direction = ParameterDirection.Input;
            trackno.SqlDbType = SqlDbType.NVarChar;
            trackno.Value = model.TrackNo;
            pList[3] = trackno;

            var logtypeName = new SqlParameter();
            logtypeName.ParameterName = "@LogType";
            logtypeName.Direction = ParameterDirection.Input;
            logtypeName.SqlDbType = SqlDbType.VarChar;
            logtypeName.Value = model.Action;
            pList[4] = logtypeName;

            var roomKeys = new SqlParameter();
            roomKeys.ParameterName = "@xmlKeys";
            roomKeys.Direction = ParameterDirection.Input;
            roomKeys.SqlDbType = SqlDbType.Xml;
            roomKeys.Value = new SqlXml(new XmlTextReader(xmlDoc
                               , XmlNodeType.Document, null));
            pList[5] = roomKeys;

            DataTable result = DotwDataAcess.Get("dotwProc", pList);
            code = result.Rows[0]["responseXML"].ToString().Trim();

            return code;
        }















        #region Remove Namespaces
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            XElement xmlDocumentWithoutNs = removeAllNamespaces(xmlDocument);
            return xmlDocumentWithoutNs;
        }

        private static XElement removeAllNamespaces(XElement xmlDocument)
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