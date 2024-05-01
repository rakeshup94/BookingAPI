using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.RTS;

namespace TravillioXMLOutService.Supplier.RTS
{
    public class RTSIntial
    {
        int sup_cutime = 100000;
        string dmc = string.Empty;
        string customerid = string.Empty;
        public List<XElement> CallSearch(XElement reqTravillio, string path,string custID, string xtype)
        {
            try
            {
                #region get cut off time
                try
                {
                    sup_cutime = supplier_Cred.secondcutoff_time();
                }
                catch { }
                #endregion
                dmc = xtype;
                customerid = custID;
                foreach (XElement item in reqTravillio.Descendants("searchRequest"))
                {                     
                    RTS_getcity rtsgetcitydet = new RTS_getcity();
                    RTS_citydetail rtscity = new RTS_citydetail();
                    rtscity.CityID = reqTravillio.Descendants("CityID").FirstOrDefault().Value;
                    DataTable dtcity = rtsgetcitydet.GetCity_RTS(rtscity);
                    string CtyCode = string.Empty;                    
                    if (dtcity != null)
                    {
                        if (dtcity.Rows.Count != 0)
                        {
                            CtyCode = dtcity.Rows[0]["SupCityId"].ToString();
                            if (CtyCode != "" || CtyCode !=null)
                            {
                                //string CtyCode = CityCode.Descendants("Supplier").Where(x => x.Descendants("SupplierID").FirstOrDefault().Value == "9").FirstOrDefault().Descendants("SupplierCityID").FirstOrDefault().Value;
                                XDocument responce = null;
                                XElement ele = null;
                                string GuestCountyCode = item.Element("PaxNationality_CountryCode").Value != string.Empty ? item.Element("PaxNationality_CountryCode").Value.ToUpper() : string.Empty;
                                RTS_HtlSearch obj = new RTS_HtlSearch();
                                HTlStaticData staticdata = new HTlStaticData();
                                Thread th = new Thread(new ThreadStart(() => responce = obj.SearchHotel(reqTravillio, CtyCode, GuestCountyCode, custID, dmc)));
                                Thread th1 = new Thread(new ThreadStart(() => ele = staticdata.GetData(CtyCode)));
                                th.Start();
                                th1.Start();
                                th.Join(sup_cutime);
                                th1.Join(sup_cutime);
                                th.Abort();
                                th1.Abort();
                                if (responce != null && ele != null)
                                {
                                    return obj.HTLResponce(responce, reqTravillio, ele,custID, dmc).ToList();
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                try
                                {
                                    APILogDetail log = new APILogDetail();
                                    log.customerID = Convert.ToInt64(customerid);
                                    log.LogTypeID = 1;
                                    log.LogType = "Search";
                                    log.SupplierID = 9;
                                    log.StartTime = DateTime.Now;
                                    log.EndTime = DateTime.Now;
                                    log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                                    log.logrequestXML = reqTravillio.ToString();
                                    log.logresponseXML = "<xml>No city date found in db</xml>";
                                    SaveAPILog savelog = new SaveAPILog();
                                    savelog.SaveAPILogs(log);
                                    return null;
                                }
                                catch { return null; }
                            }
                        }
                        else
                        {
                            try
                            {
                                APILogDetail log = new APILogDetail();
                                log.customerID = Convert.ToInt64(customerid);
                                log.LogTypeID = 1;
                                log.LogType = "Search";
                                log.SupplierID = 9;
                                log.StartTime = DateTime.Now;
                                log.EndTime = DateTime.Now;
                                log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                                log.logrequestXML = reqTravillio.ToString();
                                log.logresponseXML = "<xml>No city mapping found</xml>";
                                SaveAPILog savelog = new SaveAPILog();
                                savelog.SaveAPILogs(log);
                                return null;
                            }
                            catch { return null; }
                        }
                    }
                    else
                    {
                        try
                        {
                            APILogDetail log = new APILogDetail();
                            log.customerID = Convert.ToInt64(customerid);
                            log.LogTypeID = 1;
                            log.LogType = "Search";
                            log.SupplierID = 9;
                            log.StartTime = DateTime.Now;
                            log.EndTime = DateTime.Now;
                            log.TrackNumber = reqTravillio.Descendants("TransID").Single().Value;
                            log.logrequestXML = reqTravillio.ToString();
                            log.logresponseXML = "<xml>No city mapping found in db</xml>";
                            SaveAPILog savelog = new SaveAPILog();
                            savelog.SaveAPILogs(log);
                            return null;
                        }
                        catch { return null; }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CallSearch";
                ex1.PageName = "RTSIntial";
                ex1.CustomerID = customerid;
                ex1.TranID = reqTravillio.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
        }
    }
}