using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Xml;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;
using System.Xml.Linq;
using TravillioXMLOutService.Models.Darina;

namespace TravillioXMLOutService.App_Code
{
    public class TrvHotelCancellationFee:IDisposable
    {
        #region Logs
        public void WriteToFile(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Booking Cancellation Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region XML OUT for Hotel Cancellation Fee (Travayoo)
        public XElement HotelCancellationFee(XElement req)
        {
            #region XML OUT Hotel Cancellation Fee
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SupplierID").Single().Value;
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                #region Cancellation Fee
                #region Supplier Credentials
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                #endregion
                try
                {
                    #region Darina
                    if (supplierid == "1")
                    {                        
                        DarinaCredentials _credential = new DarinaCredentials();
                        var _url = _credential.APIURL;
                        var _action = "http://travelcontrol.softexsw.us/GetFileDetails";
                        var flag = "GetFileDetails";
                        string fileid = req.Descendants("ConfirmationNumber").Single().Value;                   
                        string serviceid = "";
                        string cancelcode = "";
                        serviceid = req.Descendants("ServiceID").Single().Value;             
                        _action = "http://travelcontrol.softexsw.us/CheckHotelCancellationCharges";
                        flag = "CheckHotelCancellationCharges";
                        string cancellationcharges = CallWebService(_url, _action, flag, fileid, serviceid, cancelcode);
                        WriteToFile(cancellationcharges.ToString());
                        XElement doccancellationcharges = XElement.Parse(cancellationcharges);
                        XNamespace res = "http://dtcws.softexsw.us";
                        int error = doccancellationcharges.Descendants(res + "CancelCode").Count();
                        if (error > 0)
                        {
                            cancelcode = doccancellationcharges.Descendants(res + "CancelCode").Single().Value;
                            if (cancelcode != null)
                            {
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                XNamespace resname = "http://travelcontrol.softexsw.us/";
                                IEnumerable<XElement> request = req.Descendants("HotelCancellationFeeRequest").ToList();
                                XElement cancellationdoc = new XElement(
                                  new XElement(soapenv + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                            new XElement(soapenv + "Header",
                                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                             new XElement("Authentication",
                                                 new XElement("AgentID", AgentID),
                                                 new XElement("UserName", username),
                                                 new XElement("Password", password),
                                                 new XElement("ServiceType", ServiceType),
                                                 new XElement("ServiceVersion", ServiceVersion))),
                                             new XElement(soapenv + "Body",
                                                 new XElement(request.Single()),
                                       new XElement("HotelCancellationFeeResponse",
                                           new XElement("Rooms",
                                               new XElement("Room",
                                                   new XElement("Cancellation",
                                                       new XElement("Amount", Convert.ToString(doccancellationcharges.Descendants(res + "Amount").Single().Value))
                                                       )
                                                   )
                                               )
                              ))));
                                return cancellationdoc;
                            }
                            else
                            {
                                IEnumerable<XElement> request = req.Descendants("HotelCancellationFeeRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                XElement cancellationdoc = new XElement(
                                  new XElement(soapenv + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                            new XElement(soapenv + "Header",
                                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                             new XElement("Authentication",
                                                 new XElement("AgentID", AgentID),
                                                 new XElement("UserName", username),
                                                 new XElement("Password", password),
                                                 new XElement("ServiceType", ServiceType),
                                                 new XElement("ServiceVersion", ServiceVersion))),
                                             new XElement(soapenv + "Body",
                                                 new XElement(request.Single()),
                                       new XElement("HotelCancellationFeeResponse",
                                           new XElement("ErrorTxt", "Server is not responding")
                                                   )
                                               )
                              ));
                                return cancellationdoc;
                            }
                        }
                        else
                        {
                            IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement cancellationdoc = new XElement(
                              new XElement(soapenv + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                        new XElement(soapenv + "Header",
                                         new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                         new XElement("Authentication",
                                             new XElement("AgentID", AgentID),
                                             new XElement("UserName", username),
                                             new XElement("Password", password),
                                             new XElement("ServiceType", ServiceType),
                                             new XElement("ServiceVersion", ServiceVersion))),
                                         new XElement(soapenv + "Body",
                                             new XElement(request.Single()),
                                   new XElement("HotelCancellationResponse",
                                       new XElement("ErrorTxt", "Reservation Not Found")
                                               )
                                           )
                          ));
                            return cancellationdoc;
                        }
                    }
                    #endregion
                    #region Tourico
                    if (supplierid == "2")
                    {
                        #region Credentials
                        XElement credential = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\Credential.xml"));
                        string userlogin = string.Empty;
                        string pwd = string.Empty;
                        string version = string.Empty;
                        userlogin = credential.Descendants("username").FirstOrDefault().Value;
                        pwd = credential.Descendants("password").FirstOrDefault().Value;
                        version = credential.Descendants("version").FirstOrDefault().Value;
                        #endregion

                        #region Tourico
                        Int32 cnfrmnum = Convert.ToInt32(req.Descendants("ConfirmationNumber").Single().Value);
                        DateTime cxldate = DateTime.ParseExact(DateTime.Now.ToString("dd/MM/yyyy"), "dd/MM/yyyy", null);
                        TouricoReservation.LoginHeader hd = new TouricoReservation.LoginHeader();
                        hd.username = userlogin;// "HOL916";
                        hd.password = pwd;// "111111";
                        hd.version = version;// "5";
                        TouricoReservation.ReservationsServiceSoapClient client = new TouricoReservation.ReservationsServiceSoapClient();
                        TouricoReservation.CancellationFeeInfo cxlfee = client.GetCancellationFee(hd, cnfrmnum, cxldate);
                        decimal cxlamt = cxlfee.CancellationFeeValue;
                        IEnumerable<XElement> request = req.Descendants("HotelCancellationFeeRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        XElement cancellationdoc = new XElement(
                                  new XElement(soapenv + "Envelope",
                                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                            new XElement(soapenv + "Header",
                                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                             new XElement("Authentication",
                                                 new XElement("AgentID", AgentID),
                                                 new XElement("UserName", username),
                                                 new XElement("Password", password),
                                                 new XElement("ServiceType", ServiceType),
                                                 new XElement("ServiceVersion", ServiceVersion))),
                                             new XElement(soapenv + "Body",
                                                 new XElement(request.Single()),
                                       new XElement("HotelCancellationFeeResponse",
                                           new XElement("Rooms",
                                               new XElement("Room",
                                                   new XElement("Cancellation",
                                                       new XElement("Amount", Convert.ToString(cxlamt))
                                                       )
                                                   )
                                               )
                              ))));
                        return cancellationdoc;
                        #endregion
                    }
                    #endregion
                    #region No Supplier Found
                    else
                    {
                        #region No Supplier Found
                        IEnumerable<XElement> request = req.Descendants("HotelCancellationFeeRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        XElement cancellationdoc = new XElement(
                          new XElement(soapenv + "Envelope",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XElement(soapenv + "Header",
                                     new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                     new XElement("Authentication",
                                         new XElement("AgentID", AgentID),
                                         new XElement("UserName", username),
                                         new XElement("Password", password),
                                         new XElement("ServiceType", ServiceType),
                                         new XElement("ServiceVersion", ServiceVersion))),
                                     new XElement(soapenv + "Body",
                                         new XElement(request.Single()),
                               new XElement("HotelCancellationFeeResponse",
                                   new XElement("ErrorTxt", "Supplier doesn't exist")
                                           )
                                       )
                      ));
                        return cancellationdoc;
                        #endregion
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    IEnumerable<XElement> request = req.Descendants("HotelCancellationFeeRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    XElement cancellationdoc = new XElement(
                      new XElement(soapenv + "Envelope",
                                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                new XElement(soapenv + "Header",
                                 new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                 new XElement("Authentication",
                                     new XElement("AgentID", AgentID),
                                     new XElement("UserName", username),
                                     new XElement("Password", password),
                                     new XElement("ServiceType", ServiceType),
                                     new XElement("ServiceVersion", ServiceVersion))),
                                 new XElement(soapenv + "Body",
                                     new XElement(request.Single()),
                           new XElement("HotelCancellationFeeResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    return cancellationdoc;
                    #endregion
                }
                #endregion
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("HotelCancellationFeeRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement cancellationdoc = new XElement(
                  new XElement(soapenv + "Envelope",
                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            new XElement(soapenv + "Header",
                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                             new XElement("Authentication",
                                 new XElement("AgentID", AgentID),
                                 new XElement("UserName", username),
                                 new XElement("Password", password),
                                 new XElement("ServiceType", ServiceType),
                                 new XElement("ServiceVersion", ServiceVersion))),
                             new XElement(soapenv + "Body",
                                 new XElement(request.Single()),
                       new XElement("HotelCancellationFeeResponse",
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
              ));
                return cancellationdoc;
                #endregion
            }
            #endregion
        }
        #endregion
        #region Methods for Darina
        public string CallWebService(string _url, string _action, string flag, string fileid, string serviceid, string cancelcode)
        {
            try
            {

                XDocument soapEnvelopeXml = new XDocument();

                if (flag == "GetFileDetails")
                {
                    soapEnvelopeXml = CreateSoapEnvelopeGetFileDetails(fileid);
                }
                if (flag == "CheckHotelCancellationCharges")
                {

                    soapEnvelopeXml = CreateSoapEnvelopeCheckHotelCancellationCharge(fileid, serviceid);
                }
                HttpWebRequest webRequest = CreateWebRequest(_url, _action);
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }

                    return soapResult;
                }
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
        }
        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }
        private static XDocument CreateSoapEnvelopeGetFileDetails(string fileid)
        {
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            DarinaCredentials _credential = new DarinaCredentials();
            AccountName = _credential.AccountName;
            UserName = _credential.UserName;
            Password = _credential.Password;
            AgentID = _credential.AgentID;
            Secret = _credential.Secret;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soap:Body>" +
                            "<GetFileDetails xmlns='http://travelcontrol.softexsw.us/'>" +
                             "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
                              "<FileID>" + fileid + "</FileID>" +
                            "</GetFileDetails>" +
                          "</soap:Body>" +
                        "</soap:Envelope>";



            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static XDocument CreateSoapEnvelopeCheckHotelCancellationCharge(string fileid, string serviceid)
        {
            #region Credentials
            string AccountName = string.Empty;
            string UserName = string.Empty;
            string Password = string.Empty;
            string AgentID = string.Empty;
            string Secret = string.Empty;
            DarinaCredentials _credential = new DarinaCredentials();
            AccountName = _credential.AccountName;
            UserName = _credential.UserName;
            Password = _credential.Password;
            AgentID = _credential.AgentID;
            Secret = _credential.Secret;
            #endregion
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
      "<soap:Body>" +
        "<CheckHotelCancellationCharges xmlns='http://travelcontrol.softexsw.us/'>" +
         "<SecStr>" + Secret + "</SecStr>" +
                             "<AccountName>" + AccountName + "</AccountName>" +
                             "<UserName>" + UserName + "</UserName>" +
                             "<Password>" + Password + "</Password>" +
                             "<AgentID>" + AgentID + "</AgentID>" +
          "<FileID>" + fileid + "</FileID>" +
          "<ServiceID>" + serviceid + "</ServiceID>" +
        "</CheckHotelCancellationCharges>" +
      "</soap:Body>" +
    "</soap:Envelope>";



            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static void InsertSoapEnvelopeIntoWebRequest(XDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
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