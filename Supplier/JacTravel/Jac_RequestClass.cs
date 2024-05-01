using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Web;
using System.Xml.Linq;
using System.Security.Cryptography;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class RequestClass
    {

        string Encode(string Data)
        {

            
            Data = HttpUtility.UrlEncode(Data);
            Data = "data=" + Data;
            return Data;
        }

        public string HttpPostRequest(string url, XElement req, string Supplierxml, string Type,int supid,int logtypeid)
        {
            
            DateTime Reqstattime = DateTime.Now;
            try
            {
               
                string postData = Encode(Supplierxml);

                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";

                byte[] data = Encoding.ASCII.GetBytes(postData);

                myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
                myHttpWebRequest.ContentLength = data.Length;
                myHttpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
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

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                log.LogTypeID = logtypeid;
                log.LogType = Type;
                log.SupplierID = supid;
                log.TrackNumber = Type == "Book" ? req.Descendants("TransactionID").Single().Value : Type == "Voucher" ? req.Descendants("TransactionID").Single().Value : req.Descendants("TransID").Single().Value;
                log.StartTime = Reqstattime;
                log.EndTime =DateTime.Now;
                log.logrequestXML = Supplierxml;
                log.logresponseXML = pageContent;

                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);


                return pageContent;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HttpPostRequest";
                ex1.PageName = "RequestClass";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;              
                ex1.TranID =  Type == "Book" ? req.Descendants("TransactionID").Single().Value : Type == "Voucher" ? req.Descendants("TransactionID").Single().Value : req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);

            }
            return null;
        }

                
        public delegate void MyDelegate(List<XElement> lst);
        public event MyDelegate MyEvent;
        public void MultiplePropetySearch(string Supplierxml, int duration, XElement Htl_static, XElement mealtype, int totalroom, XElement Facility, XElement req, int RegionID,string dmc,int supid,string custID)
        {
            try
            {
                XElement suppliercred = supplier_Cred.getsupplier_credentials(custID, Convert.ToString(supid));
                string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;

                string Result = HttpPostRequestxmlout(url, req, Supplierxml, "Search", supid, 1,custID);

                
                if (!Result.Contains("An error occurred: The underlying connection was closed: The connection was closed unexpectedly"))
                {
                    XElement doc = XElement.Parse(Result);
                    string status = (String)doc.Element("ReturnStatus").Element("Success");
                    if (status == "true")
                    {
                        Jac_HotelAvail obj = new Jac_HotelAvail();
                        List<XElement> lst = obj.GetSerachResponce(doc, duration, Htl_static, mealtype, totalroom, Facility, RegionID, req,dmc,supid,custID).ToList();
                        if (MyEvent != null)
                        {
                            MyEvent(lst);
                        }
                    }

                    
                }
          
            }
            catch (Exception ex)
            {

                //APILog.SendExcepToDB(ex);
            }
           
           
         

        }
        // Login Xml Seaction

        public string HttpPostRequestxmlout(string url, XElement req, string Supplierxml, string Type, int supid, int logtypeid,string custID)
        {

            DateTime Reqstattime = DateTime.Now;
            try
            {

                string postData = Encode(Supplierxml);

                HttpWebRequest myHttpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                myHttpWebRequest.Method = "POST";

                byte[] data = Encoding.ASCII.GetBytes(postData);

                myHttpWebRequest.ContentType = "application/x-www-form-urlencoded";
                myHttpWebRequest.ContentLength = data.Length;
                myHttpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
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

                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(custID);
                log.LogTypeID = logtypeid;
                log.LogType = Type;
                log.SupplierID = supid;
                log.TrackNumber = Type == "Book" ? req.Descendants("TransactionID").Single().Value : Type == "Voucher" ? req.Descendants("TransactionID").Single().Value : req.Descendants("TransID").Single().Value;
                log.StartTime = Reqstattime;
                log.EndTime = DateTime.Now;
                log.logrequestXML = Supplierxml;
                log.logresponseXML = pageContent;

                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);


                return pageContent;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HttpPostRequest";
                ex1.PageName = "RequestClass";
                ex1.CustomerID = custID;
                ex1.TranID = Type == "Book" ? req.Descendants("TransactionID").Single().Value : Type == "Voucher" ? req.Descendants("TransactionID").Single().Value : req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);

            }
            return null;
        }


    }
}
