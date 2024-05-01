using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Mystifly
{
    public class MystiAir_FareRule
    {
        #region Travayoo Fare Rules of Air (Mystifly)
        public XElement AirFareRule_mysti(XElement req)
        {
            try
            {
                string url = string.Empty;
                string response = string.Empty;
                XElement suppliercred = airsupplier_Cred.getgds_credentials(req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value, "12");
                url = suppliercred.Descendants("URL").FirstOrDefault().Value;
                string method = suppliercred.Descendants("AirFareRules").FirstOrDefault().Value;
                string Target = suppliercred.Descendants("Mode").FirstOrDefault().Value;
                string sessionid = suppliercred.Descendants("sessionid").FirstOrDefault().Value;
                string apireq = apirequest(req, Target, sessionid);
                Mysti_SupplierResponse sup_response = new Mysti_SupplierResponse();
                string customerid = string.Empty;
                string trackno = string.Empty;
                customerid = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                trackno = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                response = sup_response.supplierresponse_mystifly(url, apireq, method, "AirFareRules", 13, trackno, customerid).ToString();
                XElement availrsponse = XElement.Parse(response.ToString());
                XElement doc = RemoveAllNamespaces(availrsponse);
                XElement farerulelst = farerulebind(doc);
                XElement farerulesresponse = travayooapiresponse(farerulelst, req);
                return farerulesresponse;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "AirFareRule_mysti";
                ex1.PageName = "MystiAir_FareRule";
                ex1.CustomerID = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
        #endregion
        #region Fare Rules Binding
        public XElement farerulebind(XElement farerules)
        {
            try
            {
                List<XElement> baglst = farerules.Descendants("BaggageInfo").ToList();
                List<XElement> farelst = farerules.Descendants("FareRule").ToList();
                List<XElement> baggagelst = new List<XElement>();
                List<XElement> farerulelst = new List<XElement>();
                if (baglst.Count() > 0)
                {
                    baggagelst = baggagebind(baglst);
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
        public List<XElement> baggagebind(List<XElement> baggagelist)
        {
            try
            {
                List<XElement> baggagelst = new List<XElement>();
                for (int i = 0; i < baggagelist.Count(); i++)
                {
                    baggagelst.Add(new XElement("BaggageInfo",
                               new XElement("Departure", Convert.ToString(baggagelist[i].Descendants("Departure").FirstOrDefault().Value)),
                               new XElement("Arrival", Convert.ToString(baggagelist[i].Descendants("Arrival").FirstOrDefault().Value)),
                               new XElement("Baggage", Convert.ToString(baggagelist[i].Descendants("Baggage").FirstOrDefault().Value)),
                               new XElement("FlightNo", Convert.ToString(baggagelist[i].Descendants("FlightNo").FirstOrDefault().Value))
                               )
                        );
                }
                return baggagelst;
            }
            catch { return null; }
        }
        #endregion
        #region Fare Details Binding
        public List<XElement> faredetailbind(List<XElement> fareruleslst)
        {
            try
            {
                List<XElement> faredetlst = new List<XElement>();
                for (int i = 0; i < fareruleslst.Count(); i++)
                {
                    List<XElement> rulelst = fareruleslst[i].Descendants("RuleDetail").ToList();
                    faredetlst.Add(new XElement("FareRule",
                               new XElement("Airline", Convert.ToString(fareruleslst[i].Descendants("Airline").FirstOrDefault().Value)),
                               new XElement("City", Convert.ToString(fareruleslst[i].Descendants("CityPair").FirstOrDefault().Value)),
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
        public List<XElement> faredetails(List<XElement> faredetailslst)
        {
            try
            {
                List<XElement> faredetlst = new List<XElement>();
                for (int i = 0; i < faredetailslst.Count(); i++)
                {
                    faredetlst.Add(new XElement("RuleDetails",
                                   new XAttribute("title", Convert.ToString(faredetailslst[i].Descendants("Category").FirstOrDefault().Value)),
                                   Convert.ToString(faredetailslst[i].Descendants("Rules").FirstOrDefault().Value))
                        );
                }
                return faredetlst;
            }
            catch { return null; }
        }
        #endregion
        #region api response
        public XElement travayooapiresponse(XElement fareresponse, XElement req)
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
                                      new XElement("Flights",
                                          fareresponse
                                          )
                         ))));
            return searchdoc;
        }
        #endregion
        #region API Request
        public string apirequest(XElement req,string mode,string sessionid)
        {
            string faresourcecode = string.Empty;
            //string sessionid = string.Empty;
            faresourcecode = req.Descendants("faresoucecode").FirstOrDefault().Value;
            manage_session session_mgmt = new manage_session();
            //sessionid = session_mgmt.session_manage(req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value, req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value);
            string request = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:mys='Mystifly.OnePoint' xmlns:mys1='http://schemas.datacontract.org/2004/07/Mystifly.OnePoint.AirRules1_1'>" +
                              "<soapenv:Header/>" +
                              "<soapenv:Body>" +
                                "<mys:FareRules1_1>" +
                                  "<mys:rq>" +
                                    "<mys1:FareSourceCode>" + faresourcecode + "</mys1:FareSourceCode>" +
                                    "<mys1:SessionId>" + sessionid + "</mys1:SessionId>" +
                                    "<mys1:Target>" + mode + "</mys1:Target>" +
                                  "</mys:rq>" +
                                "</mys:FareRules1_1>" +
                              "</soapenv:Body>" +
                            "</soapenv:Envelope>";
            return request;
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
    }
}