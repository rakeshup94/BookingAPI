using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Air.Models.TBO;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.TBO
{
    public class tbo_farerule : IDisposable 
    {
        #region Global Variable
        tboair_supresponse api_resp;
        public string countrycode = string.Empty;
        public string countryname = string.Empty;
        public XElement airlinexml;
        public XElement airportxml;
        #endregion
        #region TBO Fare Rule Response (OUT)
        public XElement fareruleresponse_tbo(XElement req)
        {
            try
            {
                try
                {
                    airportxml = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Flight\Mystifly\airportlist.xml")); 
                    XElement record = airportxml.Descendants("record").Where(x => x.Descendants("AirportCode").FirstOrDefault().Value == req.Descendants("FareRuleRequest").Descendants("Itinerary").Descendants("Origin").FirstOrDefault().Value).FirstOrDefault();
                    countrycode = record.Descendants("countryCode").FirstOrDefault().Value;
                    countryname = record.Descendants("countryName").FirstOrDefault().Value;
                }
                catch { }
                api_resp = new tboair_supresponse();
                string api_response = api_resp.tbo_supresponse(req, fltfarerulerequest(req).ToString(), "AirFareRules", 13, req.Descendants("FareRuleRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("FareRuleRequest").FirstOrDefault().Attribute("CustomerID").Value,null);
                var xml = XDocument.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.ASCII.GetBytes(api_response), new XmlDictionaryReaderQuotas()));
                XElement flights = xml.Descendants("FareRules").FirstOrDefault();
                if (xml.Descendants("IsSuccess").FirstOrDefault().Value == "true")
                {
                    XElement farerulelst = farerulebind(flights);
                    XElement farerulesresponse = travayooapiresponse(farerulelst, req);
                    return farerulesresponse;
                }
                else
                {
                    string username = req.Descendants("UserName").FirstOrDefault().Value;
                    string password = req.Descendants("Password").FirstOrDefault().Value;
                    string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                    string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                    string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                    IEnumerable<XElement> request = req.Descendants("FareRuleRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
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
                                          new XElement("FareRuleResponse",
                                              new XElement("ErrorTxt", "No Fare Rule Found")
                                 ))));
                    return searchdoc;
                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "fareruleresponse_tbo";
                ex1.PageName = "tbo_farerule";
                ex1.CustomerID = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("FareRuleRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";                
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
                                      new XElement("FareRuleResponse",
                                          new XElement("ErrorTxt", ex.Message)
                             ))));
                return searchdoc;
                #endregion
            }
        }
        #endregion
        #region response binding
        private XElement travayooapiresponse(XElement fareresponse, XElement req)
        {
            string username = req.Descendants("UserName").FirstOrDefault().Value;
            string password = req.Descendants("Password").FirstOrDefault().Value;
            string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
            string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
            IEnumerable<XElement> request = req.Descendants("FareRuleRequest");
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            try
            {
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
                                      new XElement("FareRuleResponse",
                                          new XElement("Flights",
                                              fareresponse
                                              )
                             ))));
                return searchdoc;
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "travayooapiresponse";
                ex1.PageName = "tbo_farerule";
                ex1.CustomerID = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
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
                                      new XElement("FareRuleResponse",
                                          new XElement("ErrorTxt", ex.Message)
                             ))));
                return searchdoc;
                #endregion
            }
        }
        #endregion
        #region Fare Rules Binding
        private XElement farerulebind(XElement farerules)
        {
            try
            {
                //List<XElement> baglst = req.Descendants("Flight").ToList();
                List<XElement> farelst = farerules.Element("item").Elements("item").ToList();
                List<XElement> baggagelst = new List<XElement>();
                List<XElement> farerulelst = new List<XElement>();
                // if (baglst.Count() > 0)
                {
                    //baggagelst = baggagebind(baglst);
                }
                if (farelst.Count() > 0)
                {
                    farerulelst = faredetailbind(farelst);
                }
                XElement responsedoc = new XElement(
                    new XElement("FareRulesResult",
                        baggagelst,
                        farerulelst
                        ));

                return responsedoc;
            }
            catch { return null; }
        }
        #endregion
        #region Baggage Binding
        private List<XElement> baggagebind(List<XElement> baggagelist)
        {
            try
            {
                List<XElement> baggagelst = new List<XElement>();
                for (int i = 0; i < baggagelist.Count(); i++)
                {
                    baggagelst.Add(new XElement("BaggageInfo",
                               new XElement("Departure", Convert.ToString(baggagelist[i].Attribute("from").Value)),
                               new XElement("Arrival", Convert.ToString(baggagelist[i].Attribute("to").Value)),
                               new XElement("Baggage", Convert.ToString(baggagelist[i].Attribute("Baggage").Value)),
                               new XElement("FlightNo", Convert.ToString(baggagelist[i].Attribute("airlinenumber").Value))
                               )
                        );
                }
                return baggagelst;
            }
            catch { return null; }
        }
        #endregion
        #region Fare Details Binding
        private List<XElement> faredetailbind(List<XElement> fareruleslst)
        {
            try
            {
                //List<XElement> fareslst = req.Descendants("FlightSegments").ToList();
                List<XElement> faredetlst = new List<XElement>();
                for (int i = 0; i < fareruleslst.Count(); i++)
                {
                    List<XElement> rulelst = fareruleslst[i].Descendants("FareRuleDetail").ToList();
                    faredetlst.Add(new XElement("FareRule",
                               new XElement("Airline", fareruleslst[i].Element("Airline").Value),
                               new XElement("City", fareruleslst[i].Element("Origin").Value + fareruleslst[i].Element("Destination").Value),
                               faredetails(rulelst)
                               )
                        );
                }
                return faredetlst;
            }
            catch { return null; }
        }
        #endregion
        #region Fare Details Binding
        private List<XElement> faredetails(List<XElement> faredetailslst)
        {
            try
            {
                List<XElement> faredetlst = new List<XElement>();
                for (int i = 0; i < faredetailslst.Count(); i++)
                {
                    #region Title
                    string title = string.Empty;
                    title = "PENALTIES";
                    #endregion
                    faredetlst.Add(new XElement("RuleDetails",
                                   new XAttribute("title", Convert.ToString(title)),
                                   Convert.ToString(faredetailslst[i].Value))
                        );
                }
                return faredetlst;
            }
            catch { return null; }
        }
        #endregion
        #region TBO Air Search Request
        private string fltfarerulerequest(XElement req)
        {
            string request = string.Empty;
            try
            {
                XElement iternary = req.Descendants("Itinerary").FirstOrDefault();
                tbocommonreq airreq = new tbocommonreq();
                airreq.IPAddress = "49.205.173.6";
                airreq.EndUserBrowserAgent = "Mozilla/5.0(Windows NT 6.1)";
                airreq.PointOfSale = countrycode;
                airreq.RequestOrigin = countryname;
                airreq.TokenId = "f360ef32-07fc-4b80-8b86-358fcfb95f61";
                airreq.TrackingId = iternary.Descendants("SessionId").FirstOrDefault().Value;
                airreq.ResultId = iternary.Descendants("faresoucecode").FirstOrDefault().Value;
                request = JsonConvert.SerializeObject(airreq);
                return request;
            }
            catch { return ""; }
        }
        #endregion
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;
            if (disposing)
            {
                // Free any other managed objects here.
            }
            disposed = true;
        }
        ~tbo_farerule()
        {
            Dispose(false);
        }
        #endregion
    }
}