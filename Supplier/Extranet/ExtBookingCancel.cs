using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.Extranet
{
    public class ExtBookingCancel
    {
        List<XElement> hotelcancellationextranet;
        #region Cancellation Policies of Extranet (XML OUT for Travayoo)
        public XElement CancelBooking_Extranet(XElement req)
        {
            #region Extranet
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;

            List<XElement> doc1 = new List<XElement>();
            HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
            #region Credentials
            string exAgentID = string.Empty;
            string exUserName = string.Empty;
            string exPassword = string.Empty;
            string exServiceType = string.Empty;
            string exServiceVersion = string.Empty;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "3");
            exAgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            exUserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            exPassword = suppliercred.Descendants("Password").FirstOrDefault().Value;
            exServiceType = suppliercred.Descendants("ServiceType").FirstOrDefault().Value;
            exServiceVersion = suppliercred.Descendants("ServiceVersion").FirstOrDefault().Value;
            #endregion
            string requestxml = string.Empty;
            requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                            "<Authentication>" +
                              "<AgentID>" + exAgentID + "</AgentID>" +
                                  "<UserName>" + exUserName + "</UserName>" +
                                  "<Password>" + exPassword + "</Password>" +
                                  "<ServiceType>" + exServiceType + "</ServiceType>" +
                                  "<ServiceVersion>" + exServiceVersion + "</ServiceVersion>" +
                            "</Authentication>" +
                          "</soapenv:Header>" +
                          "<soapenv:Body>" +
                            "<HotelCancellationRequest>" +
                              "<Response_Type>XML</Response_Type>" +
                              "<ConfirmationNumber>" + req.Descendants("ConfirmationNumber").Single().Value + "</ConfirmationNumber>" +
                            "</HotelCancellationRequest>" +
                          "</soapenv:Body>" +
                        "</soapenv:Envelope>";

            try
            {
                object result = extclient.MakeCancelBookingRequestByXML(requestxml);
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = req.Descendants("TransID").Single().Value;
                        log.LogTypeID = 6;
                        log.LogType = "Cancel";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = doc.ToString();
                        SaveAPILog savelog = new SaveAPILog();
                        savelog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CancelBooking_Extranet";
                        ex1.PageName = "ExtBookingCancel";
                        ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                        ex1.TranID = req.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                    }
                    hotelcancellationextranet = doc.Descendants("Hotel").ToList();
                }
                else
                {

                    hotelcancellationextranet = doc1.Descendants("Hotel").ToList();
                }
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancelBooking_Extranet";
                ex1.PageName = "ExtBookingCancel";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                hotelcancellationextranet = doc1.Descendants("Hotel").ToList();
            }
            if (hotelcancellationextranet.Count() > 0)
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
                                   new XElement("Rooms",
                                       new XElement("Room",
                                           new XElement("Cancellation",
                                               new XElement("Amount", Convert.ToString(hotelcancellationextranet[0].Descendants("CXLAmount").Single().Value)),
                                               new XElement("Status", Convert.ToString(hotelcancellationextranet[0].Descendants("Status").Single().Value))
                                               )
                                           )
                                       )
                      ))));
                return cancellationdoc;
            }
            else
            {
                #region Exception
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
                           new XElement("ErrorTxt", "Server not responding")
                                   )
                               )
              ));
                return cancellationdoc;
                #endregion
            }
            #endregion        
        }
        #endregion
    }
}