using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Transfer.Service;

namespace TravillioXMLOutService.Transfer.HotelBeds
{
    public class HB_TrnsfrBook
    {
        #region Transfer Book
        public XElement BookTransfer(XElement req)
        {
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            #region Transfer Book
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                try
                {
                    int hotelbeds = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "10").Count();
                    if (hotelbeds == 1)
                    {
                        //HB_TrnsfrBookDetail hbreq = new HB_TrnsfrBookDetail();
                        //XElement response = hbreq.transferbookhb(req);
                        //return response;

                        string customerId = req.Descendants("bookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                        HotelBedService hbreq = new HotelBedService(customerId);
                        XElement SearReq = req.Descendants("bookRequest").FirstOrDefault();
                        XElement response = hbreq.ConfirmRequest(SearReq);
                        SearReq.AddAfterSelf(response);
                        return req;
                    
                    }
                    else
                    {
                        #region Supplier doesn't Exists
                        IEnumerable<XElement> request = req.Descendants("bookRequest");
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
                               new XElement("bookResponse",
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
                    IEnumerable<XElement> request = req.Descendants("bookRequest");
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
                           new XElement("bookResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "BookTransfer";
                    ex1.PageName = "HB_TrnsfrBook";
                    ex1.CustomerID = req.Descendants("bookRequest").Attributes("CustomerID").FirstOrDefault().Value;
                    ex1.TranID = req.Descendants("bookRequest").Attributes("TransID").FirstOrDefault().Value;
                    APILog.SendCustomExcepToDB(ex1);
                    return searchdoc;
                    #endregion
                }
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("bookRequest");
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
                       new XElement("bookResponse",
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
              ));
                return searchdoc;
                #endregion
            }
            #endregion
        }
        #endregion
    }
}