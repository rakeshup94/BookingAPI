using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Galileo
{
    #region Fare Rule
    public class gal_farerule : IDisposable
    {
        #region Global Variable
        gal_supresponse api_resp;
        #endregion
        #region Gal Fare Rule Response (OUT)
        public XElement fareruleresponse_gal(XElement req)
        {
            try
            {
                api_resp = new gal_supresponse();
                string api_response = api_resp.gal_apiresponse(req, apirequest(req).ToString(), "AirFareRules", 13, req.Descendants("FareRuleRequest").FirstOrDefault().Attribute("TransID").Value, req.Descendants("FareRuleRequest").FirstOrDefault().Attribute("CustomerID").Value);
                XElement response = XElement.Parse(api_response);
                XElement supresp = RemoveAllNamespaces(response);
                XElement farerulelst = farerulebind(supresp, req);
                XElement farerulesresponse = travayooapiresponse(farerulelst, req);
                return farerulesresponse;
            }
            catch(Exception ex)
            {
                #region Exception
                string username = req.Descendants("UserName").FirstOrDefault().Value;
                string password = req.Descendants("Password").FirstOrDefault().Value;
                string AgentID = req.Descendants("AgentID").FirstOrDefault().Value;
                string ServiceType = req.Descendants("ServiceType").FirstOrDefault().Value;
                string ServiceVersion = req.Descendants("ServiceVersion").FirstOrDefault().Value;
                IEnumerable<XElement> request = req.Descendants("FareRuleRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "fareruleresponse_gal";
                ex1.PageName = "gal_farerule";
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
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "travayooapiresponse";
                ex1.PageName = "gal_farerule";
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
        private XElement farerulebind(XElement farerules, XElement req)
        {
            try
            {
                List<XElement> baglst = req.Descendants("Flight").ToList();
                List<XElement> farelst = farerules.Descendants("FareRule").ToList();
                List<XElement> baggagelst = new List<XElement>();
                List<XElement> farerulelst = new List<XElement>();
                if (baglst.Count() > 0)
                {
                    baggagelst = baggagebind(baglst);
                }
                if (farelst.Count() > 0)
                {
                    farerulelst = faredetailbind(farelst, req);
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
        private List<XElement> faredetailbind(List<XElement> fareruleslst, XElement req)
        {
            try
            {
                List<XElement> fareslst = req.Descendants("FlightSegments").ToList();
                List<XElement> faredetlst = new List<XElement>();
                for (int i = 0; i < fareruleslst.Count(); i++)
                {
                    List<XElement> rulelst = fareruleslst[i].Descendants("FareRuleLong").ToList();
                    faredetlst.Add(new XElement("FareRule",
                               new XElement("Airline", Convert.ToString(fareslst[i].Descendants("Flight").FirstOrDefault().Attribute("Operatingairlinecode").Value)),
                               new XElement("City", Convert.ToString(fareslst[i].Descendants("Flight").FirstOrDefault().Attribute("from").Value + fareslst[i].Descendants("Flight").LastOrDefault().Attribute("to").Value)),
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
                    if (faredetailslst[i].Attribute("Category").Value == "0")
                    {
                        title = "APPLICATION AND OTHER CONDITIONS";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "2")
                    {
                        title = "DAY/TIME";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "3")
                    {
                        title = "SEASONALITY";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "4")
                    {
                        title = "FLIGHT APPLICATION";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "5")
                    {
                        title = "ADVANCE RES/TICKETING";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "6")
                    {
                        title = "MINIMUM STAY";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "7")
                    {
                        title = "MAXIMUM STAY";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "8")
                    {
                        title = "STOPOVERS";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "9")
                    {
                        title = "TRANSFERS";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "10")
                    {
                        title = "PERMITTED COMBINATIONS";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "12")
                    {
                        title = "SURCHARGES";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "15")
                    {
                        title = "SALES RESTRICTIONS";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "16")
                    {
                        title = "PENALTIES";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "17")
                    {
                        title = "HIP/MILEAGE EXCEPTIONS";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "18")
                    {
                        title = "TICKET ENDORSEMENT";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "19")
                    {
                        title = "CHILDREN DISCOUNTS";
                    }
                    else if (faredetailslst[i].Attribute("Category").Value == "31")
                    {
                        title = "VOLUNTARY CHANGES";
                    }
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
        #region Fare Rule Request
        private XElement apirequest(XElement req)
        {
            #region Request
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace ns = "http://www.travelport.com/schema/air_v42_0";
            XNamespace ns1 = "http://www.travelport.com/schema/common_v42_0";
            List<XElement> farerule = new List<XElement>();
            try
            {
                XElement farerulst = req.Descendants("iternary").FirstOrDefault();
                var query = (from e in req.Descendants("iternary").FirstOrDefault().Descendants("Flight")
                             select e).GroupBy(e => e.Attribute("FareInfoValue").Value).Select(x => x.First());
                foreach (XElement ele in query.ToList())
                {
                    farerule.Add(new XElement(ns + "FareRuleKey",
                        new XAttribute("FareInfoRef", ele.Attribute("FareInfoRef").Value),
                        new XAttribute("ProviderCode", "1G"), ele.Attribute("FareInfoValue").Value));
                }
            }
            catch { }
            XElement common_request = new XElement(soap + "Envelope",
                                        new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                        new XElement(soap + "Body",
                                        new XElement(ns + "AirFareRulesReq",
                                            new XAttribute("AuthorizedBy", "User"),
                                            new XAttribute("TraceId", "sk123"),
                                            new XAttribute("TargetBranch", "P7109079"),
                                            new XElement(ns1 + "BillingPointOfSaleInfo",
                                                new XAttribute("OriginApplication", "uAPI")),
                                               farerule
                                                )));
            return common_request;
            #endregion
        }
        #endregion
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
        ~gal_farerule()
        {
            Dispose(false);
        }
        #endregion
    }
    #endregion
}