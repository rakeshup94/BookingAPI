using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Air.Galileo;
using TravillioXMLOutService.Air.Models.Common;
using TravillioXMLOutService.Air.Mystifly;
using TravillioXMLOutService.Air.TBO;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Air.Common
{
    public class TrvAirSearch
    {
        #region Header's Parameters
        string username = string.Empty;
        string password = string.Empty;
        string AgentID = string.Empty;
        string ServiceType = string.Empty;
        string ServiceVersion = string.Empty;
        #endregion
        #region Air Availability (XML OUT for Travayoo)
        public XElement CreateAirAvailability(XElement req)
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
                #region Air Availability
                if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
                {
                    #region Air Search Response
                    try
                    {
                        int mystifly = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "12").Count();
                        int galileo = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "50").Count();
                        int tbo = req.Descendants("supplierdetail").Where(x => x.Attribute("supplierid").Value == "51").Count();
                        if (mystifly > 0 || galileo > 0 || tbo > 0)
                        {
                            #region Supplier Credentials
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(airsupplier_Cred).TypeHandle);
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(air_staticData).TypeHandle);
                            #endregion
                            #region Merged
                            XElement mystiresponse = null;
                            XElement galresponse = null;
                            XElement tboresponse = null;
                            #region Thread Initialize
                            Thread tid1 = null;
                            Thread tid2 = null;
                            Thread tid3 = null;
                            if (mystifly == 1)
                            {
                                MystiAir_GetAvail mysti = new MystiAir_GetAvail();
                                tid1 = new Thread(new ThreadStart(() => { mystiresponse = mysti.AirAvailability_mysti(req); }));
                            }
                            if (galileo == 1)
                            {
                                gal_getavail request = new gal_getavail();
                                tid2 = new Thread(new ThreadStart(() => { galresponse = request.avail_response(req); }));
                            }
                            if (tbo == 1)
                            {
                                tbo_getavail request = new tbo_getavail();
                                tid3 = new Thread(new ThreadStart(() => { tboresponse = request.avail_response(req); }));
                            }
                            #endregion
                            #region Thread Start
                            if (mystifly == 1)
                            {
                                tid1.Start();
                            }
                            if (galileo == 1)
                            {
                                tid2.Start();
                            }
                            if (tbo == 1)
                            {
                                tid3.Start();
                            }
                            #endregion
                            #region Thread Join
                            if (mystifly == 1)
                            {
                                tid1.Join();
                            }
                            if (galileo == 1)
                            {
                                tid2.Join();
                            }
                            if (tbo == 1)
                            {
                                tid3.Join();
                            }
                            #endregion
                            #region Thread Abort
                            if (tid1 != null && tid1.IsAlive)
                                tid1.Abort();
                            if (tid2 != null && tid2.IsAlive)
                                tid2.Abort();
                            if (tid3 != null && tid3.IsAlive)
                                tid3.Abort();
                            #endregion
                            #region Merge all suppliers
                            List<XElement> listmysti = null;
                            List<XElement> listgal = null;
                            List<XElement> listtbo = null;
                            int galtrue = 0;
                            int mystitrue = 0;
                            int tbotrue = 0;
                            string sessionid = string.Empty;
                            #region Mystifly
                            if (mystifly == 1)
                            {
                                if (mystiresponse != null)
                                {
                                    if (mystiresponse.Descendants("SearchResponse").Descendants("Itinerary").ToList().Count() > 0)
                                    {
                                        try
                                        {
                                            listmysti = mystiresponse.Descendants("SearchResponse").Descendants("Itinerary").ToList();
                                            mystitrue = 1;
                                        }
                                        catch { }
                                    }
                                }
                            }
                            #endregion
                            #region Gal
                            if (galileo == 1)
                            {
                                if (galresponse != null)
                                {
                                    if (galresponse.Descendants("SearchResponse").Descendants("Itinerary").ToList().Count() > 0)
                                    {
                                        try
                                        {
                                            listgal = galresponse.Descendants("SearchResponse").Descendants("Itinerary").ToList();
                                            galtrue = 1;
                                        }
                                        catch { }
                                    }
                                }
                            }
                            #endregion
                            #region TBO
                            if (tbo == 1)
                            {
                                if (tboresponse != null)
                                {
                                    if (tboresponse.Descendants("SearchResponse").Descendants("Itinerary").ToList().Count() > 0)
                                    {
                                        try
                                        {
                                            listtbo = tboresponse.Descendants("SearchResponse").Descendants("Itinerary").ToList();
                                            tbotrue = 1;
                                            sessionid = tboresponse.Descendants("SearchResponse").Descendants("Flights").FirstOrDefault().Attribute("SessionId").Value;
                                        }
                                        catch { }
                                    }
                                }
                            }
                            #endregion
                            IEnumerable<XElement> comrequest = req.Descendants("SearchRequest");
                            XNamespace trsoapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            XElement trsearchdoc = new XElement(
                                             new XElement(trsoapenv + "Envelope",
                                                       new XAttribute(XNamespace.Xmlns + "soapenv", trsoapenv),
                                                       new XElement(trsoapenv + "Header",
                                                        new XAttribute(XNamespace.Xmlns + "soapenv", trsoapenv),
                                                        new XElement("Authentication",
                                                            new XElement("AgentID", AgentID),
                                                            new XElement("UserName", username),
                                                            new XElement("Password", password),
                                                            new XElement("ServiceType", ServiceType),
                                                            new XElement("ServiceVersion", ServiceVersion))),
                                                        new XElement(trsoapenv + "Body",
                                                            new XElement(comrequest.Single()),
                                                  new XElement("SearchResponse",
                                                      new XElement("Flights",
                                                          new XAttribute("SessionId", sessionid),
                                                          listmysti, listgal, listtbo
                                                          )
                                         ))));

                            if ((mystitrue == 1 && galtrue == 1) || (mystitrue == 1 && tbotrue == 1) || (galtrue == 1 && tbotrue == 1))
                            {
                                int index = 1;
                                foreach(XElement iter in trsearchdoc.Descendants("SearchResponse").Descendants("Itinerary").ToList())
                                {
                                    iter.SetAttributeValue("SequenceNumber", Convert.ToString(index));
                                    index++;
                                }
                                //trsearchdoc.Descendants("SearchResponse").Descendants("Itinerary").OrderBy(x => (decimal)x.Attribute("amount")).ToList();
                            }
                            return trsearchdoc;
                            #endregion
                            #endregion
                            #region Indivisual results
                            //if (mystifly == 1)
                            //{
                            //    MystiAir_GetAvail mysti = new MystiAir_GetAvail();
                            //    XElement response = mysti.AirAvailability_mysti(req);
                            //    return response;
                            //}
                            //else if (galileo == 1)
                            //{
                            //    gal_getavail request = new gal_getavail();
                            //    XElement response = request.avail_response(req);
                            //    return response;
                            //}
                            //else
                            //{
                            //    #region Supplier doesn't Exists
                            //    IEnumerable<XElement> request = req.Descendants("SearchRequest");
                            //    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            //    XElement searchdoc = new XElement(
                            //      new XElement(soapenv + "Envelope",
                            //                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            //                new XElement(soapenv + "Header",
                            //                 new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            //                 new XElement("Authentication",
                            //                     new XElement("AgentID", AgentID),
                            //                     new XElement("UserName", username),
                            //                     new XElement("Password", password),
                            //                     new XElement("ServiceType", ServiceType),
                            //                     new XElement("ServiceVersion", ServiceVersion))),
                            //                 new XElement(soapenv + "Body",
                            //                     new XElement(request.Single()),
                            //           new XElement("SearchResponse",
                            //               new XElement("ErrorTxt", "Supplier doesn't Exists.")
                            //                       )
                            //                   )
                            //  ));
                            //    return searchdoc;
                            //    #endregion
                            //}
                            #endregion
                        }
                        else
                        {
                            #region Supplier doesn't Exists
                            IEnumerable<XElement> request = req.Descendants("SearchRequest");
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
                                   new XElement("SearchResponse",
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
                        IEnumerable<XElement> request = req.Descendants("SearchRequest");
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
                               new XElement("SearchResponse",
                                   new XElement("ErrorTxt", ex.Message)
                                           )
                                       )
                      ));
                        CustomException ex1 = new CustomException(ex);
                        ex1.MethodName = "CreateAirAvailability";
                        ex1.PageName = "TrvAirSearch";
                        ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                        ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
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
                    IEnumerable<XElement> request = req.Descendants("SearchRequest");
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
                           new XElement("SearchResponse",
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
                IEnumerable<XElement> request = req.Descendants("SearchRequest");
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
                       new XElement("SearchResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CreateAirAvailability";
                ex1.PageName = "TrvAirSearch";
                ex1.CustomerID = req.Descendants("SearchRequest").Attributes("CustomerID").FirstOrDefault().Value;
                ex1.TranID = req.Descendants("SearchRequest").Attributes("TransID").FirstOrDefault().Value;
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