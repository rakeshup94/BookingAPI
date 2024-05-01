using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Xml;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Serialization;
using System.Collections;

namespace TravillioXMLOutService.App_Code
{
    public class Soap_280417
    {
        string availDarina = string.Empty;
        XElement reqTravillio;
        List<Tourico.Hotel> hotelavailabilityresult;
        List<XElement> hotelavailabilitylistextranet;
        
        #region Logs
        public void WriteToFile(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {


                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Hotel Availability Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void WriteToFileResponseTime(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {


                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Time Calculate-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region Hotel Availability (XML OUT for Travayoo)
        public XElement CreateCheckAvailability(XElement req)
        {

            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            #region Hotel Availability
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                try
                {
                    // Need City Mapping Before final integration
                    // check supplier id......
                    // get city id of darina holidays
                    // get nationality id for darina holidays
                    // get destination code or destination id for Tourico Holidays
                    reqTravillio = req;
                    string avail = null;

                    int darina = req.Descendants("SupplierID").Where(x => x.Value == "1").Count();
                    int tourico = req.Descendants("SupplierID").Where(x => x.Value == "2").Count();
                    int extranet = req.Descendants("SupplierID").Where(x => x.Value == "3").Count();
                    if (darina == 1 && tourico == 1 && extranet == 1)
                    {
                        #region Darina and Tourico and Extranet
                        if (Convert.ToInt32(req.Descendants("RoomPax").Count()) == 1)
                        {
                            Thread tid1 = new Thread(new ThreadStart(gethotelavaillistDarina));
                            Thread tid2 = new Thread(new ThreadStart(gethotelavaillistTourico));
                            Thread tid3 = new Thread(new ThreadStart(gethotelavailabilityExtranet));
                            List<Tourico.Hotel> hotellisttourico = new List<Tourico.Hotel>();
                            //tid2.Priority = ThreadPriority.Highest;
                            //tid2.Priority = ThreadPriority.Lowest;
                            try
                            {
                                tid1.Start();
                                tid2.Start();
                                tid3.Start();
                            }
                            catch (ThreadStateException te)
                            {
                                WriteToFile(te.ToString());
                            }
                            tid1.Join();
                            tid2.Join();
                            tid3.Join();
                            tid1.Abort();
                            tid2.Abort();
                            tid3.Abort();
                            string flag = "htlist";
                            avail = availDarina;
                            string hoteldetail = "";
                            #region Availability
                            if (avail != null || hotelavailabilityresult.Count() > 0 || hotelavailabilitylistextranet.Count() > 0)
                            {
                                flag = "htdetail";
                                string _url = "http://travelcontrol-agents-api.azurewebsites.net/service.asmx";
                                string _action = "http://travelcontrol.softexsw.us/GetBasicData_Properties_WithFullDetails";
                                WriteToFileResponseTime("start time hotel detail Darina");
                                hoteldetail = CallWebService(req, _url, _action, flag);
                                WriteToFileResponseTime("end time hotel detail Darina");
                                XElement doc = XElement.Parse(avail);
                                WriteToFile(avail);
                                List<XElement> htlist = doc.Descendants("D").ToList();
                                XElement dochtd = XElement.Parse(hoteldetail);
                                IEnumerable<XElement> htlistdetail = dochtd.Descendants("D").ToList();
                                XElement doccurrency = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\currency.xml"));
                                XElement docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
                                XElement dococcupancy = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\Occupancy.xml"));
                                IEnumerable<XElement> currencycode = doccurrency.Descendants("d0").Where(x => x.Descendants("CurrencyID").Single().Value == req.Descendants("CurrencyID").Single().Value);
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                WriteToFileResponseTime("Start Time to make  xml");
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
                                       new XElement("searchResponse",
                                           new XElement("Hotels",
                                                   GetHotelList(htlist, htlistdetail, docmealplan, dococcupancy, currencycode),
                                                   GetHotelListTourico(hotelavailabilityresult),
                                               GetHotelListExtranet(hotelavailabilitylistextranet)
                                               )
                              ))));
                                WriteToFileResponseTime("End Time to make xml");
                                return searchdoc;
                            }
                            #endregion
                            #region Availability Not Found
                            else
                            {
                                #region No Record Found (Server Not Responding)
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                       new XElement("searchResponse",
                                           new XElement("ErrorTxt", "Server is not responding")
                                                   )
                                               )
                              ));
                                return searchdoc;
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                        #region Tourico and Extranet Hotel Availability
                        if (Convert.ToInt32(req.Descendants("RoomPax").Count()) > 1)
                        {
                            WriteToFileResponseTime("start time hotel listing Tourico and Extranet");
                            Thread tid1 = new Thread(new ThreadStart(gethotelavailabilityExtranet));
                            Thread tid2 = new Thread(new ThreadStart(gethotelavaillistTourico));
                            List<Tourico.Hotel> hotellisttourico = new List<Tourico.Hotel>();
                            tid2.Priority = ThreadPriority.Highest;
                            tid2.Priority = ThreadPriority.Lowest;
                            try
                            {
                                tid1.Start();
                                tid2.Start();
                            }
                            catch (ThreadStateException te)
                            {
                                WriteToFile(te.ToString());
                            }
                            tid1.Join();
                            tid2.Join();
                            tid1.Abort();
                            tid2.Abort();
                            WriteToFileResponseTime("end time hotel listing Tourico and Extranet");
                            #region Tourico and Extranet
                            if (hotelavailabilityresult.Count() > 0 || hotelavailabilitylistextranet.Count() > 0)
                            {
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                WriteToFileResponseTime("Start Time to make  xml");
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
                                       new XElement("searchResponse",
                                           new XElement("Hotels",
                                                   GetHotelListTourico(hotelavailabilityresult),
                                               GetHotelListExtranet(hotelavailabilitylistextranet)
                                               )
                              ))));
                                WriteToFileResponseTime("End Time to make xml");
                                return searchdoc;
                            }
                            #endregion
                            #region Tourico  and Extranet Not Responding
                            else
                            {
                                #region No Record Found (Server Not Responding)
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                       new XElement("searchResponse",
                                           new XElement("ErrorTxt", "Server is not responding")
                                                   )
                                               )
                              ));
                                return searchdoc;
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                        #region Server Not Responding
                        else
                        {
                            #region No Record Found (Server Not Responding)
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                   new XElement("searchResponse",
                                       new XElement("ErrorTxt", "Server is not responding")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                        #endregion
                    }
                    else if (darina == 1 && tourico == 1 && extranet !=1)
                    {
                        #region Darina and Tourico
                        if (Convert.ToInt32(req.Descendants("RoomPax").Count()) == 1)
                        {
                            Thread tid1 = new Thread(new ThreadStart(gethotelavaillistDarina));
                            Thread tid2 = new Thread(new ThreadStart(gethotelavaillistTourico));
                            List<Tourico.Hotel> hotellisttourico = new List<Tourico.Hotel>();
                            tid2.Priority = ThreadPriority.Highest;
                            tid2.Priority = ThreadPriority.Lowest;
                            try
                            {
                                tid1.Start();
                                tid2.Start();
                            }
                            catch (ThreadStateException te)
                            {
                                WriteToFile(te.ToString());
                            }
                            tid1.Join();
                            tid2.Join();
                            tid1.Abort();
                            tid2.Abort();
                            string flag = "htlist";
                            avail = availDarina;
                            string hoteldetail = "";
                            #region Availability
                            if (avail != null || hotelavailabilityresult.Count() > 0)
                            {
                                flag = "htdetail";
                                string _url = "http://travelcontrol-agents-api.azurewebsites.net/service.asmx";
                                string _action = "http://travelcontrol.softexsw.us/GetBasicData_Properties_WithFullDetails";
                                WriteToFileResponseTime("start time hotel detail Darina");
                                hoteldetail = CallWebService(req, _url, _action, flag);
                                WriteToFileResponseTime("end time hotel detail Darina");
                                XElement doc = XElement.Parse(avail);
                                WriteToFile(avail);
                                List<XElement> htlist = doc.Descendants("D").ToList();
                                XElement dochtd = XElement.Parse(hoteldetail);
                                IEnumerable<XElement> htlistdetail = dochtd.Descendants("D").ToList();
                                XElement doccurrency = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\currency.xml"));
                                XElement docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
                                XElement dococcupancy = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\Occupancy.xml"));
                                IEnumerable<XElement> currencycode = doccurrency.Descendants("d0").Where(x => x.Descendants("CurrencyID").Single().Value == req.Descendants("CurrencyID").Single().Value);
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                WriteToFileResponseTime("Start Time to make  xml");
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
                                       new XElement("searchResponse",
                                           new XElement("Hotels",
                                                   GetHotelList(htlist, htlistdetail, docmealplan, dococcupancy, currencycode),
                                                   GetHotelListTourico(hotelavailabilityresult)
                                               )
                              ))));
                                WriteToFileResponseTime("End Time to make xml");
                                return searchdoc;
                            }
                            #endregion
                            #region Availability Not Found
                            else
                            {
                                #region No Record Found (Server Not Responding)
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                       new XElement("searchResponse",
                                           new XElement("ErrorTxt", "Server is not responding")
                                                   )
                                               )
                              ));
                                return searchdoc;
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                        #region Tourico Hotel Availability
                        if (Convert.ToInt32(req.Descendants("RoomPax").Count()) > 1)
                        {
                            WriteToFileResponseTime("start time hotel listing Tourico");
                            gethotelavaillistTourico();
                            WriteToFileResponseTime("end time hotel listing Tourico");
                            #region Tourico
                            if (hotelavailabilityresult.Count() > 0)
                            {
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                WriteToFileResponseTime("Start Time to make  xml");
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
                                       new XElement("searchResponse",
                                           new XElement("Hotels",
                                                   GetHotelListTourico(hotelavailabilityresult)
                                               )
                              ))));
                                WriteToFileResponseTime("End Time to make xml");
                                return searchdoc;
                            }
                            #endregion
                            #region Tourico Not Responding
                            else
                            {
                                #region No Record Found (Server Not Responding)
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                       new XElement("searchResponse",
                                           new XElement("ErrorTxt", "Server is not responding")
                                                   )
                                               )
                              ));
                                return searchdoc;
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                        #region Server Not Responding
                        else
                        {
                            #region No Record Found (Server Not Responding)
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                   new XElement("searchResponse",
                                       new XElement("ErrorTxt", "Server is not responding")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                        #endregion
                    }
                    else if (darina == 1 && tourico != 1 && extranet == 1)
                    {
                        #region Darina and Extranet
                        if (Convert.ToInt32(req.Descendants("RoomPax").Count()) == 1)
                        {
                            Thread tid1 = new Thread(new ThreadStart(gethotelavaillistDarina));
                            Thread tid2 = new Thread(new ThreadStart(gethotelavailabilityExtranet));
                            List<Tourico.Hotel> hotellisttourico = new List<Tourico.Hotel>();
                            tid2.Priority = ThreadPriority.Highest;
                            tid2.Priority = ThreadPriority.Lowest;
                            try
                            {
                                tid1.Start();
                                tid2.Start();
                            }
                            catch (ThreadStateException te)
                            {
                                WriteToFile(te.ToString());
                            }
                            tid1.Join();
                            tid2.Join();
                            tid1.Abort();
                            tid2.Abort();
                            string flag = "htlist";
                            avail = availDarina;
                            string hoteldetail = "";
                            #region Availability
                            if (avail != null || hotelavailabilitylistextranet.Count() > 0)
                            {
                                flag = "htdetail";
                                string _url = "http://travelcontrol-agents-api.azurewebsites.net/service.asmx";
                                string _action = "http://travelcontrol.softexsw.us/GetBasicData_Properties_WithFullDetails";
                                WriteToFileResponseTime("start time hotel detail Darina");
                                hoteldetail = CallWebService(req, _url, _action, flag);
                                WriteToFileResponseTime("end time hotel detail Darina");
                                XElement doc = XElement.Parse(avail);
                                WriteToFile(avail);
                                List<XElement> htlist = doc.Descendants("D").ToList();
                                XElement dochtd = XElement.Parse(hoteldetail);
                                IEnumerable<XElement> htlistdetail = dochtd.Descendants("D").ToList();
                                XElement doccurrency = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\currency.xml"));
                                XElement docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
                                XElement dococcupancy = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\Occupancy.xml"));
                                IEnumerable<XElement> currencycode = doccurrency.Descendants("d0").Where(x => x.Descendants("CurrencyID").Single().Value == req.Descendants("CurrencyID").Single().Value);
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                WriteToFileResponseTime("Start Time to make  xml");
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
                                       new XElement("searchResponse",
                                           new XElement("Hotels",
                                                   GetHotelList(htlist, htlistdetail, docmealplan, dococcupancy, currencycode),
                                                   GetHotelListExtranet(hotelavailabilitylistextranet)
                                               )
                              ))));
                                WriteToFileResponseTime("End Time to make xml");
                                return searchdoc;
                            }
                            #endregion
                            #region Availability Not Found
                            else
                            {
                                #region No Record Found (Server Not Responding)
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                       new XElement("searchResponse",
                                           new XElement("ErrorTxt", "Server is not responding")
                                                   )
                                               )
                              ));
                                return searchdoc;
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                        #region Extranet Hotel Availability
                        if (Convert.ToInt32(req.Descendants("RoomPax").Count()) > 1)
                        {
                            WriteToFileResponseTime("start time hotel listing Extranet");
                            gethotelavailabilityExtranet();
                            WriteToFileResponseTime("end time hotel listing Extranet");
                            #region Tourico
                            if (hotelavailabilitylistextranet.Count() > 0)
                            {
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
                                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                                WriteToFileResponseTime("Start Time to make  xml");
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
                                       new XElement("searchResponse",
                                           new XElement("Hotels",
                                                   GetHotelListExtranet(hotelavailabilitylistextranet)
                                               )
                              ))));
                                WriteToFileResponseTime("End Time to make xml");
                                return searchdoc;
                            }
                            #endregion
                            #region Tourico Not Responding
                            else
                            {
                                #region No Record Found (Server Not Responding)
                                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                       new XElement("searchResponse",
                                           new XElement("ErrorTxt", "Server is not responding")
                                                   )
                                               )
                              ));
                                return searchdoc;
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                        #region Server Not Responding
                        else
                        {
                            #region No Record Found (Server Not Responding)
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                   new XElement("searchResponse",
                                       new XElement("ErrorTxt", "Server is not responding")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                        #endregion
                    }
                    else if (darina == 1 && tourico != 1 && extranet != 1)
                    {

                        #region Darina Hotel Availability
                        string flag = "htlist";
                        var _url = "http://travelcontrol-agents-api.azurewebsites.net/service_v2.asmx";
                        var _action = "http://travelcontrol.softexsw.us/CheckAvailability";
                        WriteToFileResponseTime("start time hotel listing Darina");
                        availDarina = CallWebService(reqTravillio, _url, _action, flag);
                        WriteToFile(availDarina);
                        WriteToFileResponseTime("end time hotel listing Darina");
                        #endregion

                        flag = "htlist";
                        avail = availDarina;
                        string hoteldetail = "";
                        #region Availability
                        if (avail != null)
                        {
                            flag = "htdetail";
                            _url = "http://travelcontrol-agents-api.azurewebsites.net/service.asmx";
                            _action = "http://travelcontrol.softexsw.us/GetBasicData_Properties_WithFullDetails";
                            WriteToFileResponseTime("start time hotel detail Darina");
                            hoteldetail = CallWebService(req, _url, _action, flag);
                            WriteToFileResponseTime("end time hotel detail Darina");
                            XElement doc = XElement.Parse(avail);
                            WriteToFile(avail);
                            List<XElement> htlist = doc.Descendants("D").ToList();
                            XElement dochtd = XElement.Parse(hoteldetail);
                            IEnumerable<XElement> htlistdetail = dochtd.Descendants("D").ToList();
                            XElement doccurrency = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\currency.xml"));
                            XElement docmealplan = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\MealPlan.xml"));
                            XElement dococcupancy = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Darina\Occupancy.xml"));
                            IEnumerable<XElement> currencycode = doccurrency.Descendants("d0").Where(x => x.Descendants("CurrencyID").Single().Value == req.Descendants("CurrencyID").Single().Value);
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            WriteToFileResponseTime("Start Time to make  xml");
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
                                   new XElement("searchResponse",
                                       new XElement("Hotels",
                                               GetHotelList(htlist, htlistdetail, docmealplan, dococcupancy, currencycode)
                                           )
                          ))));
                            WriteToFileResponseTime("End Time to make xml");
                            return searchdoc;
                        }
                        #endregion
                        #region Availability Not Found
                        else
                        {
                            #region No Record Found (Server Not Responding)
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                   new XElement("searchResponse",
                                       new XElement("ErrorTxt", "Server is not responding")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                        #endregion
                    }
                    else if (darina != 1 && tourico == 1 && extranet != 1)
                    {
                        WriteToFileResponseTime("start time hotel listing Tourico");
                        gethotelavaillistTourico();
                        WriteToFileResponseTime("end time hotel listing Tourico");
                        #region Tourico
                        if (hotelavailabilityresult.Count() > 0)
                        {
                            
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            WriteToFileResponseTime("Start Time to make  xml");
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
                                   new XElement("searchResponse",
                                       new XElement("Hotels",
                                               GetHotelListTourico(hotelavailabilityresult)
                                           )
                          ))));
                            WriteToFileResponseTime("End Time to make xml");
                            return searchdoc;
                        }
                        #endregion
                        #region Tourico Not Responding
                        else
                        {
                            #region No Record Found (Server Not Responding)
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                   new XElement("searchResponse",
                                       new XElement("ErrorTxt", "Server is not responding")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                        #endregion
                    }
                    else if (darina != 1 && tourico == 1 && extranet == 1)
                    {
                        WriteToFileResponseTime("start time hotel listing Tourico");
                        Thread tid1 = new Thread(new ThreadStart(gethotelavailabilityExtranet));
                        Thread tid2 = new Thread(new ThreadStart(gethotelavaillistTourico));
                        List<Tourico.Hotel> hotellisttourico = new List<Tourico.Hotel>();
                        tid2.Priority = ThreadPriority.Highest;
                        tid2.Priority = ThreadPriority.Lowest;
                        try
                        {
                            tid1.Start();
                            tid2.Start();
                        }
                        catch (ThreadStateException te)
                        {
                            WriteToFile(te.ToString());
                        }
                        tid1.Join();
                        tid2.Join();
                        tid1.Abort();
                        tid2.Abort();

                        WriteToFileResponseTime("end time hotel listing Tourico");
                        #region Tourico and Extranet
                        if (hotelavailabilityresult.Count() > 0 || hotelavailabilitylistextranet.Count() > 0)
                        {
                            
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            WriteToFileResponseTime("Start Time to make  xml");
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
                                   new XElement("searchResponse",
                                       new XElement("Hotels",
                                               GetHotelListTourico(hotelavailabilityresult),
                                               GetHotelListExtranet(hotelavailabilitylistextranet)
                                           )
                          ))));
                            WriteToFileResponseTime("End Time to make xml");
                            return searchdoc;
                        }
                        #endregion
                        #region Tourico and Extranet Not Responding
                        else
                        {
                            #region No Record Found (Server Not Responding)
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                   new XElement("searchResponse",
                                       new XElement("ErrorTxt", "Server is not responding")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                        #endregion
                    }
                    else if (darina != 1 && tourico != 1 && extranet == 1) 
                    {
                        WriteToFileResponseTime("start time hotel listing Extranet");
                        gethotelavailabilityExtranet();
                        WriteToFileResponseTime("end time hotel listing Extranet");
                        #region Extranet
                        if (hotelavailabilitylistextranet.Count() > 0)
                        {
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
                            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                            WriteToFileResponseTime("Start Time to make  xml");
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
                                   new XElement("searchResponse",
                                       new XElement("Hotels",
                                               GetHotelListExtranet(hotelavailabilitylistextranet)
                                           )
                          ))));
                            WriteToFileResponseTime("End Time to make xml");
                            return searchdoc;
                        }
                        #region Extranet Not Responding
                        else
                        {
                            #region No Record Found (Server Not Responding)
                            IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                                   new XElement("searchResponse",
                                       new XElement("ErrorTxt", "Server is not responding")
                                               )
                                           )
                          ));
                            return searchdoc;
                            #endregion
                        }
                        #endregion
                        #endregion
                    }
                    else
                    {
                        #region Supplier doesn't Exists
                        IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                               new XElement("searchResponse",
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
                    IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                           new XElement("searchResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    return searchdoc;
                    #endregion
                }
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("searchRequest");
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
                       new XElement("searchResponse",
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
        #region Hotel Availability Methods
        public void gethotelavaillistDarina()
        {
            #region Darina Hotel Availability
            string flag = "htlist";
            var _url = "http://travelcontrol-agents-api.azurewebsites.net/service_v2.asmx";
            var _action = "http://travelcontrol.softexsw.us/CheckAvailability";
            WriteToFileResponseTime("start time hotel listing Darina");
            availDarina = CallWebService(reqTravillio, _url, _action, flag);
            WriteToFile(availDarina);
            WriteToFileResponseTime("end time hotel listing Darina");
            #endregion
        }
        public void gethotelavaillistTourico()
        {
            #region Tourio Hotel Availability
            List<Tourico.Hotel> result = new List<Tourico.Hotel>();

            Tourico.AuthenticationHeader hd = new Tourico.AuthenticationHeader();
            hd.LoginName = "HOL916";
            hd.Password = "111111";
            hd.Version = "5";

            Tourico.HotelFlowClient client = new Tourico.HotelFlowClient();

            Tourico.SearchRequest request = new Tourico.SearchRequest();

            List<XElement> trum = reqTravillio.Descendants("RoomPax").ToList();
            Tourico.RoomInfo[] roominfo = new Tourico.RoomInfo[trum.Count()];

            for (int j = 0; j < trum.Count(); j++)
            {
                int childcount = Convert.ToInt32(trum[j].Element("Child").Value);
                Tourico.ChildAge[] chdage = new Tourico.ChildAge[childcount];
                for (int k = 0; k < childcount; k++)
                {
                    chdage[k] = new Tourico.ChildAge
                    {
                        age = Convert.ToInt32(trum[j].Element("ChildAge").Value)
                    };
                }

                roominfo[j] = new Tourico.RoomInfo
                {
                    AdultNum = Convert.ToInt32(trum[j].Element("Adult").Value),
                    ChildNum = Convert.ToInt32(trum[j].Element("Child").Value),
                    ChildAges = chdage
                };

            }

            request.Destination = reqTravillio.Descendants("CityCode").Single().Value.ToString();

            request.CheckIn = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            request.CheckOut = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            request.RoomsInformation = roominfo;
            request.MaxPrice = 0;
            request.StarLevel = Convert.ToInt32(reqTravillio.Descendants("MinStarRating").Single().Value);
            request.AvailableOnly = true;


            request.PropertyType = 0;

            request.ExactDestination = true;

            Tourico.Feature[] feature = new Tourico.Feature[1];
            for (int i = 0; i < 1; i++)
            {
                feature[i] = new Tourico.Feature { name = "", value = "" };
            }

            

            hotelavailabilityresult = client.SearchHotels(hd, request, feature).HotelList.ToList();


            //List<Tourico.Hotel> hotl = hotelavailabilityresult.Where(x => x.hotelId == 2207).ToList();


            #endregion

        }
        #region Extranet Availability
        public void gethotelavailabilityExtranet()
        {
            List<XElement> doc1 = new List<XElement>();
            HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
            #region Extranet Request/Response
            string requestxml = string.Empty;
            requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>"+
                          "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>"+
                            "<Authentication>"+
                              "<AgentID>0</AgentID>"+
                              "<UserName>Suraj</UserName>"+
                              "<Password>123#</Password>"+
                              "<ServiceType>HT_001</ServiceType>"+
                              "<ServiceVersion>v1.0</ServiceVersion>"+
                            "</Authentication>"+
                          "</soapenv:Header>"+
                          "<soapenv:Body>"+
                            "<searchRequest>"+
                              "<Response_Type>XML</Response_Type>"+
                              "<CustomerID>" + reqTravillio.Descendants("CustomerID").Single().Value + "</CustomerID>" +
                              "<FromDate>" + reqTravillio.Descendants("FromDate").SingleOrDefault().Value + "</FromDate>" +
                              "<ToDate>" + reqTravillio.Descendants("ToDate").SingleOrDefault().Value + "</ToDate>" +
                              "<Nights>1</Nights>"+
                              "<CountryID>0</CountryID>"+
                              "<CountryName />"+
                              "<CityCode>" + reqTravillio.Descendants("CityCode").Single().Value + "</CityCode>" +
                              "<CityName>" + reqTravillio.Descendants("CityCode").Single().Value + "</CityName>" +
                              "<AreaID></AreaID>"+
                              "<AreaName />"+
                              "<MinStarRating>"+reqTravillio.Descendants("MinStarRating").Single().Value+"</MinStarRating>"+
                              "<MaxStarRating>" + reqTravillio.Descendants("MaxStarRating").Single().Value + "</MaxStarRating>" +
                              "<HotelName></HotelName>"+
                              "<PaxNationality_CountryID>" + reqTravillio.Descendants("PaxNationality_CountryCode").Single().Value + "</PaxNationality_CountryID>" +
                              "<CurrencyID>1</CurrencyID>"+
                              reqTravillio.Descendants("Rooms").SingleOrDefault().ToString() +
                              "<MealPlanList>"+
                                "<MealType>1</MealType>"+
                                "<MealType>2</MealType>"+
                                "<MealType>3</MealType>"+
                                "<MealType>4</MealType>"+
                                "<MealType>5</MealType>"+
                              "</MealPlanList>"+
                              "<PropertyType>1</PropertyType>"+
                              "<SuppliersList />"+
                              "<SubAgentID>0</SubAgentID>"+
                            "</searchRequest>"+
                          "</soapenv:Body>" +
                        "</soapenv:Envelope>";
            try
            {

                object result = extclient.GetSearchCityRequestByXML(requestxml,false);
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    hotelavailabilitylistextranet = doc.Descendants("Hotel").ToList();
                }
                else
                {
                    
                    hotelavailabilitylistextranet = doc1.Descendants("Hotel").ToList();
                   
                }
            }
            catch(Exception ex)
            {
                
                hotelavailabilitylistextranet = doc1.Descendants("Hotel").ToList();
            }
            
           
            #endregion
        }
        #endregion
        #endregion
        #region Darina Holidays
        #region Darina's Hotel Listing
        private IEnumerable<XElement> GetHotelList(List<XElement> htlist, IEnumerable<XElement> htlistdetail, XElement docmealplan, XElement dococcupancy, IEnumerable<XElement> currencycode)
        {
            #region Darina Hotel List
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = htlist.Descendants("Hotels").Count();
                List<XElement> hotellist = htlist.Descendants("Hotels").ToList();
                try
                {
                    Parallel.For(0, length, i =>
                    {

                        IEnumerable<XElement> hoteldetails = htlistdetail.Descendants("d0").Where(x => x.Descendants("HotelID").Single().Value == hotellist[i].Element("HotelID").Value);
                        List<XElement> hotelfacilities = htlistdetail.Descendants("HFac").Where(x => x.Descendants("HotelID").Single().Value == hotellist[i].Element("HotelID").Value).ToList();
                        List<XElement> roomlist = htlist.Descendants("AccRates").Where(x => x.Descendants("Hotel").Single().Value == hotellist[i].Element("HotelID").Value).ToList();
                        string map = Convert.ToString(hotellist[i].Element("Maplink").Value);

                        hotellst.Add(new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(hotellist[i].Element("HotelID").Value)),
                                               new XElement("HotelName", Convert.ToString(hotellist[i].Element("Name").Value)),
                                               new XElement("PropertyTypeName", Convert.ToString(hoteldetails.Descendants("ProprtyTypeName").Single().Value)),
                                               new XElement("CountryID", Convert.ToString(hoteldetails.Descendants("CountryID").Single().Value)),
                                               new XElement("CountryName", Convert.ToString(hoteldetails.Descendants("CountryName").Single().Value)),
                                               new XElement("CountryCode", Convert.ToString(hoteldetails.Descendants("CountryCode").Single().Value)),
                                               new XElement("CityId", Convert.ToString(hoteldetails.Descendants("CityID").Single().Value)),
                                               new XElement("CityCode", Convert.ToString(hoteldetails.Descendants("CityCode").Single().Value)),
                                               new XElement("CityName", Convert.ToString(hoteldetails.Descendants("CityName").Single().Value)),
                                               new XElement("AreaId", Convert.ToString(hoteldetails.Descendants("AreaID").Single().Value)),
                                               new XElement("AreaName", Convert.ToString(hoteldetails.Descendants("AreaName").Single().Value))
                                               , new XElement("Address", Convert.ToString(hoteldetails.Descendants("Address").Single().Value)),
                                               new XElement("Location", Convert.ToString(hoteldetails.Descendants("Address").Single().Value)),
                                               new XElement("Description", ""),
                                               new XElement("StarRating", Convert.ToString(hoteldetails.Descendants("Landcategory").Single().Value)),
                                               new XElement("MinRate", Convert.ToString(roomlist.Descendants("RatePerStay").FirstOrDefault().Value))
                                               , new XElement("HotelImgSmall", Convert.ToString(hotellist[i].Element("SmallImgLink").Value)),
                                               new XElement("HotelImgLarge", Convert.ToString(hotellist[i].Element("LargeImgLink").Value)),
                                               new XElement("MapLink", map),
                                               new XElement("Longitude", ""),
                                               new XElement("Latitude", ""),
                                               new XElement("DMC", "Darina"),
                                               new XElement("SupplierID", "1"),
                                               new XElement("Currency", Convert.ToString(currencycode.Descendants("CurrencyName").Single().Value)),
                                               new XElement("Offers", "")
                                               , new XElement("Facilities",
                                                   GetHotelFacilities(hotelfacilities))
                                               , new XElement("Rooms",

                                                    GetHotelRooms(htlist, roomlist, docmealplan, dococcupancy)
                                                   )
                        ));

                    });
                }
                catch (Exception ex)
                {
                    return hotellst;
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
        #region Darina's Hotel Room's Listing
        public IEnumerable<XElement> GetHotelRooms(List<XElement> htlist, List<XElement> roomlist, XElement docmealplan, XElement dococcupancy)
        {
            #region Darina Hotel's Room List
            List<XElement> str = new List<XElement>();
            IEnumerable<XElement> roomtypes = htlist;
            DateTime fromDate = DateTime.ParseExact(reqTravillio.Descendants("FromDate").Single().Value, "dd/MM/yyyy", null);
            DateTime toDate = DateTime.ParseExact(reqTravillio.Descendants("ToDate").Single().Value, "dd/MM/yyyy", null);
            int nights = (int)(toDate - fromDate).TotalDays;
            Parallel.For(0, roomlist.Count(), i =>
            {

                IEnumerable<XElement> docmealplandet = docmealplan.Descendants("d0").Where(x => x.Descendants("MealPlanID").Single().Value == roomlist[i].Descendants("MealPlan").Single().Value);
                IEnumerable<XElement> dococcupancydet = dococcupancy.Descendants("d0").Where(x => x.Descendants("OccupancyID").Single().Value == roomlist[i].Descendants("Occupancy").Single().Value);
                IEnumerable<XElement> room = roomtypes.Descendants("RoomTypes").Where(x => x.Descendants("RoomTypeID").Single().Value == roomlist[i].Descendants("RoomType").Single().Value);
                string isavailableval = roomlist[i].Descendants("Availability").Single().Value;
                string isavailable = string.Empty;
                if (isavailableval == "1")
                {
                    isavailable = "true";
                }
                else
                {
                    isavailable = "false";
                }
                str.Add(new XElement("RoomTypes", new XAttribute("Index", i + 1), new XAttribute("TotalRate", roomlist[i].Descendants("RatePerStay").Single().Value),
                    new XElement("Room",
                         new XAttribute("ID", Convert.ToString(roomlist[i].Descendants("RoomType").Single().Value)),
                         new XAttribute("SuppliersID", "1"),
                         new XAttribute("RoomSeq", "1"),
                         new XAttribute("SessionID", Convert.ToString(roomlist[i].Descendants("Serial").Single().Value)),
                         new XAttribute("RoomType", Convert.ToString(room.Descendants("RoomTypeName").Single().Value)),
                         new XAttribute("OccupancyID", Convert.ToString(roomlist[i].Descendants("Occupancy").Single().Value)),
                         new XAttribute("OccupancyName", Convert.ToString(dococcupancydet.Descendants("OccupancyName").Single().Value)),
                         new XAttribute("MealPlanID", Convert.ToString(roomlist[i].Descendants("MealPlan").Single().Value)),
                         new XAttribute("MealPlanName", Convert.ToString(docmealplandet.Descendants("MealPlanName").Single().Value)),
                         new XAttribute("MealPlanCode", ""),
                         new XAttribute("MealPlanPrice", ""),
                         new XAttribute("PerNightRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value)),
                         new XAttribute("TotalRoomRate", Convert.ToString(roomlist[i].Descendants("RatePerStay").Single().Value)),
                         new XAttribute("CancellationDate", ""),
                         new XAttribute("CancellationAmount", ""),
                         new XAttribute("isAvailable", isavailable),
                         new XElement("RequestID", Convert.ToString(roomlist[i].Descendants("RequestID").Single().Value)),
                         new XElement("Offers", ""),
                         new XElement("PromotionList",
                         new XElement("Promotions", Convert.ToString(roomlist[i].Descendants("Offers").Single().Value))),
                         new XElement("CancellationPolicy", ""),
                         new XElement("Amenities", new XElement("Amenity", "")),
                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                         new XElement("Supplements", new XElement("Supplement", new XAttribute("suppId", ""), new XAttribute("suppName", ""), new XAttribute("supptType", "")
                             , new XAttribute("suppIsMandatory", false), new XAttribute("suppChargeType", ""), new XAttribute("suppPrice", ""), new XAttribute("suppType", Convert.ToString("")))),
                             new XElement("PriceBreakups",
                                 GetRoomsPriceBreakupDarina(nights, Convert.ToString(roomlist[i].Descendants("RatePerNight").Single().Value))),
                                 new XElement("AdultNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value)),
                                 new XElement("ChildNum", Convert.ToString(reqTravillio.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value))
                         )));
            });
            return str;
            #endregion
        }
        #endregion
        #region Darina's Hotel Facilities
        private IEnumerable<XElement> GetHotelFacilities(List<XElement> hotelfacilities)
        {

            Int32 length = hotelfacilities.Count();
            List<XElement> Facilities = new List<XElement>();

            if (length == 0)
            {
                Facilities.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {

                Parallel.For(0, length, i =>
                {

                    Facilities.Add(new XElement("Facility", Convert.ToString(hotelfacilities[i].Descendants("Facility").Single().Value)));

                });
            }
            return Facilities;
        }
        #endregion
        private IEnumerable<XElement> GetRoomsPriceBreakupDarina(int nights, string pernightprice)
        {
            #region Darina Room's Price Breakups
            List<XElement> str = new List<XElement>();
            Parallel.For(0, nights, i =>
            {
                str.Add(new XElement("Price",
                       new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                       new XAttribute("PriceValue", Convert.ToString(pernightprice)))
                );
            });
            return str;
            #endregion
        }
        #endregion
        #region Tourico Holidays
        #region Tourico Hotel Listing
        private IEnumerable<XElement> GetHotelListTourico(List<Tourico.Hotel> htlist)
        {
            #region Tourico Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = htlist.Count();
                int minstarlevel = Convert.ToInt32(reqTravillio.Descendants("MinStarRating").Single().Value);
                int maxstarlevel = Convert.ToInt32(reqTravillio.Descendants("MaxStarRating").Single().Value);
                #region For Static Data
                XElement staticallhotellist = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\Tourico\HotelInfo.xml"));
                List<XElement> statichotellist = staticallhotellist.Descendants("Hotel").Where(x => x.Descendants("sDestination").SingleOrDefault().Value == reqTravillio.Descendants("CityCode").SingleOrDefault().Value).ToList();

                #endregion
                try
                {
                    Parallel.For(0, length, i =>
                    {
                        #region Fetch hotel according to star category
                        if (Convert.ToInt32(htlist[i].starsLevel) >= minstarlevel && Convert.ToInt32(htlist[i].starsLevel) <= maxstarlevel)
                        {

                            IEnumerable<XElement> hoteldetail = statichotellist.Where(x => Convert.ToInt32(x.Descendants("HotelId").SingleOrDefault().Value) == htlist[i].hotelId);
                            if (hoteldetail.ToList().Count() > 0)
                            {
                                decimal totalamt;
                                List<Tourico.RoomType> rmtypelist = new List<Tourico.RoomType>();
                                rmtypelist = htlist[i].RoomTypes.ToList();
                                int bb = rmtypelist[0].Occupancies[0].BoardBases.Count();
                                if (bb > 0)
                                {
                                    totalamt = rmtypelist[0].Occupancies[0].occupPublishPrice + rmtypelist[0].Occupancies[0].BoardBases[0].bbPublishPrice;
                                }
                                else
                                {
                                    totalamt = htlist[i].RoomTypes[0].Occupancies[0].occupPrice;
                                }
                                hotellst.Add(new XElement("Hotel",
                                    //new XElement("HotelID", Convert.ToString(htlist[i].hotelId)),
                                    //new XElement("HotelName", Convert.ToString(htlist[i].name)),
                                                       new XElement("HotelID", Convert.ToString(hoteldetail.Descendants("HotelId").SingleOrDefault().Value)),
                                                       new XElement("HotelName", Convert.ToString(hoteldetail.Descendants("HotelName").SingleOrDefault().Value)),
                                                       new XElement("PropertyTypeName", Convert.ToString(htlist[i].PropertyType)),
                                                       new XElement("CountryID", Convert.ToString(hoteldetail.Descendants("CountryCode").SingleOrDefault().Value)),
                                                       new XElement("CountryName", Convert.ToString(hoteldetail.Descendants("CountryName").SingleOrDefault().Value)),
                                                       new XElement("CountryCode", Convert.ToString(hoteldetail.Descendants("CountryCode").SingleOrDefault().Value)),
                                    //new XElement("CountryCode", Convert.ToString(htlist[i].Location.countryCode)),
                                                       new XElement("CityId", Convert.ToString("")),
                                    //new XElement("CityCode", Convert.ToString(htlist[i].Location.stateCode)),
                                                       new XElement("CityCode", Convert.ToString(hoteldetail.Descendants("sDestination").SingleOrDefault().Value)),
                                    //new XElement("CityName", Convert.ToString(htlist[i].Location.city)),
                                                       new XElement("CityName", Convert.ToString(hoteldetail.Descendants("AddressCity").SingleOrDefault().Value)),
                                                       new XElement("AreaId", Convert.ToString("")),
                                                       new XElement("AreaName", Convert.ToString(hoteldetail.Descendants("Location").SingleOrDefault().Value)),
                                    //new XElement("Address", Convert.ToString(htlist[i].Location.address)),
                                    //new XElement("Location", Convert.ToString(htlist[i].Location.location)),
                                                       new XElement("Address", Convert.ToString(hoteldetail.Descendants("Address").SingleOrDefault().Value)),
                                                       new XElement("Location", Convert.ToString(hoteldetail.Descendants("Location").SingleOrDefault().Value)),
                                                       new XElement("Description", Convert.ToString(htlist[i].desc)),
                                    //new XElement("StarRating", Convert.ToString(htlist[i].starsLevel)),
                                                       new XElement("StarRating", Convert.ToString(hoteldetail.Descendants("Stars").SingleOrDefault().Value)),
                                                       new XElement("MinRate", Convert.ToString(totalamt))
                                                       , new XElement("HotelImgSmall", Convert.ToString(hoteldetail.Descendants("ThumbnailPath").SingleOrDefault().Value)),
                                                       new XElement("HotelImgLarge", Convert.ToString(hoteldetail.Descendants("ThumbnailPath").SingleOrDefault().Value)),
                                                       new XElement("MapLink", ""),
                                    //new XElement("Longitude", Convert.ToString(htlist[i].Location.longitude)),
                                    //new XElement("Latitude", Convert.ToString(htlist[i].Location.latitude)),
                                                       new XElement("Longitude", Convert.ToString(hoteldetail.Descendants("Longitude").SingleOrDefault().Value)),
                                                       new XElement("Latitude", Convert.ToString(hoteldetail.Descendants("Latitude").SingleOrDefault().Value)),
                                                       new XElement("DMC", "Tourico"),
                                                       new XElement("SupplierID", "2"),
                                                       new XElement("Currency", Convert.ToString(htlist[i].currency)),
                                                       new XElement("Offers", "")
                                                       , new XElement("Facilities",
                                                           new XElement("Facility", "No Facility Available"))
                                                       , new XElement("Rooms",
                                                             GetHotelRoomgroupTourico(rmtypelist)
                                                           )
                                ));
                            }
                        }
                        #endregion
                    });
                }
                catch (Exception ex)
                {
                    return hotellst;
                }
            }
            catch (Exception exe)
            {
                return hotellst;
            }
            return hotellst;
            #endregion
        }
        #endregion
        #region Tourico Hotel's Room Grouping
        public IEnumerable<XElement> GetHotelRoomgroupTourico(List<Tourico.RoomType> roomlist)
        {
            List<XElement> str = new List<XElement>();
            List<XElement> strgrp = new List<XElement>();
            int rindex = 1;
            Parallel.For(0, roomlist.Count(), i =>
            //for(int i=0;i< roomlist.Count();i++)
            {
                //int bb = roomlist[i].Occupancies[0].BoardBases.Count();
                //if (bb > 1)
                //{
                    
                //    str = GetHotelRoomListingTourico(roomlist[i]).ToList();
                    
                //    for (int j = 0; j < str.Count(); j++)
                //    {

                //        str[j].Add(new XAttribute("Index", rindex));
                //        strgrp.Add(str[j]);
                //        rindex++;
                //    }    



                //}
                //else
                {
                    str = GetHotelRoomListingTourico(roomlist[i]).ToList();
                    Parallel.For(0, str.Count(), k =>
                    //for (int k = 0; k < str.Count(); k++)
                    {

                        str[k].Add(new XAttribute("Index", rindex));
                        strgrp.Add(str[k]);
                        rindex++;
                    });    

                    
                }
            });
            return strgrp;
        }
        #endregion
        #region Tourico Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingTourico(Tourico.RoomType roomlist)
        {
            List<XElement> str = new List<XElement>();
            List<Tourico.Occupancy> roomList1 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList2 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList3 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList4 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList5 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList6 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList7 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList8 = new List<Tourico.Occupancy>();
            List<Tourico.Occupancy> roomList9 = new List<Tourico.Occupancy>();
            int totalroom = Convert.ToInt32(reqTravillio.Descendants("RoomPax").Count());
            
            #region Notes: The maximum number of rooms that can be retrieved by a single search request is nine (9)
            #endregion

            #region Room Count 1
            if (totalroom == 1)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();

                for (int m = 0; m < roomList1.Count(); m++)
                {
                    // add room 1
                    
                    #region room's group

                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                    string promotion = string.Empty;
                    if (roomlist.Discount != null)
                    {
                        #region Discount (Tourico) Amount already subtracted
                        try
                        {
                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                            XmlDocument doc3 = new XmlDocument();
                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                            XmlWriter writer3 = XmlWriter.Create(sww3);
                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                            var typxsd = XDocument.Parse(sww3.ToString());
                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                            var distype = typxsd.Root.Attribute(disprefix + "type");
                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                            {
                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                            }
                            else
                            {
                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                        #endregion
                    }

                    if (bb == 0)
                    {
                        group++;
                        #region No Board Bases
                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", roomList1[m].occupPublishPrice),
                        new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                             new XAttribute("SuppliersID", "2"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                             new XAttribute("MealPlanID", Convert.ToString("")),
                             new XAttribute("MealPlanName", Convert.ToString("")),
                             new XAttribute("MealPlanCode", ""),
                             new XAttribute("MealPlanPrice", ""),
                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                             new XAttribute("isAvailable", roomlist.isAvailable),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                             new XElement("PromotionList", 
                             new XElement("Promotions", Convert.ToString(promotion))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements",
                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                             )));
                        #endregion
                    }
                    else
                    {
                        #region Board Bases >0
                        Parallel.For(0, bb, j =>
                        //for (int j = 0; j < bb; j++)
                        {
                            int countpaidnight1 = 0;
                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                            {
                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                {
                                    countpaidnight1 = countpaidnight1 + 1;
                                }
                            });
                            
                            group++;
                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;

                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalamt),

                            new XElement("Room",
                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                             new XAttribute("SuppliersID", "2"),
                             new XAttribute("RoomSeq", "1"),
                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                             new XAttribute("MealPlanCode", ""),
                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt/countpaidnight1)),
                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                             new XAttribute("CancellationDate", ""),
                             new XAttribute("CancellationAmount", ""),
                              new XAttribute("isAvailable", roomlist.isAvailable),
                             new XElement("RequestID", Convert.ToString("")),
                             new XElement("Offers", ""),
                              new XElement("PromotionList", 
                             new XElement("Promotions", Convert.ToString(promotion))),
                             new XElement("CancellationPolicy", ""),
                             new XElement("Amenities", new XElement("Amenity", "")),
                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                             new XElement("Supplements",
                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                 ),
                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                             )));
                        });
                        #endregion
                    }

                    //return str;
                    #endregion

                }
                return str;
            }
            #endregion
            #region Room Count 2
            if (totalroom == 2)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                           
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();
                //List<Tourico.Occupancy> roomList2 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 2).ToList();
                Parallel.For(0, roomList1.Count(), m =>
               //for (int m = 0; m < roomList1.Count(); m++)
               {
                   Parallel.For(0, roomList2.Count(), n =>
                   //for (int n = 0; n < roomList2.Count(); n++)
                   {

                       // add room 1, 2

                       #region room's group

                       int bb = roomlist.Occupancies[m].BoardBases.Count();
                       List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                       List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                       string promotion = string.Empty;
                       if (roomlist.Discount != null)
                       {
                           #region Discount (Tourico) Amount already subtracted
                           try
                           {
                               XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                               XmlDocument doc3 = new XmlDocument();
                               System.IO.StringWriter sww3 = new System.IO.StringWriter();
                               XmlWriter writer3 = XmlWriter.Create(sww3);
                               xsSubmit3.Serialize(writer3, roomlist.Discount);
                               var typxsd = XDocument.Parse(sww3.ToString());
                               var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                               var distype = typxsd.Root.Attribute(disprefix + "type");
                               if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                               {
                                   promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                               }
                               else
                               {
                                   promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                               }
                           }
                           catch (Exception ex)
                           {

                           }
                           #endregion
                       }

                       if (bb == 0)
                       {
                           group++;
                           decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice;
                           #region No Board Bases
                           str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                           new XElement("Room",
                                new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                new XAttribute("SuppliersID", "2"),
                                new XAttribute("RoomSeq", "1"),
                                new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                new XAttribute("MealPlanID", Convert.ToString("")),
                                new XAttribute("MealPlanName", Convert.ToString("")),
                                new XAttribute("MealPlanCode", ""),
                                new XAttribute("MealPlanPrice", ""),
                                new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                new XAttribute("CancellationDate", ""),
                                new XAttribute("CancellationAmount", ""),
                                new XAttribute("isAvailable", roomlist.isAvailable),
                                new XElement("RequestID", Convert.ToString("")),
                                new XElement("Offers", ""),
                                 new XElement("PromotionList", 
                                new XElement("Promotions", Convert.ToString(promotion))),
                                new XElement("CancellationPolicy", ""),
                                new XElement("Amenities", new XElement("Amenity", "")),
                                new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                new XElement("Supplements",
                                    GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                    ),
                                    new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                    new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                    new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                ),

                           new XElement("Room",
                                new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                new XAttribute("SuppliersID", "2"),
                                new XAttribute("RoomSeq", "2"),
                                new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                new XAttribute("MealPlanID", Convert.ToString("")),
                                new XAttribute("MealPlanName", Convert.ToString("")),
                                new XAttribute("MealPlanCode", ""),
                                new XAttribute("MealPlanPrice", ""),
                                new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                new XAttribute("CancellationDate", ""),
                                new XAttribute("CancellationAmount", ""),
                                new XAttribute("isAvailable", roomlist.isAvailable),
                                new XElement("RequestID", Convert.ToString("")),
                                new XElement("Offers", ""),
                                 new XElement("PromotionList", 
                                new XElement("Promotions", Convert.ToString(promotion))),
                                new XElement("CancellationPolicy", ""),
                                new XElement("Amenities", new XElement("Amenity", "")),
                                new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                new XElement("Supplements",
                                    GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                    ),
                                    new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                    new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                    new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                )));
                           #endregion
                       }
                       else
                       {
                           #region Board Bases >0
                           Parallel.For(0, bb, j =>
                           //for (int j = 0; j < bb; j++)
                           {
                               int countpaidnight1 = 0;
                               Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                               {
                                   if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                   {
                                       countpaidnight1 = countpaidnight1 + 1;
                                   }
                               });
                               int countpaidnight2 = 0;
                               Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                               {
                                   if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                   {
                                       countpaidnight2 = countpaidnight2 + 1;
                                   }
                               });
                               
                               group++;
                               decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                               decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                               decimal totalp = totalamt + totalamt2;
                               str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                               new XElement("Room",
                                new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                new XAttribute("SuppliersID", "2"),
                                new XAttribute("RoomSeq", "1"),
                                new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                new XAttribute("MealPlanCode", ""),
                                new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                new XAttribute("PerNightRoomRate", Convert.ToString(totalamt/countpaidnight1)),
                                new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                new XAttribute("CancellationDate", ""),
                                new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                new XElement("RequestID", Convert.ToString("")),
                                new XElement("Offers", ""),
                                 new XElement("PromotionList", 
                                new XElement("Promotions", Convert.ToString(promotion))),
                                new XElement("CancellationPolicy", ""),
                                new XElement("Amenities", new XElement("Amenity", "")),
                                new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                new XElement("Supplements",
                                    GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                    ),
                                    new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                    new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                    new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                ),

                               new XElement("Room",
                                new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                new XAttribute("SuppliersID", "2"),
                                new XAttribute("RoomSeq", "2"),
                                new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                new XAttribute("MealPlanCode", ""),
                                new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2/countpaidnight2)),
                                new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                new XAttribute("CancellationDate", ""),
                                new XAttribute("CancellationAmount", ""),
                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                new XElement("RequestID", Convert.ToString("")),
                                new XElement("Offers", ""),
                                 new XElement("PromotionList", 
                                new XElement("Promotions", Convert.ToString(promotion))),
                                new XElement("CancellationPolicy", ""),
                                new XElement("Amenities", new XElement("Amenity", "")),
                                new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                new XElement("Supplements",
                                    GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                    ),
                                    new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                    new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                    new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                )));
                           });
                           #endregion
                       }

                       //return str;
                       #endregion

                   });
               });
                return str;
            }
            #endregion
            #region Room Count 3
            if (totalroom == 3)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>                    
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                           
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                           
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                //List<Tourico.Occupancy> roomList1 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 1).ToList();
                //List<Tourico.Occupancy> roomList2 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 2).ToList();
                //List<Tourico.Occupancy> roomList3 = roomlist.Occupancies.Where(x => x.Rooms[0].seqNum == 3).ToList();
                #region Room 3
                Parallel.For(0, roomList1.Count(), m =>
                //for (int m = 0; m < roomList1.Count(); m++)
                {
                    Parallel.For(0, roomList2.Count(), n =>
                    //for (int n = 0; n < roomList2.Count(); n++)
                    {
                        Parallel.For(0, roomList3.Count(), o =>
                        //for (int o = 0; o < roomList3.Count(); o++)
                        {
                            // add room 1, 2, 3

                            #region room's group

                            int bb = roomlist.Occupancies[m].BoardBases.Count();
                            List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                            List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                            string promotion = string.Empty;
                            if (roomlist.Discount != null)
                            {
                                #region Discount (Tourico) Amount already subtracted
                                try
                                {
                                    XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                    XmlDocument doc3 = new XmlDocument();
                                    System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                    XmlWriter writer3 = XmlWriter.Create(sww3);
                                    xsSubmit3.Serialize(writer3, roomlist.Discount);
                                    var typxsd = XDocument.Parse(sww3.ToString());
                                    var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                    var distype = typxsd.Root.Attribute(disprefix + "type");
                                    if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                    {
                                        promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                    }
                                    else
                                    {
                                        promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                                #endregion
                            }

                            if (bb == 0)
                            {
                                group++;
                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice;
                                #region No Board Bases
                                str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList", 
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                     ),

                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                     ),

                                new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "3"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", ""),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(),0)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                     )));
                                #endregion
                            }
                            else
                            {
                                #region Board Bases >0
                                Parallel.For(0, bb, j =>
                                //for (int j = 0; j < bb; j++)
                                {
                                    int countpaidnight1 = 0;
                                    Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight1 = countpaidnight1 + 1;
                                        }
                                    });
                                    int countpaidnight2 = 0;
                                    Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight2 = countpaidnight2 + 1;
                                        }
                                    });
                                    int countpaidnight3 = 0;
                                    Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                    {
                                        if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                        {
                                            countpaidnight3 = countpaidnight3 + 1;
                                        }
                                    });
                                    

                                    group++;
                                    decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                    decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                    decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                    decimal totalp = totalamt + totalamt2 + totalamt3;

                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "1"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt/countpaidnight1)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "2"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2/countpaidnight2)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                     ),

                                    new XElement("Room",
                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                     new XAttribute("SuppliersID", "2"),
                                     new XAttribute("RoomSeq", "3"),
                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                     new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                     new XAttribute("MealPlanCode", ""),
                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                     new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3/countpaidnight3)),
                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                     new XAttribute("CancellationDate", ""),
                                     new XAttribute("CancellationAmount", ""),
                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                     new XElement("RequestID", Convert.ToString("")),
                                     new XElement("Offers", ""),
                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                     new XElement("CancellationPolicy", ""),
                                     new XElement("Amenities", new XElement("Amenity", "")),
                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                     new XElement("Supplements",
                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                         ),
                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                     )));
                                });
                                #endregion
                            }

                            //return str;
                            #endregion
                        });
                    });
                });
                return str;
                #endregion

            }
            #endregion
            #region Room Count 4
            if (totalroom == 4)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                          
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 4
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                // add room 1, 2, 3, 4
                                
                                #region room's group

                                int bb = roomlist.Occupancies[m].BoardBases.Count();
                                List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                string promotion = string.Empty;
                                if (roomlist.Discount != null)
                                {
                                    #region Discount (Tourico) Amount already subtracted
                                    try
                                    {
                                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                        XmlDocument doc3 = new XmlDocument();
                                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                        XmlWriter writer3 = XmlWriter.Create(sww3);
                                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                                        var typxsd = XDocument.Parse(sww3.ToString());
                                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                        var distype = typxsd.Root.Attribute(disprefix + "type");
                                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                        {
                                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                        }
                                        else
                                        {
                                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                        }
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    #endregion
                                }

                                if (bb == 0)
                                {
                                    group++;
                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice;

                                    #region No Board Bases
                                    str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(),0)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                    new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", ""),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(),0)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    #endregion
                                }
                                else
                                {
                                    #region Board Bases >0
                                    for (int j = 0; j < bb; j++)
                                    {
                                        int countpaidnight1 = 0;
                                        Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight1 = countpaidnight1 + 1;
                                            }
                                        });
                                        int countpaidnight2 = 0;
                                        Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight2 = countpaidnight2 + 1;
                                            }
                                        });
                                        int countpaidnight3 = 0;
                                        Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight3 = countpaidnight3 + 1;
                                            }
                                        });
                                        int countpaidnight4 = 0;
                                        Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                        {
                                            if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                            {
                                                countpaidnight4 = countpaidnight4 + 1;
                                            }
                                        });
                                        
                                        group++;
                                        decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                        decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                        decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                        decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4;

                                        str.Add(new XElement("RoomTypes",  new XAttribute("TotalRate", totalp),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "1"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt/countpaidnight1)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "2"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2/countpaidnight2)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "3"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3/countpaidnight3)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                         ),

                                        new XElement("Room",
                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                         new XAttribute("SuppliersID", "2"),
                                         new XAttribute("RoomSeq", "4"),
                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                         new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                         new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                         new XAttribute("MealPlanCode", ""),
                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                         new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4/countpaidnight4)),
                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                         new XAttribute("CancellationDate", ""),
                                         new XAttribute("CancellationAmount", ""),
                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                         new XElement("RequestID", Convert.ToString("")),
                                         new XElement("Offers", ""),
                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                         new XElement("CancellationPolicy", ""),
                                         new XElement("Amenities", new XElement("Amenity", "")),
                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                         new XElement("Supplements",
                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                             ),
                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                         )));
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                        }
                    }
                }
                return str;
                #endregion

            }
            #endregion
            #region Room Count 5
            if (totalroom == 5)
            {

                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 5
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    // add room 1, 2, 3, 4, 5
                                    
                                    #region room's group

                                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                    string promotion = string.Empty;
                                    if (roomlist.Discount != null)
                                    {
                                        #region Discount (Tourico) Amount already subtracted
                                        try
                                        {
                                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                            XmlDocument doc3 = new XmlDocument();
                                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                            XmlWriter writer3 = XmlWriter.Create(sww3);
                                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                                            var typxsd = XDocument.Parse(sww3.ToString());
                                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                            var distype = typxsd.Root.Attribute(disprefix + "type");
                                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                            {
                                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                            }
                                            else
                                            {
                                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                            }
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                        #endregion
                                    }

                                    if (bb == 0)
                                    {
                                        group++;
                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice;

                                        #region No Board Bases
                                        str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),
                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(),0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(),0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                             ),

                                        new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "5"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", ""),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(),0)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        #endregion
                                    }
                                    else
                                    {
                                        #region Board Bases >0
                                        for (int j = 0; j < bb; j++)
                                        {
                                            int countpaidnight1 = 0;
                                            Parallel.For(0, roomList1[m].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList1[m].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight1 = countpaidnight1 + 1;
                                                }
                                            });
                                            int countpaidnight2 = 0;
                                            Parallel.For(0, roomList2[n].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList2[n].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight2 = countpaidnight2 + 1;
                                                }
                                            });
                                            int countpaidnight3 = 0;
                                            Parallel.For(0, roomList3[o].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList3[o].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight3 = countpaidnight3 + 1;
                                                }
                                            });
                                            int countpaidnight4 = 0;
                                            Parallel.For(0, roomList4[p].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList4[p].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight4 = countpaidnight4 + 1;
                                                }
                                            });
                                            int countpaidnight5 = 0;
                                            Parallel.For(0, roomList5[q].PriceBreakdown.Count(), jj =>
                                            {
                                                if (roomList5[q].PriceBreakdown[jj].valuePublish != 0)
                                                {
                                                    countpaidnight5 = countpaidnight5 + 1;
                                                }
                                            });
                                            group++;
                                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                            decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                            decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                            decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                            decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5;

                                            str.Add(new XElement("RoomTypes", new XAttribute("TotalRate", totalp),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "1"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt/countpaidnight1)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "2"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt2/countpaidnight2)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "3"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt3/countpaidnight3)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "4"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt4/countpaidnight4)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                             ),

                                            new XElement("Room",
                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                             new XAttribute("SuppliersID", "2"),
                                             new XAttribute("RoomSeq", "5"),
                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                             new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                             new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                             new XAttribute("MealPlanCode", ""),
                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                             new XAttribute("PerNightRoomRate", Convert.ToString(totalamt5/countpaidnight5)),
                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                             new XAttribute("CancellationDate", ""),
                                             new XAttribute("CancellationAmount", ""),
                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                             new XElement("RequestID", Convert.ToString("")),
                                             new XElement("Offers", ""),
                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                             new XElement("CancellationPolicy", ""),
                                             new XElement("Amenities", new XElement("Amenity", "")),
                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                             new XElement("Supplements",
                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                 ),
                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                             )));
                                        }
                                        #endregion
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 6
            if (totalroom == 6)
            {

                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 6
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        // add room 1, 2, 3, 4, 5, 6
                                        
                                        #region room's group

                                        int bb = roomlist.Occupancies[m].BoardBases.Count();
                                        List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                        List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                        string promotion = string.Empty;
                                        if (roomlist.Discount != null)
                                        {
                                            #region Discount (Tourico) Amount already subtracted
                                            try
                                            {
                                                XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                XmlDocument doc3 = new XmlDocument();
                                                System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                XmlWriter writer3 = XmlWriter.Create(sww3);
                                                xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                var typxsd = XDocument.Parse(sww3.ToString());
                                                var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                var distype = typxsd.Root.Attribute(disprefix + "type");
                                                if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                {
                                                    promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                }
                                                else
                                                {
                                                    promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                }
                                            }
                                            catch (Exception ex)
                                            {

                                            }
                                            #endregion
                                        }

                                        if (bb == 0)
                                        {
                                            group++;
                                            decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice;

                                            #region No Board Bases
                                            str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                 new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(),0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(),0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                 new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(),0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                 ),

                                            new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "6"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString("")),
                                                 new XAttribute("MealPlanName", Convert.ToString("")),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", ""),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                 new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(),0)),
                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                 )));
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Board Bases >0
                                            for (int j = 0; j < bb; j++)
                                            {
                                                group++;
                                                decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6;

                                                str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "1"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "2"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "3"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "4"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "5"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                 ),

                                                new XElement("Room",
                                                 new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                 new XAttribute("SuppliersID", "2"),
                                                 new XAttribute("RoomSeq", "6"),
                                                 new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                 new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                 new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                 new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                 new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                 new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                 new XAttribute("MealPlanCode", ""),
                                                 new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                 new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                 new XAttribute("CancellationDate", ""),
                                                 new XAttribute("CancellationAmount", ""),
                                                  new XAttribute("isAvailable", roomlist.isAvailable),
                                                 new XElement("RequestID", Convert.ToString("")),
                                                 new XElement("Offers", ""),
                                                  new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                 new XElement("CancellationPolicy", ""),
                                                 new XElement("Amenities", new XElement("Amenity", "")),
                                                 new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                 new XElement("Supplements",
                                                     GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                     ),
                                                     new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                     new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                 )));
                                            }
                                            #endregion
                                        }
                                        #endregion

                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 7
            if (totalroom == 7)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
                

                #region Room 7
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            // add room 1, 2, 3, 4, 5, 6, 7
                                           
                                            #region room's group

                                            int bb = roomlist.Occupancies[m].BoardBases.Count();
                                            List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                            List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                            string promotion = string.Empty;
                                            if (roomlist.Discount != null)
                                            {
                                                #region Discount (Tourico) Amount already subtracted
                                                try
                                                {
                                                    XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                    XmlDocument doc3 = new XmlDocument();
                                                    System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                    XmlWriter writer3 = XmlWriter.Create(sww3);
                                                    xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                    var typxsd = XDocument.Parse(sww3.ToString());
                                                    var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                    var distype = typxsd.Root.Attribute(disprefix + "type");
                                                    if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                    {
                                                        promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                    }
                                                    else
                                                    {
                                                        promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                    }
                                                }
                                                catch (Exception ex)
                                                {

                                                }
                                                #endregion
                                            }

                                            if (bb == 0)
                                            {
                                                group++;
                                                decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice;

                                                #region No Board Bases
                                                str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(),0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(),0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(),0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(),0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                     ),

                                                new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "7"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString("")),
                                                     new XAttribute("MealPlanName", Convert.ToString("")),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", ""),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                     new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(),0)),
                                                         new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                     )));
                                                #endregion
                                            }
                                            else
                                            {
                                                #region Board Bases >0
                                                for (int j = 0; j < bb; j++)
                                                {
                                                    group++;
                                                    decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                    decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                    decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7;

                                                    str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "1"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "2"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "3"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "4"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "5"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                     new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "6"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                     ),

                                                    new XElement("Room",
                                                     new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                     new XAttribute("SuppliersID", "2"),
                                                     new XAttribute("RoomSeq", "7"),
                                                     new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                     new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                     new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                     new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                     new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                     new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                     new XAttribute("MealPlanCode", ""),
                                                     new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                     new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                     new XAttribute("CancellationDate", ""),
                                                     new XAttribute("CancellationAmount", ""),
                                                      new XAttribute("isAvailable", roomlist.isAvailable),
                                                     new XElement("RequestID", Convert.ToString("")),
                                                     new XElement("Offers", ""),
                                                      new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                     new XElement("CancellationPolicy", ""),
                                                     new XElement("Amenities", new XElement("Amenity", "")),
                                                     new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                     new XElement("Supplements",
                                                         GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                         ),
                                                         new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                         new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                     )));
                                                }
                                                #endregion
                                            }
                                            #endregion
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 8
            if (totalroom == 8)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 8)
                        {
                            roomList8.Add(roomlist.Occupancies[i]);
                        }
                    });

                });
               

                #region Room 8
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                // add room 1, 2, 3, 4, 5, 6, 7, 8
                                               
                                                #region room's group

                                                int bb = roomlist.Occupancies[m].BoardBases.Count();
                                                List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                                List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                                string promotion = string.Empty;
                                                if (roomlist.Discount != null)
                                                {
                                                    #region Discount (Tourico) Amount already subtracted
                                                    try
                                                    {
                                                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                        XmlDocument doc3 = new XmlDocument();
                                                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                        XmlWriter writer3 = XmlWriter.Create(sww3);
                                                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                        var typxsd = XDocument.Parse(sww3.ToString());
                                                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                        var distype = typxsd.Root.Attribute(disprefix + "type");
                                                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                        {
                                                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                        }
                                                        else
                                                        {
                                                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {

                                                    }
                                                    #endregion
                                                }

                                                if (bb == 0)
                                                {
                                                    group++;
                                                    decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice;

                                                    #region No Board Bases
                                                    str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "1"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "2"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "3"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "4"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "5"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "6"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "7"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                         ),

                                                    new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "8"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString("")),
                                                         new XAttribute("MealPlanName", Convert.ToString("")),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", ""),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPublishPrice)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                         new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(),0)),
                                                             new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                         )));
                                                    #endregion
                                                }
                                                else
                                                {
                                                    #region Board Bases >0
                                                    for (int j = 0; j < bb; j++)
                                                    {
                                                        group++;
                                                        decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                        decimal totalamt8 = roomList8[t].occupPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice;
                                                        decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8;

                                                        str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "1"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "2"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "3"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "4"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "5"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "6"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                          new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "7"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                         ),

                                                        new XElement("Room",
                                                         new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                         new XAttribute("SuppliersID", "2"),
                                                         new XAttribute("RoomSeq", "8"),
                                                         new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                         new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                         new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                         new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                         new XAttribute("MealPlanID", Convert.ToString(roomList8[t].BoardBases[j].bbId)),
                                                         new XAttribute("MealPlanName", Convert.ToString(roomList8[t].BoardBases[j].bbName)),
                                                         new XAttribute("MealPlanCode", ""),
                                                         new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice)),
                                                         new XAttribute("TotalRoomRate", Convert.ToString(totalamt8)),
                                                         new XAttribute("CancellationDate", ""),
                                                         new XAttribute("CancellationAmount", ""),
                                                          new XAttribute("isAvailable", roomlist.isAvailable),
                                                         new XElement("RequestID", Convert.ToString("")),
                                                         new XElement("Offers", ""),
                                                         new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                         new XElement("CancellationPolicy", ""),
                                                         new XElement("Amenities", new XElement("Amenity", "")),
                                                         new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                         new XElement("Supplements",
                                                             GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                             ),
                                                             new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                             new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                         )));
                                                    }
                                                    #endregion
                                                }
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion
            #region Room Count 9
            if (totalroom == 9)
            {
                int group = 0;
                Parallel.For(0, roomlist.Occupancies.Count(), i =>
                {
                    Parallel.For(0, roomlist.Occupancies[i].Rooms.Count(), j =>
                    {
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 1)
                        {
                            roomList1.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 2)
                        {
                            roomList2.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 3)
                        {
                            roomList3.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 4)
                        {
                            roomList4.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 5)
                        {
                            roomList5.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 6)
                        {
                            roomList6.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 7)
                        {
                            roomList7.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 8)
                        {
                            roomList8.Add(roomlist.Occupancies[i]);
                        }
                        if (roomlist.Occupancies[i].Rooms[j].seqNum == 9)
                        {
                            roomList9.Add(roomlist.Occupancies[i]);
                        }
                    });

                });

                #region Room 9
                for (int m = 0; m < roomList1.Count(); m++)
                {
                    for (int n = 0; n < roomList2.Count(); n++)
                    {
                        for (int o = 0; o < roomList3.Count(); o++)
                        {
                            for (int p = 0; p < roomList4.Count(); p++)
                            {
                                for (int q = 0; q < roomList5.Count(); q++)
                                {
                                    for (int r = 0; r < roomList6.Count(); r++)
                                    {
                                        for (int s = 0; s < roomList7.Count(); s++)
                                        {
                                            for (int t = 0; t < roomList8.Count(); t++)
                                            {
                                                for (int u = 0; u < roomList8.Count(); u++)
                                                {
                                                    // add room 1, 2, 3, 4, 5, 6, 7, 8, 9
                                                    
                                                    #region room's group

                                                    int bb = roomlist.Occupancies[m].BoardBases.Count();
                                                    List<Tourico.Supplement> supplements = roomlist.Occupancies[m].SelctedSupplements.ToList();
                                                    List<Tourico.Price> pricebrkups = roomlist.Occupancies[m].PriceBreakdown.ToList();
                                                    string promotion = string.Empty;
                                                    if (roomlist.Discount != null)
                                                    {
                                                        #region Discount (Tourico) Amount already subtracted
                                                        try
                                                        {
                                                            XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                                                            XmlDocument doc3 = new XmlDocument();
                                                            System.IO.StringWriter sww3 = new System.IO.StringWriter();
                                                            XmlWriter writer3 = XmlWriter.Create(sww3);
                                                            xsSubmit3.Serialize(writer3, roomlist.Discount);
                                                            var typxsd = XDocument.Parse(sww3.ToString());
                                                            var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                                                            var distype = typxsd.Root.Attribute(disprefix + "type");
                                                            if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                                                            {
                                                                promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                                                            }
                                                            else
                                                            {
                                                                promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {

                                                        }
                                                        #endregion
                                                    }

                                                    if (bb == 0)
                                                    {
                                                        group++;
                                                        decimal totalp = roomList1[m].occupPublishPrice + roomList2[n].occupPublishPrice + roomList3[o].occupPublishPrice + roomList4[p].occupPublishPrice + roomList5[q].occupPublishPrice + roomList6[r].occupPublishPrice + roomList7[s].occupPublishPrice + roomList8[t].occupPublishPrice + roomList9[u].occupPublishPrice;

                                                        #region No Board Bases
                                                        str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),
                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "1"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList1[m].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                             new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "2"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList2[n].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "3"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList3[o].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "4"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList4[p].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "5"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList5[q].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "6"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList6[r].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "7"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList7[s].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "8"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList8[t].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                             ),

                                                        new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "9"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList9[u].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString("")),
                                                             new XAttribute("MealPlanName", Convert.ToString("")),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", ""),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].avrNightPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(roomList9[u].occupPublishPrice)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                             new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList9[u].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(),0)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList9[u].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList9[u].Rooms[0].ChildNum))
                                                             )));
                                                        #endregion
                                                    }
                                                    else
                                                    {
                                                        #region Board Bases >0
                                                        for (int j = 0; j < bb; j++)
                                                        {
                                                            group++;
                                                            decimal totalamt = roomList1[m].occupPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt2 = roomList2[n].occupPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt3 = roomList3[o].occupPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt4 = roomList4[p].occupPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt5 = roomList5[q].occupPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt6 = roomList6[r].occupPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt7 = roomList7[s].occupPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt8 = roomList8[t].occupPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice;
                                                            decimal totalamt9 = roomList9[u].occupPublishPrice + roomList9[u].BoardBases[j].bbPublishPrice;
                                                            decimal totalp = totalamt + totalamt2 + totalamt3 + totalamt4 + totalamt5 + totalamt6 + totalamt7 + totalamt8 + totalamt9;

                                                            str.Add(new XElement("RoomTypes", new XAttribute("Index", group), new XAttribute("TotalRate", totalp),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "1"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList1[m].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList1[m].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList1[m].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList1[m].avrNightPublishPrice + roomList1[m].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList1[m].PriceBreakdown.ToList(), roomList1[m].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList1[m].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList1[m].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "2"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList2[n].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList2[n].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList2[n].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList2[n].avrNightPublishPrice + roomList2[n].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt2)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList2[n].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList2[n].PriceBreakdown.ToList(), roomList2[n].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList2[n].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList2[n].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "3"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList3[o].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList3[o].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList3[o].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList3[o].avrNightPublishPrice + roomList3[o].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt3)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList3[o].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList3[o].PriceBreakdown.ToList(), roomList3[o].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList3[o].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList3[o].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "4"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList4[p].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList4[p].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList4[p].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList4[p].avrNightPublishPrice + roomList4[p].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt4)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList4[p].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList4[p].PriceBreakdown.ToList(), roomList4[p].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList4[p].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList4[p].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "5"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList5[q].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList5[q].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList5[q].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList5[q].avrNightPublishPrice + roomList5[q].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt5)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList5[q].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList5[q].PriceBreakdown.ToList(), roomList5[q].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList5[q].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList5[q].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "6"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList6[r].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList6[r].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList6[r].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList6[r].avrNightPublishPrice + roomList6[r].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt6)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList6[r].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList6[r].PriceBreakdown.ToList(), roomList6[r].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList6[r].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList6[r].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "7"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList7[s].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList7[s].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList7[s].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList7[s].avrNightPublishPrice + roomList7[s].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt7)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList7[s].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList7[s].PriceBreakdown.ToList(), roomList7[s].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList7[s].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList7[s].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "8"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList8[t].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList8[t].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList8[t].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList8[t].avrNightPublishPrice + roomList8[t].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt8)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList8[t].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList8[t].PriceBreakdown.ToList(), roomList8[t].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList8[t].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList8[t].Rooms[0].ChildNum))
                                                             ),

                                                            new XElement("Room",
                                                             new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                                                             new XAttribute("SuppliersID", "2"),
                                                             new XAttribute("RoomSeq", "9"),
                                                             new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                                                             new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                                                             new XAttribute("OccupancyID", Convert.ToString(roomList9[u].bedding)),
                                                             new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                                                             new XAttribute("MealPlanID", Convert.ToString(roomList9[u].BoardBases[j].bbId)),
                                                             new XAttribute("MealPlanName", Convert.ToString(roomList9[u].BoardBases[j].bbName)),
                                                             new XAttribute("MealPlanCode", ""),
                                                             new XAttribute("MealPlanPrice", Convert.ToString(roomList9[u].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("PerNightRoomRate", Convert.ToString(roomList9[u].avrNightPublishPrice + roomList9[u].BoardBases[j].bbPublishPrice)),
                                                             new XAttribute("TotalRoomRate", Convert.ToString(totalamt9)),
                                                             new XAttribute("CancellationDate", ""),
                                                             new XAttribute("CancellationAmount", ""),
                                                              new XAttribute("isAvailable", roomlist.isAvailable),
                                                             new XElement("RequestID", Convert.ToString("")),
                                                             new XElement("Offers", ""),
                                                              new XElement("PromotionList",
                                     new XElement("Promotions", Convert.ToString(promotion))),
                                                             new XElement("CancellationPolicy", ""),
                                                             new XElement("Amenities", new XElement("Amenity", "")),
                                                             new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                                                             new XElement("Supplements",
                                                                 GetRoomsSupplementTourico(roomList9[u].SelctedSupplements.ToList())
                                                                 ),
                                                                 new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(roomList9[u].PriceBreakdown.ToList(), roomList9[u].BoardBases[j].bbPublishPrice)),
                                                                 new XElement("AdultNum", Convert.ToString(roomList9[u].Rooms[0].AdultNum)),
                                                                 new XElement("ChildNum", Convert.ToString(roomList9[u].Rooms[0].ChildNum))
                                                             )));
                                                        }
                                                        #endregion
                                                    }
                                                    #endregion
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return str;
                #endregion
            }
            #endregion

            return str;
        }
        #endregion
        #region Tourico Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomsTouricoMultiBoard(Tourico.RoomType roomlist, int j)
        {
            List<XElement> str = new List<XElement>();
            Parallel.For(0, roomlist.Occupancies.Count(), i =>
            {
                int roomcount = roomlist.Occupancies[0].Rooms.Count();
                //int bb = roomlist.Occupancies[i].BoardBases.Count();
                //int boardbaseid = roomlist.Occupancies[i].BoardBases[i].bbId;
                List<Tourico.Supplement> supplements = roomlist.Occupancies[i].SelctedSupplements.ToList();
                List<Tourico.Price> pricebrkups = roomlist.Occupancies[i].PriceBreakdown.ToList();
                string promotion = string.Empty;
                if (roomlist.Discount != null)
                {
                    #region Discount (Tourico) Amount already subtracted
                    try
                    {
                        XmlSerializer xsSubmit3 = new XmlSerializer(typeof(Tourico.Promotion));
                        XmlDocument doc3 = new XmlDocument();
                        System.IO.StringWriter sww3 = new System.IO.StringWriter();
                        XmlWriter writer3 = XmlWriter.Create(sww3);
                        xsSubmit3.Serialize(writer3, roomlist.Discount);
                        var typxsd = XDocument.Parse(sww3.ToString());
                        var disprefix = typxsd.Root.GetNamespaceOfPrefix("xsi");
                        var distype = typxsd.Root.Attribute(disprefix + "type");
                        if (Convert.ToString(distype.Value) == "q1:ProgressivePromotion")
                        {
                            promotion = typxsd.Root.Attribute("name").Value + " " + typxsd.Root.Attribute("value").Value + " " + typxsd.Root.Attribute("type").Value + " off";
                        }
                        else
                        {
                            promotion = "Stay for " + typxsd.Root.Attribute("stay").Value + " Nights and Pay for " + typxsd.Root.Attribute("pay").Value + " Nights only";
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    #endregion
                }
                Parallel.For(0, roomcount, p =>
           {
               #region Board Bases >0
               //Parallel.For(0, bb, j =>
               //{
               decimal bbprice = roomlist.Occupancies[i].BoardBases[j].bbPublishPrice;
               int bbid = roomlist.Occupancies[i].BoardBases[j].bbId;
               string bbname = roomlist.Occupancies[i].BoardBases[j].bbName;

               decimal totalamt = roomlist.Occupancies[i].occupPublishPrice + bbprice;
               //if (bbid == boardbaseid)
               {
                   str.Add(new XElement("Room",
                    new XAttribute("ID", Convert.ToString(roomlist.hotelRoomTypeId)),
                    new XAttribute("SuppliersID", "2"),
                    new XAttribute("SessionID", Convert.ToString(roomlist.roomId)),
                    new XAttribute("RoomType", Convert.ToString(roomlist.roomTypeCategory)),
                    new XAttribute("OccupancyID", Convert.ToString(roomlist.Occupancies[i].bedding)),
                    new XAttribute("OccupancyName", Convert.ToString(roomlist.name)),
                    new XAttribute("MealPlanID", Convert.ToString(bbid)),
                    new XAttribute("MealPlanName", Convert.ToString(bbname)),
                    new XAttribute("MealPlanCode", ""),
                    new XAttribute("MealPlanPrice", Convert.ToString(bbprice)),
                    new XAttribute("PerNightRoomRate", Convert.ToString(roomlist.Occupancies[i].avrNightPublishPrice)),
                    new XAttribute("TotalRoomRate", Convert.ToString(totalamt)),
                    new XAttribute("CancellationDate", ""),
                    new XAttribute("CancellationAmount", ""),
                     new XAttribute("isAvailable", roomlist.isAvailable),
                    new XElement("RequestID", Convert.ToString("")),
                    new XElement("Offers", ""),
                    new XElement("Promotions", Convert.ToString(promotion)),
                    new XElement("CancellationPolicy", ""),
                    new XElement("Amenities", new XElement("Amenity", "")),
                    new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                    new XElement("Supplements",
                        GetRoomsSupplementTourico(supplements)
                        ),
                        new XElement("PriceBreakups", GetRoomsPriceBreakupTourico(pricebrkups,0)),
                        new XElement("AdultNum", Convert.ToString(roomlist.Occupancies[i].Rooms[0].AdultNum)),
                        new XElement("ChildNum", Convert.ToString(roomlist.Occupancies[i].Rooms[0].ChildNum))
                    ));
               }
               //});
               #endregion
           });
            });
            return str;
        }
        #endregion
        #region Tourico Room's Supplements
        private IEnumerable<XElement> GetRoomsSupplementTourico(List<Tourico.Supplement> supplements)
        {
            #region Tourico Supplements
            List<XElement> str = new List<XElement>();

            Parallel.For(0, supplements.Count(), i =>
            {

                Tourico.Supplement ss = supplements[i];

                XmlSerializer xsSubmit = new XmlSerializer(typeof(Tourico.Supplement));
                XmlDocument doc = new XmlDocument();
                System.IO.StringWriter sww = new System.IO.StringWriter();
                XmlWriter writer = XmlWriter.Create(sww);
                xsSubmit.Serialize(writer, ss);
                var xsd = XDocument.Parse(sww.ToString());
                var prefix = xsd.Root.GetNamespaceOfPrefix("xsi");
                var type = xsd.Root.Attribute(prefix + "type");
                if (Convert.ToString(type.Value) == "q1:PerPersonSupplement")
                {
                    var agegroup = XDocument.Parse(sww.ToString());

                    var prefixage = agegroup.Root.GetNamespaceOfPrefix("q1");

                    List<XElement> totalsupp = agegroup.Root.Descendants(prefixage + "SupplementAge").ToList();

                    str.Add(new XElement("Supplement",
                        new XAttribute("suppId", Convert.ToString(supplements[i].suppId)),
                        new XAttribute("suppName", Convert.ToString(supplements[i].suppName)),
                        new XAttribute("supptType", Convert.ToString(supplements[i].supptType)),
                        new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].suppIsMandatory)),
                        new XAttribute("suppChargeType", Convert.ToString(supplements[i].suppChargeType)),
                        new XAttribute("suppPrice", Convert.ToString(supplements[i].publishPrice)),
                        new XAttribute("suppType", Convert.ToString(type.Value)),
                        new XElement("SuppAgeGroup",
                            GetRoomsSupplementAgeGroupTourico(totalsupp)
                            )
                        ));
                }
                else
                {
                    str.Add(new XElement("Supplement",
                        new XAttribute("suppId", Convert.ToString(supplements[i].suppId)),
                        new XAttribute("suppName", Convert.ToString(supplements[i].suppName)),
                        new XAttribute("supptType", Convert.ToString(supplements[i].supptType)),
                        new XAttribute("suppIsMandatory", Convert.ToString(supplements[i].suppIsMandatory)),
                        new XAttribute("suppChargeType", Convert.ToString(supplements[i].suppChargeType)),
                        new XAttribute("suppPrice", Convert.ToString(supplements[i].publishPrice)),
                        new XAttribute("suppType", Convert.ToString(type.Value)))
                     );
                }
            });

            return str;
            #endregion
        }
        #endregion
        #region Tourico Room Supplement's Age Group
        private IEnumerable<XElement> GetRoomsSupplementAgeGroupTourico(List<XElement> supplementagegroup)
        {
            #region Tourico Supplements Age Group
            List<XElement> str = new List<XElement>();

            Parallel.For(0, supplementagegroup.Count(), i =>
            {
                str.Add(new XElement("SupplementAge",
                       new XAttribute("suppFrom", Convert.ToString(supplementagegroup[i].Attribute("suppFrom").Value)),
                       new XAttribute("suppTo", Convert.ToString(supplementagegroup[i].Attribute("suppTo").Value)),
                       new XAttribute("suppQuantity", Convert.ToString(supplementagegroup[i].Attribute("suppQuantity").Value)),
                       new XAttribute("suppPrice", Convert.ToString(supplementagegroup[i].Attribute("suppPrice").Value)))
                );
            });
            return str;
            #endregion
        }
        #endregion
        #region Tourico Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupTourico(List<Tourico.Price> pricebreakups,decimal mealprice)
        {
            #region Tourico Room's Price Breakups
            List<XElement> str = new List<XElement>();
            int countpaidnight=0;
            Parallel.For(0, pricebreakups.Count(), j =>
                {
                    if (pricebreakups[j].valuePublish != 0)
                    {
                        countpaidnight = countpaidnight + 1;
                    }
                });
            decimal mealpricepernight = mealprice / countpaidnight;

            Parallel.For(0, pricebreakups.Count(), i =>
            {
                if (pricebreakups[i].valuePublish != 0)
                {
                    str.Add(new XElement("Price",
                           new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                           new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].valuePublish + mealpricepernight)))
                    );
                }
                else
                {
                    str.Add(new XElement("Price",
                          new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                          new XAttribute("PriceValue", Convert.ToString(pricebreakups[i].valuePublish)))
                   );
                }
            });
            return str;
            #endregion
        }
        #endregion
        #endregion
        #region Methods for Darina Holidays
        public string CallWebService(XElement req, string url, string action, string flag)
        {
            try
            {
                var _url = url;
                var _action = action;
                XDocument soapEnvelopeXml = new XDocument();
                if (flag == "htlist")
                {
                    WriteToFile("Availability Request start");
                    soapEnvelopeXml = CreateSoapEnvelopehtlist(req);
                    WriteToFile(soapEnvelopeXml.ToString());
                    WriteToFile("Availability Request end");
                }
                if (flag == "htdetail")
                {
                    soapEnvelopeXml = CreateSoapEnvelopehtdetail(req);
                }
                HttpWebRequest webRequest = CreateWebRequest(_url, _action);
                InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);
                IAsyncResult asyncResult = webRequest.BeginGetResponse(null, null);
                asyncResult.AsyncWaitHandle.WaitOne();
                string soapResult;
                using (WebResponse webResponse = webRequest.EndGetResponse(asyncResult))
                {
                    using (StreamReader rd = new StreamReader(webResponse.GetResponseStream()))
                    {
                        soapResult = rd.ReadToEnd();
                    }
                    return soapResult;
                }
            }
            catch (WebException webex)
            {
                WebResponse errResp = webex.Response;
                using (Stream respStream = errResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(respStream);
                    string text = reader.ReadToEnd();
                    return text;
                }
            }
        }
        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction", action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }
        private static XDocument CreateSoapEnvelopehtlist(XElement req)
        {
            int totalchild = Convert.ToInt32(req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value);
            string mainchildrenages = "";
            string childrenages = "";
            if (totalchild > 0)
            {
                List<XElement> childage = req.Descendants("RoomPax").Descendants("ChildAge").ToList();
                for (int i = 0; i < childage.Count(); i++)
                {
                    childrenages = childrenages + "<int>" + Convert.ToString(childage[i].Value) + "</int>";
                }
                mainchildrenages = "<ChildrenAges>" + childrenages + "</ChildrenAges>";
            }
            else
            {
                mainchildrenages = "<ChildrenAges />";
            }
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                         "<soap:Body>" +
                           "<CheckAvailability xmlns='http://travelcontrol.softexsw.us/'>" +
                             "<SecStr>#C|559341#W#274298</SecStr>" +
                             "<AccountName>DTC</AccountName>" +
                             "<UserName>XML2016Ra</UserName>" +
                             "<Password>DarinAH</Password>" +
                             "<AgentID>1701</AgentID>" +
                             "<FromDate>" + req.Descendants("FromDate").Single().Value + "</FromDate>" +
                             "<ToDate>" + req.Descendants("ToDate").Single().Value + "</ToDate>" +
                             "<CountryID>" + req.Descendants("CountryID").Single().Value + "</CountryID>" +
                             "<CityID>" + req.Descendants("CityID").Single().Value + "</CityID>" + // 50
                             "<AreaID>0</AreaID>" +
                             "<AreaName></AreaName>" +
                             "<MinLandCategory>" + req.Descendants("MinStarRating").Single().Value + "</MinLandCategory>" +
                             "<MaxLandCategory>" + req.Descendants("MaxStarRating").Single().Value + "</MaxLandCategory>" +
                             "<PropertyType>0</PropertyType>" +
                             "<HotelName></HotelName>" +
                             "<OccupancyID>0</OccupancyID>" +
                             "<AdultPax>" + req.Descendants("RoomPax").Descendants("Adult").FirstOrDefault().Value + "</AdultPax>" +
                             "<ChildPax>" + req.Descendants("RoomPax").Descendants("Child").FirstOrDefault().Value + "</ChildPax>" +
                                    mainchildrenages +
                             "<ExtraBedAdult>false</ExtraBedAdult>" +
                             "<ExtraBedChild>false</ExtraBedChild>" +
                             "<Nationality_CountryID>" + req.Descendants("PaxNationality_CountryID").Single().Value + "</Nationality_CountryID>" +
                             "<CurrencyID>" + req.Descendants("CurrencyID").Single().Value + "</CurrencyID>" +
                             "<MaxOverallPrice>0</MaxOverallPrice>" +
                             "<Availability>ShowAvailableOnly</Availability>" +
                             "<RoomType>0</RoomType>" +
                             "<MealPlan>0</MealPlan>" +
                             "<GetHotelImageLink>true</GetHotelImageLink>" +
                             "<GetHotelMapLink>true</GetHotelMapLink>" +
                             "<Source>0</Source>" +
                             "<LimitRoomTypesInResult>0</LimitRoomTypesInResult>" +
                           "</CheckAvailability>" +
                         "</soap:Body>" +
                       "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static XDocument CreateSoapEnvelopehtdetail(XElement req)
        {
            string ss = "<soap:Envelope xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'>" +
                         "<soap:Body>" +
                           "<GetBasicData_Properties_WithFullDetails xmlns='http://travelcontrol.softexsw.us/'>" +
                             "<SecStr>#C|559341#W#274298</SecStr>" +
                             "<AccountName>DTC</AccountName>" +
                             "<UserName>XML2016Ra</UserName>" +
                             "<Password>DarinAH</Password>" +
                             "<AgentID>1701</AgentID>" +
                             "<CountryID>" + req.Descendants("CountryID").Single().Value + "</CountryID>" +
                             "<CountryCode></CountryCode>" +
                             "<CountryName></CountryName>" +
                             "<CityID>" + req.Descendants("CityID").Single().Value + "</CityID>" +// pass 50 for test
                             "<CityCode></CityCode>" +
                             "<CityName></CityName>" +
                             "<AreaID>0</AreaID>" +
                             "<AreaName></AreaName>" +
                             "<LandCatregory>6</LandCatregory>" +
                             "<HotelName></HotelName>" +
                           "</GetBasicData_Properties_WithFullDetails>" +
                         "</soap:Body>" +
                        "</soap:Envelope>";
            XDocument soapEnvelop = XDocument.Parse(ss);
            return soapEnvelop;
        }
        private static void InsertSoapEnvelopeIntoWebRequest(XDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }
        #endregion
        #region Hotel Extranet
        #region Extranet Hotel Listing
        private IEnumerable<XElement> GetHotelListExtranet(List<XElement> htlist)
        {
            #region Extranet Hotels
            List<XElement> hotellst = new List<XElement>();
            try
            {
                Int32 length = 0;
                if (htlist != null)
                {
                    length = htlist.Count();
                }
                else
                {
                    length = 0;
                }
                try
                {
                    Parallel.For(0, length, i =>
                    {


                        hotellst.Add(new XElement("Hotel",
                                               new XElement("HotelID", Convert.ToString(htlist[i].Descendants("HotelID").SingleOrDefault().Value)),
                                               new XElement("HotelName", Convert.ToString(htlist[i].Descendants("HotelName").SingleOrDefault().Value)),
                                               new XElement("PropertyTypeName", Convert.ToString(htlist[i].Descendants("PropertyTypeName").SingleOrDefault().Value)),
                                               new XElement("CountryID", Convert.ToString(htlist[i].Descendants("CountryID").SingleOrDefault().Value)),
                                               new XElement("CountryName", Convert.ToString(htlist[i].Descendants("CountryName").SingleOrDefault().Value)),
                                               new XElement("CountryCode", Convert.ToString(htlist[i].Descendants("CountryCode").SingleOrDefault().Value)),
                                               new XElement("CityId", Convert.ToString(htlist[i].Descendants("CityId").SingleOrDefault().Value)),
                                               new XElement("CityCode", Convert.ToString(htlist[i].Descendants("CityCode").SingleOrDefault().Value)),
                                               new XElement("CityName", Convert.ToString(htlist[i].Descendants("CityName").SingleOrDefault().Value)),
                                               new XElement("AreaId", Convert.ToString(htlist[i].Descendants("AreaId").SingleOrDefault().Value)),
                                               new XElement("AreaName", Convert.ToString(htlist[i].Descendants("AreaName").SingleOrDefault().Value))
                                               , new XElement("Address", Convert.ToString(htlist[i].Descendants("Address").SingleOrDefault().Value)),
                                               new XElement("Location", Convert.ToString(htlist[i].Descendants("Location").SingleOrDefault().Value)),
                                               new XElement("Description", Convert.ToString(htlist[i].Descendants("Description").SingleOrDefault().Value)),
                                               new XElement("StarRating", Convert.ToString(htlist[i].Descendants("StarRating").SingleOrDefault().Value)),
                                               new XElement("MinRate", Convert.ToString(htlist[i].Descendants("MinRate").SingleOrDefault().Value))
                                               , new XElement("HotelImgSmall", Convert.ToString(htlist[i].Descendants("HotelImgSmall").SingleOrDefault().Value)),
                                               new XElement("HotelImgLarge", Convert.ToString(htlist[i].Descendants("HotelImgLarge").SingleOrDefault().Value)),
                                               new XElement("MapLink", ""),
                                               new XElement("Longitude", Convert.ToString(htlist[i].Descendants("Langtitude").SingleOrDefault().Value)),
                                               new XElement("Latitude", Convert.ToString(htlist[i].Descendants("Latitude").SingleOrDefault().Value)),
                                               new XElement("DMC", "Extranet"),
                                               new XElement("SupplierID", "3"),
                                               new XElement("Currency", Convert.ToString(htlist[i].Descendants("Currency").SingleOrDefault().Value)),
                                               new XElement("Offers", "")
                                               , new XElement("Facilities",
                                                  GetHotelFacilitiesExtranet(htlist[i].Descendants("Facility").ToList()))
                                               , new XElement("Rooms",
                                                    GetHotelRoomgroupExtranet(htlist[i].Descendants("Room").ToList())
                                                   )
                        ));

                    });
                }
                catch (Exception ex)
                {
                    return hotellst;
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
        #region Extranet Hotel's Room Grouping
        public IEnumerable<XElement> GetHotelRoomgroupExtranet(List<XElement> roomlist)
        {
            List<XElement> str = new List<XElement>();
            List<XElement> strgrp = new List<XElement>();
            //Parallel.For(0, roomlist.Count(), i =>
            int rindex = 1;
            for (int i = 0; i < roomlist.Count(); i++)
            {
                str = GetHotelRoomListingExtranet(roomlist[i]).ToList();

                for (int j = 0; j < str.Count();j++ )
                {

                    str[j].Add(new XAttribute("Index", rindex));
                    strgrp.Add(str[j]);
                    rindex++;
                }    
            };
            return strgrp;
        }
        #endregion
        #region Extranet Hotel's Room Listing
        public IEnumerable<XElement> GetHotelRoomListingExtranet(XElement roomlist)
        {
            
            List<XElement> strgrp = new List<XElement>();
             int group = 0;

             List<XElement> totalroom = reqTravillio.Descendants("RoomPax").ToList();
            
                
                 #region room's group

                 #region Board Bases >0

                 List<XElement> bb = roomlist.Descendants("Meal").ToList();

                 //Parallel.For(0, bb.Count(), j =>
                 for (int j = 0; j < bb.Count(); j++)
                 {
                     group++;
                     List<XElement> str = new List<XElement>();
                     for (int m = 0; m < totalroom.Count(); m++)
                     {
                         
                         decimal totalrate = calculatetotalroomrate(bb[j].Descendants("Price").ToList(),Convert.ToString(m+1));

                         str.Add(new XElement("Room",
                          new XAttribute("ID", Convert.ToString(roomlist.Descendants("RoomTypeId").SingleOrDefault().Value)),
                          new XAttribute("SuppliersID", "3"),
                          new XAttribute("RoomSeq", m + 1),
                          new XAttribute("SessionID", Convert.ToString("")),
                          new XAttribute("RoomType", Convert.ToString(roomlist.Descendants("RoomTypeName").SingleOrDefault().Value)),
                          new XAttribute("OccupancyID", Convert.ToString("")),
                          new XAttribute("OccupancyName", Convert.ToString("")),
                          new XAttribute("MealPlanID", Convert.ToString(bb[j].Descendants("MealPlanID").SingleOrDefault().Value)),
                          new XAttribute("MealPlanName", Convert.ToString(bb[j].Descendants("MealPlanName").SingleOrDefault().Value)),
                          new XAttribute("MealPlanCode", ""),
                          new XAttribute("MealPlanPrice", Convert.ToString("0")),
                          new XAttribute("PerNightRoomRate", Convert.ToString(bb[j].Descendants("PricePerNight").SingleOrDefault().Value)),
                          new XAttribute("TotalRoomRate", Convert.ToString(totalrate)),
                          new XAttribute("CancellationDate", ""),
                          new XAttribute("CancellationAmount", ""),
                           new XAttribute("isAvailable", Convert.ToString(roomlist.Descendants("AvailableAllRoom").SingleOrDefault().Value)),
                          new XElement("RequestID", Convert.ToString("")),
                          new XElement("Offers", ""),
                          new XElement("PromotionList",
                              GetHotelpromotionsExtranet(bb[j].Descendants("pro").ToList())),
                          new XElement("CancellationPolicy", ""),
                          new XElement("Amenities", new XElement("Amenity", "")),
                          new XElement("Images", new XElement("Image", new XAttribute("Path", ""))),
                          new XElement("Supplements", ""
                             //GetRoomsSupplementTourico(roomList1[m].SelctedSupplements.ToList())
                              ),
                              new XElement("PriceBreakups", 
                             GetRoomsPriceBreakupExtranet(bb[j].Descendants("Price").ToList(),Convert.ToString(m+1))
                                  ),
                              new XElement("AdultNum", Convert.ToString(totalroom[m].Element("Adult").Value)),
                              new XElement("ChildNum", Convert.ToString(totalroom[m].Element("Child").Value))
                          ));
                        
                     }

                     strgrp.Add(new XElement("RoomTypes", new XAttribute("TotalRate", Convert.ToString(bb[j].Descendants("TotalRoomRate").SingleOrDefault().Value)),
                     str));
                     
                 }
                 #endregion


                 //return str;
                 #endregion






                 return strgrp;
        }
        #endregion
        #region Calculate total room price
        private decimal calculatetotalroomrate(List<XElement> pernightrate, string roomseq)
        {
            decimal totalroomrate=0;

            for (int i = 0; i < pernightrate.Count();i++)
            {
                List<XElement> pernightprice = pernightrate[i].Descendants("SearchRoomPrice").Where(x => x.Descendants("RoomNo").Single().Value == roomseq).ToList();

                if (pernightprice.Count > 0)
                {
                    totalroomrate = totalroomrate + Convert.ToDecimal(pernightprice.Descendants("PriceForDate").SingleOrDefault().Value);
                }
                else
                {
                    totalroomrate = totalroomrate + 0;
                }
            }

            return totalroomrate;
        }
        #endregion
        #region Extranet Room's Price Breakups
        private IEnumerable<XElement> GetRoomsPriceBreakupExtranet(List<XElement> pricebreakups,string roomseq)
        {
            #region Extranet Room's Price Breakups
            List<XElement> str = new List<XElement>();
            Parallel.For(0, pricebreakups.Count(), i =>
            {
                List<XElement> pernightprice = pricebreakups[i].Descendants("SearchRoomPrice").Where(x => x.Descendants("RoomNo").Single().Value == roomseq).ToList();

                if (pernightprice.Count > 0)
                {
                    
                    str.Add(new XElement("Price",
                       new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                       new XAttribute("PriceValue", Convert.ToString(pernightprice.Descendants("PriceForDate").SingleOrDefault().Value)))
                );
                }
                else
                {
                    str.Add(new XElement("Price",
                       new XAttribute("Night", Convert.ToString(Convert.ToInt32(i + 1))),
                       new XAttribute("PriceValue", Convert.ToString(0)))
                );
                }

                
            });
            return str;
            #endregion
        }
        #endregion
        #region Extranet's Room's Promotion
        private IEnumerable<XElement> GetHotelpromotionsExtranet(List<XElement> roompromotions)
        {

            Int32 length = roompromotions.Count();
            List<XElement> promotion = new List<XElement>();

            if (length == 0)
            {
                promotion.Add(new XElement("Promotions", ""));
            }
            else
            {

                Parallel.For(0, length, i =>
                {

                    promotion.Add(new XElement("Promotions", Convert.ToString(roompromotions[i].Value)));

                });
            }
            return promotion;
        }
        #endregion
        #region Extranet's Hotel Facilities
        private IEnumerable<XElement> GetHotelFacilitiesExtranet(List<XElement> hotelfacilities)
        {

            Int32 length = hotelfacilities.Count();
            List<XElement> Facilities = new List<XElement>();

            //Facilities.Add(new XElement("Facility", "No Facility Available"));

            if (length == 0)
            {
                Facilities.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {

                Parallel.For(0, length, i =>
                {

                    Facilities.Add(new XElement("Facility", Convert.ToString(hotelfacilities[i].Value)));

                });
            }
            return Facilities;
        }
        #endregion
        #endregion
    }
}