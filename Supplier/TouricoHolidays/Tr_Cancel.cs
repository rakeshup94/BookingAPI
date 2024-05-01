using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.TouricoHolidays
{
    public class Tr_Cancel
    {
        #region Cancellation of Booking Tourico (XML OUT for Travayoo)
        public XElement CancelBooking_Tourico(XElement req)
        {
            #region Credentials            
            string userlogin = string.Empty;
            string pwd = string.Empty;
            string version = string.Empty;
            //userlogin = credential.Descendants("username").FirstOrDefault().Value;
            //pwd = credential.Descendants("password").FirstOrDefault().Value;
            //version = credential.Descendants("version").FirstOrDefault().Value;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "2");
            userlogin = suppliercred.Descendants("username").FirstOrDefault().Value;
            pwd = suppliercred.Descendants("password").FirstOrDefault().Value;
            version = suppliercred.Descendants("version").FirstOrDefault().Value;
            #endregion

            #region Tourico
            Int32 cnfrmnum = Convert.ToInt32(req.Descendants("ServiceID").FirstOrDefault().Value);
            DateTime cxldate = DateTime.ParseExact(DateTime.Now.ToString("dd/MM/yyyy"), "dd/MM/yyyy", null);
            TouricoReservation.LoginHeader hd = new TouricoReservation.LoginHeader();
            hd.username = userlogin;// "HOL916";
            hd.password = pwd;// "111111";
            hd.version = version;// "5";            
            TouricoReservation.ReservationsServiceSoapClient client = new TouricoReservation.ReservationsServiceSoapClient();
            TouricoReservation.CancellationFeeInfo cxlfee = client.GetCancellationFee(hd, cnfrmnum, cxldate);

            decimal cxlamt = cxlfee.CancellationFeeValue;
            WriteToFile_Log("Cancel Cost Tourico");
            WriteToFile_Log(Convert.ToString(cxlamt));

            bool reqtourico = client.CancelReservation(hd, req.Descendants("ServiceID").FirstOrDefault().Value);

            #region Log Save

            try
            {
                string response = "<xml>" + reqtourico + "</xml>";
                APILogDetail log = new APILogDetail();
                log.customerID = Convert.ToInt64(req.Descendants("CustomerID").Single().Value);
                log.TrackNumber = req.Descendants("TransID").Single().Value;
                log.LogTypeID = 6;
                log.LogType = "Cancel";
                log.SupplierID = 2;
                log.logrequestXML = req.ToString();
                log.logresponseXML = response.ToString();
                SaveAPILog savelog = new SaveAPILog();
                savelog.SaveAPILogs(log);
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CancelBooking_Tourico";
                ex1.PageName = "Tr_Cancel";
                ex1.CustomerID = req.Descendants("CustomerID").Single().Value;
                ex1.TranID = req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
            }

            #endregion

            string status = string.Empty;
            if (reqtourico == true)
            {
                status = "Success";
            }
            else
            {
                status = "Failed";
            }
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            IEnumerable<XElement> request = req.Descendants("HotelCancellationRequest");
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            XElement cancellationdoc = new XElement(
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
                           new XElement("HotelCancellationResponse",
                               new XElement("Rooms",
                                   new XElement("Room",
                                       new XElement("Cancellation",
                                           new XElement("Amount", Convert.ToString(cxlamt)),
                                           new XElement("Status", Convert.ToString(status))
                                           )
                                       )
                                   )
                  ))));
            return cancellationdoc;
            #endregion
        }
        #endregion
        #region Logs
        public void WriteToFile_Log(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Booking Cancellation Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
    }

}