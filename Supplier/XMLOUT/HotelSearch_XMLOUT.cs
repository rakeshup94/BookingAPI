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
using TravillioXMLOutService.Supplier.DotW;
using TravillioXMLOutService.Common.DotW;
using TravillioXMLOutService.Supplier.JacTravel;
using TravillioXMLOutService.Supplier.Extranet;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Supplier.TouricoHolidays;
using TravillioXMLOutService.Supplier.GIATA;
using TravillioXMLOutService.Models.Supplier_Cred;
using TravillioXMLOutService.Models.Darina;
using TravillioXMLOutService.Models.HotelsPro;
using TravillioXMLOutService.Models.Common;
using TravillioXMLOutService.Models.Tourico;
using TravillioXMLOutService.Models.Travco;
using TravillioXMLOutService.Models.RTS;
using TravillioXMLOutService.Models.Juniper;
using TravillioXMLOutService.Models.Restel;
using TravillioXMLOutService.Models.JacTravel;
using TravillioXMLOutService.Models.Godou;
using TravillioXMLOutService.Models.SalTours;
using TravillioXMLOutService.Models.EBookingCenter;

namespace TravillioXMLOutService.Supplier.XMLOUT
{
    public class HotelSearch_XMLOUT : IDisposable
    {
        DotwService dotwObj;
        string touricouserlogin = string.Empty;
        string touricopassword = string.Empty;
        string touricoversion = string.Empty;
        string availDarina = string.Empty;
        XElement reqTravillio;
        //string jacpath = string.Empty;
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
        public void WriteToFileResponseTimeHotel(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {


                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    //writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    //writer.WriteLine("---------------------------Time Calculate-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region Hotel Availability (XML OUT for Travayoo)
        public XElement HotelAvail_XMLOUT(XElement req)
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
                    DateTime Reqstattime = DateTime.Now;
                    reqTravillio = req;
                    int darina = req.Descendants("SupplierID").Where(x => x.Value == "1").Count();
                    int tourico = req.Descendants("SupplierID").Where(x => x.Value == "2").Count();
                    int extranet = req.Descendants("SupplierID").Where(x => x.Value == "3").Count();
                    int hotelbeds = req.Descendants("SupplierID").Where(x => x.Value == "4").Count();
                    int DOTW = req.Descendants("SupplierID").Where(x => x.Value == "5").Count();
                    int hotelspro = req.Descendants("SupplierID").Where(x => x.Value == "6").Count();
                    int travco = req.Descendants("SupplierID").Where(x => x.Value == "7").Count();
                    int JacTravel = req.Descendants("SupplierID").Where(x => x.Value == "8").Count();
                    int RTS = req.Descendants("SupplierID").Where(x => x.Value == "9").Count();
                    int Miki = req.Descendants("SupplierID").Where(x => x.Value == "11").Count();
                    int restel = req.Descendants("SupplierID").Where(x => x.Value == "13").Count();
                    int JuniperW2M = req.Descendants("SupplierID").Where(x => x.Value == "16").Count();
                    int EgyptExpress = req.Descendants("SupplierID").Where(x => x.Value == "17").Count();
                    int SalTour = req.Descendants("SupplierID").Where(x => x.Value == "19").Count();
                    int tbo = req.Descendants("SupplierID").Where(x => x.Value == "21").Count();
                    int LOH = req.Descendants("SupplierID").Where(x => x.Value == "23").Count();
                    int Gadou = req.Descendants("SupplierID").Where(x => x.Value == "31").Count();
                    int LCI = req.Descendants("SupplierID").Where(x => x.Value == "35").Count();
                    int SunHotels = req.Descendants("SupplierID").Where(x => x.Value == "36").Count();
                    int totalstay = req.Descendants("SupplierID").Where(x => x.Value == "37").Count();
                    int Smyrooms = req.Descendants("SupplierID").Where(x => x.Value == "39").Count();
                    int AlphaTours = req.Descendants("SupplierID").Where(x => x.Value == "41").Count();
                    int Hoojoozat = req.Descendants("SupplierID").Where(x => x.Value == "45").Count();
                    int vot = req.Descendants("SupplierID").Where(x => x.Value == "46").Count();
                    int ebookingcenter = req.Descendants("SupplierID").Where(x => x.Value == "47").Count();
                    int bookingexpress = req.Descendants("SupplierID").Where(x => x.Value == "501").Count();
                    if (darina > 0 || tourico > 0 || extranet > 0 || hotelbeds > 0 || DOTW > 0 || hotelspro > 0 || travco > 0 || JacTravel > 0 || RTS > 0 || Miki > 0 || restel > 0 || JuniperW2M > 0 || EgyptExpress > 0 || SalTour > 0 || tbo > 0 || LOH > 0 || Gadou > 0 || LCI > 0 || SunHotels > 0 || totalstay > 0 || Smyrooms > 0 || AlphaTours > 0 || Hoojoozat > 0 || vot > 0 || ebookingcenter > 0 || bookingexpress > 0)
                    {                        
                        #region Supplier Credentials
                        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                        #endregion
                        #region Object Initialization
                        HotelSearch_HA htlsrch_ha = new HotelSearch_HA();
                        HotelSearch_NAHA htlsrch_naha = new HotelSearch_NAHA();
                        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(common_citymap).TypeHandle);
                        #endregion
                        XElement responseHa = null;
                        XElement responseNAHA = null;
                        #region Bind Static Data
                        if (darina > 0)
                        {
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(dr_staticdata).TypeHandle);
                        }
                        if (tourico > 0)
                        {
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(trc_statichtl).TypeHandle);
                        }
                        if (DOTW > 0)
                        {
                            dotwObj = new DotwService(req.Descendants("CustomerID").FirstOrDefault().Value);
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(CommonHelper).TypeHandle);
                        }
                        if (hotelspro > 0)
                        {
                            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(htlpro_facility).TypeHandle);
                        }
                        if (travco > 0)
                        {
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(travco_static).TypeHandle);
                        }
                        if(JacTravel > 0 || totalstay > 0)
                        {
                            //try
                            //{
                            //    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(jac_staticdata).TypeHandle);
                            //    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(jac_staticregion).TypeHandle);
                            //    XElement jaccitymapping = jac_staticdata.jac_citymapping();
                            //    int duration = 0;
                            //    int RegionID = 0;
                            //    foreach (XElement item in reqTravillio.Descendants("searchRequest"))
                            //    {
                            //        duration = Convert.ToInt32(JacHelper.GetDuration(item.Element("ToDate").Value, item.Element("FromDate").Value, out duration));
                            //        XElement ele = jaccitymapping.Descendants("d0").Where(x => x.Descendants("Serial").FirstOrDefault().Value == item.Descendants("CityID").FirstOrDefault().Value).FirstOrDefault();
                            //        RegionID = Convert.ToInt32(ele.Descendants("Supplier").Where(x => x.Descendants("SupplierID").FirstOrDefault().Value == "8").FirstOrDefault().Descendants("SupplierCityID").FirstOrDefault().Value);
                            //    }
                            //    jacpath = jac_staticregion.jac_regionmapping(RegionID);
                            //}
                            //catch { }

                        }
                        if (RTS > 0)
                        {
                            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(RTS_citymap).TypeHandle);
                        }
                        if (restel > 0)
                        {
                            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(restel_citymapping).TypeHandle);
                        }
                        if (SalTour > 0)
                        {
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Sal_Currency).TypeHandle);
                        }
                        //if (Gadou > 0)
                        //{
                        //    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Gadou_Currency).TypeHandle);
                        //}
                        if (ebookingcenter > 0)
                        {
                            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(EBookingStatic).TypeHandle);
                        }
                        #endregion
                        //List<Task> tasklst = new List<Task>();
                        //tasklst.Add(Task.Run(() => { responseHa = htlsrch_ha.HotelAvail_HA(req, "HA", jacpath); }));
                        //tasklst.Add(Task.Run(() => { responseNAHA = htlsrch_naha.HotelAvail_NAHA(req, "", jacpath).Result; }));
                        //Task.WaitAll(tasklst.ToArray());
                        #region Thread Initialize
                        Thread thid1 = null;
                        Thread thid2 = null;
                        thid1 = new Thread(new ThreadStart(() => { responseHa = htlsrch_ha.HotelAvail_HA(req, "HA"); }));
                        thid2 = new Thread(new ThreadStart(() => { responseNAHA = htlsrch_naha.HotelAvail_NAHA(req, ""); }));
                        #endregion
                        #region Thread Start
                        try
                        {
                            thid1.Start();
                            thid2.Start();
                        }
                        catch (ThreadStateException te)
                        {

                        }
                        #endregion
                        #region Thread Join
                        thid1.Join();
                        thid2.Join();
                        #endregion
                        #region Thread Abort
                        thid1.Abort();
                        thid2.Abort();
                        #endregion
                        #region Merge Hotel's List
                        IEnumerable<XElement> request = req.Descendants("searchRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        #region Get Hotel List
                        List<XElement> hahotels = null;
                        List<XElement> nahahotels = null;
                        WriteToLogFile("XML OUT start for " + req.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                        #region HA
                        if (responseHa != null)
                        {
                            if (responseHa.Descendants("Hotel").ToList().Count() > 0)
                            {
                                try
                                {
                                    hahotels = responseHa.Descendants("Hotel").ToList();
                                }
                                catch { }
                            }
                        }
                        #endregion
                        #region NAHA
                        if (responseNAHA != null)
                        {
                            if (responseNAHA.Descendants("Hotel").ToList().Count() > 0)
                            {
                                try
                                {
                                    nahahotels = responseNAHA.Descendants("Hotel").ToList();
                                }
                                catch { }
                            }
                        }
                        #endregion
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
                               new XElement("searchResponse",
                                   new XElement("Hotels",
                                       hahotels,
                                          nahahotels
                                       )
                      ))));
                        WriteToLogFile("XML OUT end for " + req.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));

                        #region Save into Logs
                        try
                        {
                            DateTime ReqEndtime = DateTime.Now;
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt64(reqTravillio.Descendants("CustomerID").Single().Value);
                            log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                            log.LogTypeID = 101;
                            log.LogType = "BSearch";
                            log.logrequestXML = req.ToString();
                            log.logresponseXML = searchdoc.ToString();
                            log.StartTime = Reqstattime;
                            log.EndTime = ReqEndtime;
                            SaveAPILog savelog = new SaveAPILog();
                            savelog.SaveAPILogs(log);
                        }
                        catch (Exception ex)
                        {
                            CustomException ex1 = new CustomException(ex);
                            ex1.MethodName = "HotelAvail_XMLOUT";
                            ex1.PageName = "HotelSearch_XMLOUT";
                            ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                            ex1.TranID = req.Descendants("TransID").Single().Value;
                            SaveAPILog saveex = new SaveAPILog();
                            saveex.SendCustomExcepToDB(ex1);
                        }
                        #endregion

                        searchdoc = GiataMapping_Hotel.MapGiataData(searchdoc);
                        return searchdoc;
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
                    CustomException ex1 = new CustomException(ex);
                    ex1.MethodName = "CreateCheckAvailability";
                    ex1.PageName = "HotelSearch_XMLOUT";
                    ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                    ex1.TranID = req.Descendants("TransID").Single().Value;
                    SaveAPILog saveex = new SaveAPILog();
                    saveex.SendCustomExcepToDB(ex1);
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
        public static void WriteToLogFile(string logtxt)
        {
            string _filePath = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
            //string path = Path.Combine(_filePath, @"log.txt");
            string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(logtxt);
                writer.Close();
            }

        }
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}