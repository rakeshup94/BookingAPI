using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.HotelsPro;

namespace TravillioXMLOutService.App_Code
{
    public class HotelsProCancellation
    {
        XElement reqTravayoo;       
        #region Hotel Availability of HotelsPro (XML OUT for Travayoo)
        public XElement HotelroomCancellationHotelsPro(XElement req)
        {
            XElement hotelCancellation = null;
            XElement cancellationdoc = null;
            try
            {
                string url = string.Empty;
                #region Credentials
                XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "6");
                url = suppliercred.Descendants("cancelendpoint").FirstOrDefault().Value;
                #endregion
                string bookingnumber = req.Descendants("ConfirmationNumber").Single().Value;
                url = url+"/" + bookingnumber + "";
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);

                string usernamep = string.Empty;
                string passwordp = string.Empty;

                //HotelsProCredentials _credential = new HotelsProCredentials();
                //usernamep = _credential.username;
                //passwordp = _credential.password;
                usernamep = suppliercred.Descendants("username").FirstOrDefault().Value;
                passwordp = suppliercred.Descendants("password").FirstOrDefault().Value;

                string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(usernamep + ":" + passwordp));
                webRequest.Headers.Add("Authorization", "Basic " + svcCredentials);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = 0;
                webRequest.Method = "POST";
                webRequest.Host = suppliercred.Descendants("host").FirstOrDefault().Value;

                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                        try
                        {
                            string suprequest = url;
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt32(req.Descendants("CustomerID").Single().Value);
                            log.TrackNumber = req.Descendants("TransID").Single().Value;
                            log.LogTypeID = 6;
                            log.LogType = "Cancel";
                            log.SupplierID = 6;
                            log.logrequestXML = suprequest.ToString();
                            log.logresponseXML = soapResult.ToString();
                            //APILog.SaveAPILogs(log);
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SaveAPILogs(log);
                        }
                        catch (Exception exx)
                        {
                            CustomException ex1 = new CustomException(exx);
                            ex1.MethodName = "HotelroomCancellationHotelsPro";
                            ex1.PageName = "HotelsProCancellation";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            //APILog.SendCustomExcepToDB(ex1);
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                    }
                    XmlDocument doc = (XmlDocument)JsonConvert.DeserializeXmlNode(soapResult, "CancellationResponse");
                    dynamic cxlresponse = Newtonsoft.Json.JsonConvert.DeserializeObject(soapResult);
                    
                   
                    #region Cancellation Response

                    string username = req.Descendants("UserName").Single().Value;
                    string password = req.Descendants("Password").Single().Value;
                    string AgentID = req.Descendants("AgentID").Single().Value;
                    string ServiceType = req.Descendants("ServiceType").Single().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                    string status = string.Empty;                    
                    status = "Success";
                    string amount = string.Empty;
                    try
                    {
                        amount = cxlresponse.charge_amount;
                    }
                    catch { }
                    #region XML OUT CXL
                    IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    cancellationdoc = new XElement(
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
                                       new XElement("Rooms",
                                           new XElement("Room",
                                               new XElement("Cancellation",
                                                   new XElement("Amount", Convert.ToString(amount)),
                                                   new XElement("Status", Convert.ToString(status))
                                                   )
                                               )
                                           )
                          ))));

                    #endregion
                    #endregion

                }
                return cancellationdoc;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "HotelroomCancellationHotelsPro";
                ex1.PageName = "HotelsProCancellation";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                //APILog.SendCustomExcepToDB(ex1);
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return hotelCancellation;
            }

        }
        #endregion
    }
}