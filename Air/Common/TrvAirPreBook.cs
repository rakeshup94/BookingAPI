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
    public class TrvAirPreBook
    {
        #region Header's Parameters
        string username = string.Empty;
        string password = string.Empty;
        string AgentID = string.Empty;
        string ServiceType = string.Empty;
        string ServiceVersion = string.Empty;
        #endregion
        #region Air PreBooking (XML OUT for Travayoo)
        public XElement CreateAirPreBook(XElement req)
        {
            #region Air Response
            try
            {
                HeaderAuth headercheck = new HeaderAuth();
                username = req.Descendants("UserName").Single().Value;
                password = req.Descendants("Password").Single().Value;
                AgentID = req.Descendants("AgentID").Single().Value;
                ServiceType = req.Descendants("ServiceType").Single().Value;
                ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
                #region Air Price Check
                if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
                {
                    #region Air PreBook Response
                    try
                    {
                        int mystifly = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "12").Count();
                        int galileo = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "50").Count();
                        int tbo = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "51").Count();
                        if (mystifly == 1)
                        {
                            MystiAir_PriceCheck mysti = new MystiAir_PriceCheck();
                            XElement response = mysti.AirPriceCheck_mysti(req);
                            return response;
                        }
                        else if (galileo == 1)
                        {
                            gal_pricecheck reqobj = new gal_pricecheck();
                            XElement response = reqobj.pricecheckgal_response(req);
                            return response;
                        }
                        else if (tbo == 1)
                        {
                            tbo_pricecheck reqobj = new tbo_pricecheck();
                            XElement response = reqobj.pricechecktbo_response(req);
                            return response;
                        }
                        else
                        {
                            #region Supplier doesn't Exists
                            IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                                   new XElement("PreBookResponse",
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
                        IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                               new XElement("PreBookResponse",
                                   new XElement("ErrorTxt", ex.Message)
                                           )
                                       )
                      ));
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CreateAirPreBook";
                        ex1.PageName = "TrvAirPreBook";
                        ex1.CustomerID = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
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
                    IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                           new XElement("PreBookResponse",
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
                IEnumerable<XElement> request = req.Descendants("PreBookRequest");
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
                       new XElement("PreBookResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CreateAirPreBook";
                ex1.PageName = "TrvAirPreBook";
                ex1.CustomerID = req.Descendants("PreBookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("PreBookRequest").Attributes("TransID").FirstOrDefault().Value;
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