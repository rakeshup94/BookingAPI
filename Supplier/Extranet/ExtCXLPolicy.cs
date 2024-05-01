using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.Extranet
{
    
    public class ExtCXLPolicy
    {
        XElement reqTravayoo;
        #region Cancellation Policies of Extranet (XML OUT for Travayoo)
        public XElement GetCXLPolicyExtranet(XElement req)
        {
            reqTravayoo = req;
            XElement cxlpolicyresponse = null;
            XElement cxlpolicy = null;
            IEnumerable<XElement> request = req.Descendants("hotelcancelpolicyrequest").ToList();
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            try
            {
                HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
                #region Request
                List<XElement> getroom = reqTravayoo.Descendants("Room").ToList();
                XElement occupancyrequest = new XElement(
                        new XElement("Keys", getroomkey(getroom)));
                string requestxml = string.Empty;
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
                                "<cancelPolicyRequest>" +
                                  "<Response_Type>XML</Response_Type>" +
                                  "<CustomerID>" + reqTravayoo.Descendants("CustomerID").FirstOrDefault().Value + "</CustomerID>" +
                                  "<RequestID>" + reqTravayoo.Descendants("TransID").FirstOrDefault().Value + "</RequestID>" +
                                   "<FromDate>" + reqTravayoo.Descendants("FromDate").FirstOrDefault().Value + "</FromDate>" +
                                  "<ToDate>" + reqTravayoo.Descendants("ToDate").FirstOrDefault().Value + "</ToDate>" +
                                  "<PropertyId>" + reqTravayoo.Descendants("RoomTypes").FirstOrDefault().Attribute("HtlCode").Value + "</PropertyId>" +
                                  occupancyrequest +
                                  "<CultureId>1</CultureId>" +
                                  "<GuestNationalityId>" + reqTravayoo.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value + "</GuestNationalityId>" +
                                  reqTravayoo.Descendants("Rooms").SingleOrDefault().ToString() +
                                "</cancelPolicyRequest>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
                #endregion
                #region Response
                object result = extclient.GetCancelPolicyByXML(requestxml);
                if (result != null)
                {
                    
                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravayoo.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                        log.LogTypeID = 3;
                        log.LogType = "CXLPolicy";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = result.ToString();
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "GetCXLPolicyExtranet";
                        ex1.PageName = "ExtCXLPolicy";
                        ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                        ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                    }
                    try
                    {
                        XElement doc = XElement.Parse(result.ToString());
                        List<XElement> hotelavailabilityres = doc.Descendants("Hotel").ToList();
                        cxlpolicyresponse = GetHotelListExtranet(hotelavailabilityres).FirstOrDefault();
                        #region XML OUT
                        cxlpolicy = new XElement(
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
                                                   new XElement("HotelDetailwithcancellationResponse",
                                                       new XElement("Hotels",
                                                           cxlpolicyresponse
                                          )))));

                        #endregion
                    }
                    catch(Exception ex)
                    {
                        #region Exception
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "GetCXLPolicyExtranet";
                        ex1.PageName = "ExtCXLPolicy";
                        ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                        ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        #endregion
                        XElement searchdoc = new XElement(
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
                                   new XElement("HotelDetailwithcancellationResponse",
                                       new XElement("ErrorTxt", "No Policy Found")
                                               )
                                           )
                          ));
                        return searchdoc;
                        #endregion
                    }
                }
                else
                {
                    #region No Result

                    try
                    {
                        APILogDetail log = new APILogDetail();
                        log.customerID = Convert.ToInt64(reqTravayoo.Descendants("CustomerID").Single().Value);
                        log.TrackNumber = reqTravayoo.Descendants("TransID").Single().Value;
                        log.LogTypeID = 3;
                        log.LogType = "CXLPolicy";
                        log.SupplierID = 3;
                        log.logrequestXML = requestxml.ToString();
                        log.logresponseXML = result.ToString();
                        APILog.SaveAPILogs(log);
                    }
                    catch (Exception ex)
                    {
                        #region Exception
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "GetCXLPolicyExtranet";
                        ex1.PageName = "ExtCXLPolicy";
                        ex1.CustomerID = reqTravayoo.Descendants("CustomerID").Single().Value;
                        ex1.TranID = reqTravayoo.Descendants("TransID").Single().Value;
                        APILog.SendCustomExcepToDB(ex1);
                        #endregion
                    }

                    XElement searchdoc = new XElement(
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
                                   new XElement("HotelDetailwithcancellationResponse",
                                       new XElement("ErrorTxt", "No Policy Found")
                                               )
                                           )
                          ));
                    return searchdoc;
                    #endregion
                }
                #endregion
                return cxlpolicy;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "GetCXLPolicyExtranet";
                ex1.PageName = "ExtCXLPolicy";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransactionID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                XElement searchdoc = new XElement(
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
                                   new XElement("HotelDetailwithcancellationResponse",
                                       new XElement("ErrorTxt", "No Policy Found")
                                               )
                                           )
                          ));
                return searchdoc;

                #endregion
            }
        }
        #endregion
        #region Extranet
        #region Extranet Hotel Listing
        private IEnumerable<XElement> GetHotelListExtranet(List<XElement> htlist)
        {
            #region Extranet
            List<XElement> hotellst = new List<XElement>();
            Int32 length = htlist.Count();
            try
            {
                for (int i = 0; i < length; i++)
                {
                    hotellst.Add(new XElement("Hotel",
                                           new XElement("HotelID", Convert.ToString(htlist[i].Attribute("HotelID").Value)),
                                                       new XElement("HotelName", Convert.ToString(htlist[i].Attribute("HotelName").Value)),
                                                       new XElement("HotelImgSmall", Convert.ToString("")),
                                                       new XElement("HotelImgLarge", Convert.ToString("")),
                                                       new XElement("MapLink", ""),
                                                       new XElement("DMC", "Extranet"),
                                                       new XElement("Currency", ""),
                                                       new XElement("Offers", "")
                                                       , new XElement("Rooms",
                                                    GetHotelRoomListingExtranet(htlist[i].Descendants("CancelPolicy").ToList())
                                                   )
                    ));
                }
            }
            catch (Exception ex)
            {
                return hotellst;
            }
            return hotellst;
            #endregion
        }
        #endregion

        #region Extranet Hotel's Room Listing
        private IEnumerable<XElement> GetHotelRoomListingExtranet(List<XElement> roomlist)
        {
            List<XElement> str = new List<XElement>();
            #region CXL Policy
            str.Add(new XElement("Room",
                 new XAttribute("ID", Convert.ToString("")),
                 new XAttribute("RoomType", Convert.ToString("")),
                 new XAttribute("MealPlanPrice", ""),
                 new XAttribute("PerNightRoomRate", Convert.ToString("")),
                 new XAttribute("TotalRoomRate", Convert.ToString("")),
                 new XAttribute("CancellationDate", ""),
                 new XElement("CancellationPolicies",
             GetRoomCancellationPolicyExtranet(roomlist))
                 ));
            #endregion
            return str;

        }
        #endregion

        #region Room's Cancellation Policies from Extranet
        private IEnumerable<XElement> GetRoomCancellationPolicyExtranet(List<XElement> cancellationpolicy)
        {
            #region Room's Cancellation Policies from Extranet
            List<XElement> htrm = new List<XElement>();
            
            for (int i = 0; i < cancellationpolicy.Count(); i++)
            {
                string currencycode = string.Empty;
                try
                { currencycode = cancellationpolicy[i].Attribute("Currency").Value; }
                catch { }
                htrm.Add(new XElement("CancellationPolicy", "Cancellation done on after " + cancellationpolicy[i].Attribute("RefundDate").Value + "  will apply " + currencycode + " " + cancellationpolicy[i].Attribute("RefundPriceEffective").Value + "  Cancellation fee"
                    , new XAttribute("LastCancellationDate", Convert.ToString(cancellationpolicy[i].Attribute("RefundDate").Value))
                    , new XAttribute("ApplicableAmount", cancellationpolicy[i].Attribute("RefundPriceEffective").Value)
                     , new XAttribute("RefundValue", cancellationpolicy[i].Attribute("RefundValue") == null ? null : cancellationpolicy[i].Attribute("RefundValue").Value)
                      , new XAttribute("MarkUpInBreakUps", cancellationpolicy[i].Attribute("MarkUpInBreakUps") == null ? null : cancellationpolicy[i].Attribute("MarkUpInBreakUps").Value)
                       , new XAttribute("MarkUpApplied", cancellationpolicy[i].Attribute("MarkUpApplied") == null ? null : cancellationpolicy[i].Attribute("MarkUpApplied").Value)
                    , new XAttribute("NoShowPolicy", "0")));
            };
            return htrm;
            #endregion
        }
                
        #endregion

        #endregion
        #region CXL Policy's Keys
        private List<XElement> getroomkey(List<XElement> room)
        {
            #region Bind Room keys
            List<XElement> str = new List<XElement>();

            for (int i = 0; i < room.Count(); i++)
            {
                str.Add(new XElement("Key", Convert.ToString(room[i].Attribute("SessionID").Value))
                );
            }
            return str;
            #endregion
        }
        #endregion
    }
}