using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Galileo;
using TravillioXMLOutService.Air.Mystifly;
using TravillioXMLOutService.Air.TBO;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Common
{
    public class TrvAirFareRule
    {
        #region Header's Parameters
        string username = string.Empty;
        string password = string.Empty;
        string AgentID = string.Empty;
        string ServiceType = string.Empty;
        string ServiceVersion = string.Empty;
        #endregion
        #region Air Fare Rules (XML OUT for Travayoo)
        public XElement CreateAirFareRules(XElement req)
        {
            #region Air Fare Rules Response
            try
            {
                HeaderAuth headercheck = new HeaderAuth();
                username = req.Descendants("UserName").Single().Value;
                password = req.Descendants("Password").Single().Value;
                AgentID = req.Descendants("AgentID").Single().Value;
                ServiceType = req.Descendants("ServiceType").Single().Value;
                ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                #region Air Fare Rules Check
                if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
                {
                    #region Air Fare Rules Response
                    try
                    {
                        int mystifly = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "12").Count();
                        int galileo = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "50").Count();
                        int tbo = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "51").Count();
                        if (mystifly == 1)
                        {
                            MystiAir_FareRule mysti = new MystiAir_FareRule();
                            XElement response = mysti.AirFareRule_mysti(req);
                            return response;
                        }
                        else if (galileo == 1)
                        {
                            gal_farerule galreqobj = new gal_farerule();
                            XElement response = galreqobj.fareruleresponse_gal(req);
                            return response;
                        }
                        else if (tbo == 1)
                        {
                            tbo_farerule reqobj = new tbo_farerule();
                            XElement response = reqobj.fareruleresponse_tbo(req);
                            return response;
                        }
                        else
                        {
                            #region Supplier doesn't Exists
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
                                       new XElement("ErrorTxt", "Supplier doesn't Exists.")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                    }
                    catch (Exception ex)
                    {
                        #region Exception
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
                                           )
                                       )
                      ));
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CreateAirFareRules";
                        ex1.PageName = "TrvAirFareRule";
                        ex1.CustomerID = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                        SaveAPILog saveex = new SaveAPILog();
                        saveex.SendCustomExcepToDB(ex1);
                        return searchdoc;
                        #endregion
                    }
                    #endregion
                }
                else
                {
                    #region Invalid Credential
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
                               new XElement("ErrorTxt", "Invalid Credentials")
                                       )
                                   )
                  ));
                    return searchdoc;
                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                #region Exception
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
                                   )
                               )
              ));
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CreateAirFareRules";
                ex1.PageName = "TrvAirFareRule";
                ex1.CustomerID = req.Descendants("FareRuleRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("FareRuleRequest").Attributes("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return searchdoc;
                #endregion
            }
            #endregion
        }
        #endregion
    }
}