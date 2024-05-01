using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.GIATA
{
    public class GiataMapping_Hotel : IDisposable
    {       
        //private static decimal CalculateMinPrice(DataTable HACrncy, decimal SupplierRate, string HtlCrncyCode)
        //{
        //    string BuyingRate = HACrncy.Select("crncyCode = '" + HtlCrncyCode + "'")[0][1].ToString();
        //    decimal MainAgentBuyingrate = Convert.ToDecimal(BuyingRate) * SupplierRate;
        //    return MainAgentBuyingrate;
        //}
        private static decimal CalculateMinPrice(DataTable HACrncy, decimal SupplierRate, string HtlCrncyCode)
        {
            decimal MainAgentBuyingrate = 1;

            if (HACrncy.Select("crncyCode = '" + HtlCrncyCode + "'").Count() > 0)
            {
                string BuyingRate = HACrncy.Select("crncyCode = '" + HtlCrncyCode + "'")[0][1].ToString();
                MainAgentBuyingrate = Convert.ToDecimal(BuyingRate) * SupplierRate;

            }


            return MainAgentBuyingrate;
        }
        public static XElement MapGiataData(XElement xmlData)
        {
            WriteToLogFile("Process Start giata for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
            string constr = System.Configuration.ConfigurationManager.ConnectionStrings["INGMContext"].ConnectionString.ToString();
            SqlConnection conn = null;
            SqlDataReader rdr = null;
            XElement giataresponse = null;
            SqlDataAdapter adp = null;
            DataSet ds = new DataSet();
            string GIATA = xmlData.Descendants("searchRequest").Elements("Giata").FirstOrDefault().Value;
            string CityCode = xmlData.Descendants("searchRequest").Elements("CityID").FirstOrDefault().Value;
            // new code added for currency conversion on 20/06/2018
            string DeployementType = xmlData.Descendants("searchRequest").Elements("DeployementType").FirstOrDefault().Value;
            string customerid = xmlData.Descendants("searchRequest").Elements("CustomerID").FirstOrDefault().Value;


            string CountryCode = xmlData.Descendants("searchRequest").Elements("CountryCode").FirstOrDefault().Value;

            if (GIATA.ToUpper() == "FALSE")// To check whether customer Demands for Giata or Not
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("GiataID");
                dt.Columns.Add("HotelID");
                dt.Columns.Add("Suppliers");
                ds.Tables.Add(dt);
                ds.Tables[0].TableName = "Table";
            }
            else
            {
                //DataTable inputData = new DataTable("Hotels");                                
                //inputData.Columns.Add("HotelID");
                //inputData.Columns.Add("SupplierID");
                //var hotelData = xmlData.Descendants("Hotel").ToArray();
                //WriteToLogFile("DataTable start for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                //for(int i=0;i<xmlData.Descendants("Hotel").Count();i++)
                //{
                //    DataRow dr = inputData.NewRow();
                //    dr[0] = hotelData[i].Descendants("HotelID").FirstOrDefault().Value;
                //    dr[1] = hotelData[i].Descendants("SupplierID").FirstOrDefault().Value;
                //    inputData.Rows.Add(dr);
                //}
                //WriteToLogFile("DataTable end for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));



                #region Code commented on 14-12-18 to get combined results

                //  conn = new
              //SqlConnection(constr);
              //  conn.Open();
              //  SqlCommand cmd = new SqlCommand(
              //      "GetGiataData_New", conn);
              //  cmd.CommandType = CommandType.StoredProcedure;
              //  //cmd.Parameters.AddWithValue("@tblCustomers", inputData);
              //  cmd.Parameters.AddWithValue("@CityID", CityCode);
              //  WriteToLogFile("DataBase Record Fetch start from  giata for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
              //  adp = new SqlDataAdapter(cmd);
              //  adp.Fill(ds);
                //  WriteToLogFile("DataBase Record Fetch end from  giata for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));

                #endregion


                var xmlElements = new XElement("Hotels",
            xmlData.Descendants("Hotels").Descendants("Hotel").Select(i => new XElement("Hotel",
            new XElement("HotelID", i.Element("HotelID").Value)
                , new XElement("SupplierID", i.Element("SupplierID").Value)
                 //, new XElement("RequestID", i.Element("RequestID").Value)

            ))

         ).ToString();


                WriteToLogFile("DataBase Record Fetch start from  giata for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));

                conn = new
              SqlConnection(constr);
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "GetGiataData", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@XML", xmlElements);
                cmd.Parameters.AddWithValue("@CountryCode", CountryCode);
                adp = new SqlDataAdapter(cmd);
                adp.Fill(ds);


                WriteToLogFile("DataBase Record Fetch end from  giata for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));

            }
            try
            {
             
                List<string> hotelid_SuplID = xmlData.Descendants("Hotel").Select(x=>x.Element("HotelID").Value+"_"+x.Element("SupplierID").Value).ToList();
                // create and open a connection object            

                DataSet Currency = new DataSet();
                conn = new
             SqlConnection(constr);
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    "GetCurrencyRates", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userID", customerid);
                cmd.Parameters.AddWithValue("@status", DeployementType);
                adp = new SqlDataAdapter(cmd);
                adp.Fill(Currency);


                object xds = ds.GetXml();
                var availResponse = XDocument.Parse(xds.ToString());
                List<XElement> availResponse_new = availResponse.Descendants("Table").Where(x => hotelid_SuplID.Contains(x.Element("HotelID").Value + "_" + x.Element("Suppliers").Value)).ToList();
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                List<XElement> st = availResponse.Descendants("NewDataSet").Descendants("Table").Elements("HotelID").ToList();
                List<XElement> st1 = xmlData.Descendants("searchResponse").Descendants("Hotels").Descendants("Hotel").Elements("HotelID").ToList();
                XElement avail_new = new  XElement("NewDataSet", availResponse_new);
                #region Mani 18-10-2018
                //object objgiataDt =
                //    new XElement("searchResponse",
                //    new XElement("Hotels",
                //     from cust in xmlData.Descendants("searchResponse").Descendants("Hotels").Elements("Hotel")
                //     join ord in availResponse.Descendants("NewDataSet").Elements("Table")
                //     //on new { HotelID = Convert.ToString(cust.Element("HotelID").Value) } equals new { HotelID = Convert.ToString(ord.Element("HotelID").Value) }
                //     on new { HotelID = Convert.ToString(cust.Element("HotelID").Value), suplid = Convert.ToString(cust.Element("SupplierID").Value) } equals new { HotelID = Convert.ToString(ord.Element("HotelID").Value), suplid = Convert.ToString(ord.Element("Suppliers").Value) }
                //     into pst
                //     from ord in pst.DefaultIfEmpty()
                //     select new XElement("Hotel",
                //                new XElement("HotelName", cust.Element("HotelName").Value),
                //         //new XElement("HotelID", ord == null ? cust.Element("HotelID").Value : string.Join(",", availResponse.Descendants("NewDataSet").Elements("Table").Where(x => x.Element("GiataID").Value == ord.Element("GiataID").Value).OrderByDescending(k => k.Element("GiataID").Value).Select(y => y.Element("HotelID").Value))),
                //         new XElement("HotelID", cust.Element("HotelID").Value),
                //           new XElement("PropertyTypeName", cust.Element("PropertyTypeName") == null ? "" : Convert.ToString(cust.Element("PropertyTypeName").Value)),
                //           new XElement("CountryID", cust.Element("CountryID") == null ? "" : Convert.ToString(cust.Element("CountryID").Value)),
                //             new XElement("CountryName", cust.Element("CountryName") == null ? "" : Convert.ToString(cust.Element("CountryName").Value)),
                //              new XElement("CityId", cust.Element("CityId") == null ? "" : Convert.ToString(cust.Element("CityId").Value)),
                //                       new XElement("CityCode", cust.Element("CityCode") == null ? "" : Convert.ToString(cust.Element("CityCode").Value)),
                //                       new XElement("CityName", cust.Element("CityName") == null ? "" : Convert.ToString(cust.Element("CityName").Value)),
                //                  new XElement("AreaId", cust.Element("AreaId") == null ? "" : Convert.ToString(cust.Element("AreaId").Value)),
                //                   new XElement("AreaName", cust.Element("AreaName") == null ? "" : Convert.ToString(cust.Element("AreaName").Value)),
                //                     new XElement("RequestID", cust.Element("RequestID") == null ? "" : Convert.ToString(cust.Element("RequestID").Value)),
                //                       new XElement("Address", cust.Element("Address") == null ? "" : Convert.ToString(cust.Element("Address").Value)),
                //                            new XElement("Location", cust.Element("Location") == null ? "" : Convert.ToString(cust.Element("Location").Value)),
                //                                 new XElement("Description", cust.Element("Description") == null ? "" : Convert.ToString(cust.Element("Description").Value)),
                //                                   new XElement("StarRating", cust.Element("StarRating") == null ? "" : Convert.ToString(cust.Element("StarRating").Value)),
                //                                     new XElement("MinRate", cust.Element("MinRate") == null ? "" : Convert.ToString(cust.Element("MinRate").Value)),
                //                                     new XElement("ConvertedMinRate", CalculateMinPrice(Currency.Tables[0], Convert.ToDecimal(cust.Element("MinRate").Value), Convert.ToString(cust.Element("Currency").Value))),
                //                                       new XElement("HotelImgSmall", cust.Element("HotelImgSmall") == null ? "" : Convert.ToString(cust.Element("HotelImgSmall").Value)),
                //                                         new XElement("HotelImgLarge", cust.Element("HotelImgLarge") == null ? "" : Convert.ToString(cust.Element("HotelImgLarge").Value)),
                //                                          new XElement("MapLink", cust.Element("MapLink") == null ? "" : Convert.ToString(cust.Element("MapLink").Value)),
                //                                           //new XElement("Longitude", cust.Element("Longitude") == null ?(cust.Element("SupplierID").Value=="1"?ord.Element("Longitude").Value: "") : Convert.ToString(cust.Element("Longitude").Value)),
                //                                            //new XElement("Latitude", cust.Element("Latitude") == null ? (cust.Element("SupplierID").Value == "1" ? ord.Element("Latitude").Value : "") : Convert.ToString(cust.Element("Latitude").Value)),
                //                                             new XElement("Longitude", cust.Element("Longitude") == null ? "" : (cust.Element("SupplierID").Value == "1" ? ord == null ? "0" : ord.Element("Longitude").Value : Convert.ToString(cust.Element("Longitude").Value))),
                //                                            new XElement("Latitude", cust.Element("Latitude") == null ? "" : (cust.Element("SupplierID").Value == "1" ? ord == null ? "0" : ord.Element("Latitude").Value : Convert.ToString(cust.Element("Latitude").Value))),
                //                                            new XElement("DMC", cust.Element("DMC") == null ? "" : Convert.ToString(cust.Element("DMC").Value)),
                //                                            new XElement("Currency", cust.Element("Currency") == null ? "" : Convert.ToString(cust.Element("Currency").Value)),
                //                                            new XElement("Offers", cust.Element("Offers") == null ? "" : Convert.ToString(cust.Element("Offers").Value)),
                //                                            new XElement("Facilities",cust.Element("Facilities").Descendants("Facility")),
                //                                           new XElement("SupplierID", cust.Element("SupplierID") == null ? "" : cust.Element("SupplierID").Value),
                //                                           new XElement("GiataID", ord == null ? "0" : ord.Element("GiataID").Value),
                //                                             new XElement(("GiataList"),
                //                                            //from r in (ord != null ? (availResponse.Descendants("NewDataSet").Elements("Table").Where(x => x.Element("GiataID").Value == ord.Element("GiataID").Value).OrderByDescending(k => k.Element("GiataID").Value)).ToList() : xmlData.Descendants("searchResponse").Descendants("Hotels").Elements("Hotel").Where(x => x.Element("HotelID").Value == cust.Element("HotelID").Value))
                //                                            from r in (ord != null ? (availResponse.Descendants("NewDataSet").Elements("Table").Where(x => x.Element("GiataID").Value == ord.Element("GiataID").Value).OrderByDescending(k => k.Element("GiataID").Value)).ToList() : xmlData.Descendants("searchResponse").Descendants("Hotels").Elements("Hotel").Where(x => x.Element("HotelID").Value == cust.Element("HotelID").Value && x.Element("SupplierID").Value == cust.Element("SupplierID").Value))
                //                                            select new XElement(("GiataHotelList"),
                //                                                new XAttribute("GHtlID", ord == null ? cust.Element("HotelID").Value : r.Element("HotelID").Value),
                //                                                new XAttribute("GSupID", ord == null ? cust.Element("SupplierID").Value : r.Element("Suppliers").Value),
                //                                                new XAttribute("xmlout", cust.Element("DMC") == null ? "false" : Convert.ToString(cust.Element("DMC").Value)=="HA"?"true":"false"),
                //                                                //new XAttribute("GRequestID", xmlData.Descendants("searchResponse").Descendants("Hotels").Descendants("Hotel").Where(x => x.Element("HotelID").Value == (ord == null ? cust.Element("HotelID").Value : r.Element("HotelID").Value)).Descendants("RequestID").FirstOrDefault().Value)
                //                                                 new XAttribute("GRequestID", ord == null ? cust.Element("RequestID").Value:r.Element("RequestID").Value))
                                                                
                //                                                ),
                //                                                  new XElement("Rooms", cust.Element("Rooms").Descendants("RoomTypes")
                //                )))
                //         );
                //var consGiataDt = XDocument.Parse(objgiataDt.ToString());
                //giataresponse =
                //new XElement(soapenv + "Envelope",

                //          new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                //          new XElement(soapenv + "Header",
                //           new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                //           new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                //           new XElement(soapenv + "Body",
                //           new XElement(xmlData.Descendants("searchRequest").FirstOrDefault()),
                //           new XElement("searchResponse",
                //          new XElement("Hotels",
                //              from r in consGiataDt.Descendants("searchResponse").Descendants("Hotels").Descendants("Hotel").Where(x => x.Element("GiataID").Value == "0")
                //              select new XElement(r),
                //              from k in consGiataDt.Descendants("searchResponse").Descendants("Hotels").Descendants("Hotel").Where(x => x.Element("GiataID").Value != "0").OrderBy(x => Convert.ToDecimal(x.Element("ConvertedMinRate").Value))
                //              group k by (string)k.Element("GiataID") into p
                //              select new XElement(p.First())
                //             ))));
                ////giataresponse =
                ////new XElement(soapenv + "Envelope",
                ////          new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                ////          new XElement(soapenv + "Header",
                ////           new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                ////           new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                ////           new XElement(soapenv + "Body",
                ////           new XElement(xmlData.Descendants("searchRequest").FirstOrDefault()),
                ////           new XElement("searchResponse",
                ////          new XElement("Hotels",
                ////              from r in consGiataDt.Descendants("searchResponse").Descendants("Hotels").Descendants("Hotel")
                ////              select new XElement(r)))));

                #endregion
                WriteToLogFile("Response start from  giata for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                giataresponse = GiataResponse(xmlData, avail_new, Currency.Tables[0]);
                //giataresponse = GiataResponsemulti(xmlData, avail_new, Currency.Tables[0]);
                WriteToLogFile("Response End from  giata for " + xmlData.Descendants("TransID").FirstOrDefault().Value + " at: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "MapGiataData";
                ex1.PageName = "GiataMapping_Hotel";
                ex1.CustomerID = xmlData.Descendants("CustomerID").Single().Value;
                ex1.TranID = xmlData.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return giataresponse;
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
                if (rdr != null)
                {
                    rdr.Close();
                }
            }
            return giataresponse;
        }
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
        public static List<XElement> CalculateHotelResult(List<XElement> hotelsWithGiata, List<XElement> toAdd, int Minval, int Maxval)
        {
            List<XElement> lstnew = new List<XElement>();
            foreach (XElement ele in hotelsWithGiata.Skip(Minval).Take(Maxval))
            {
                if (toAdd.Descendants("HotelID").Select(y => y.Value).ToList().Contains(ele.Element("HotelID").Value) == true)
                {
                    lstnew.Add(ele);
                }
            }
            return lstnew;
        }
        public static XElement GiataResponse(XElement xmlData, XElement availResponse, DataTable HACrncy)
        {
            XElement Response = null;
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            try
            {
                List<XElement> list1 = new List<XElement>();
                List<XElement> list2 = new List<XElement>();
                List<XElement> tr1 = new List<XElement>();
                List<XElement> tr2 = new List<XElement>();
                List<XElement> tr3 = new List<XElement>();
                List<XElement> tr4 = new List<XElement>();
                List<XElement> tr5 = new List<XElement>();
                List<XElement> tr6 = new List<XElement>();
                List<XElement> tr7 = new List<XElement>();
                List<string> giataIds = availResponse.Descendants("GiataID").Select(x => x.Value).Distinct().ToList();
                int number = Convert.ToInt32(Math.Round( giataIds.Count() / 5 == 0? giataIds.Count:Convert.ToDecimal(giataIds.Count/5),0,MidpointRounding.AwayFromZero));
                
                //var giataIdChunk = BreakIntoChunks(giataIds, number,5);
                var giataIdChunk = Partition(giataIds,5);
                List<List<XElement>> giataLists = new List<List<XElement>>();
                List<List<XElement>> hotelsLists = new List<List<XElement>>();
                var giataGroup = availResponse.Descendants("Table").GroupBy(x => x.Element("GiataID").Value);
               

              // new  List<string> hotelsAvail = availResponse.Descendants("Hotel").Select(x => x.Element("HotelID").Value + "_" + x.Element("SupplierID").Value).ToList();
                List<string> hotelsAvail = availResponse.Descendants("HotelID").Select(x => x.Value).Distinct().ToList();
                 List<XElement> hotelsWithGiata = xmlData.Descendants("Hotel").Where(x => hotelsAvail.Contains(x.Element("HotelID").Value)).ToList();    
               //new List<XElement> hotelsWithGiata = xmlData.Descendants("Hotel").Where(x=>hotelsAvail.Contains(x.Element("HotelID").Value+"_"+x.Element("SupplierID").Value)).ToList();    
                foreach (var list in giataIdChunk)
                {
                    List<XElement> lstnew = new List<XElement>();
                    List<XElement> batlstnew = new List<XElement>();
                    List<XElement> toAdd = availResponse.Descendants("Table").Where(x => list.Contains(x.Element("GiataID").Value)).ToList();
                    giataLists.Add(toAdd);
                    #region Slow Result 
                    //hotelsLists.Add(hotelsWithGiata.Where(x => toAdd.Descendants("HotelID").Select(y => y.Value).ToList().Contains(x.Element("HotelID").Value)).ToList());
                    #endregion

                    #region
                    int ThreadCount = 5;
                    int BalBatch = 0;
                    int BatchCluster = (hotelsWithGiata.Count / ThreadCount);

                    if (BatchCluster <= 0)
                    {
                        List<XElement> lst1 = new List<XElement>();
                        BatchCluster = hotelsWithGiata.Count;
                        var threads = new List<Thread>
                        {
                          new Thread(() => lstnew=CalculateHotelResult(hotelsWithGiata,toAdd, 0,BatchCluster)),
                        };
                        threads.ForEach(t => t.Start());
                        threads.ForEach(t => t.Join());
                        threads.ForEach(t => t.Abort());
                        hotelsLists.Add(lstnew);
                    }
                    else
                    {
                        List<XElement> lst1 = new List<XElement>();
                        List<XElement> lst2 = new List<XElement>();
                        List<XElement> lst3 = new List<XElement>();
                        List<XElement> lst4 = new List<XElement>();
                        List<XElement> lst5 = new List<XElement>();
                        List<XElement> lst6 = new List<XElement>();
                        BalBatch = (hotelsWithGiata.Count - BatchCluster * ThreadCount);
                        var threadscluster = new List<Thread>
                        {
                          new Thread(() =>lst1= CalculateHotelResult(hotelsWithGiata, toAdd, 0, BatchCluster)),
                          new Thread(() => lst2=CalculateHotelResult(hotelsWithGiata, toAdd, BatchCluster * 1, BatchCluster)),
                          new Thread(() => lst3=CalculateHotelResult(hotelsWithGiata, toAdd, BatchCluster * 2, BatchCluster)),
                          new Thread(() => lst4=CalculateHotelResult(hotelsWithGiata, toAdd, BatchCluster * 3, BatchCluster)),
                          new Thread(() => lst5=CalculateHotelResult(hotelsWithGiata,toAdd, BatchCluster * 4, BatchCluster)),
                        new Thread(() =>  lst6=CalculateHotelResult(hotelsWithGiata,toAdd, BatchCluster * 5, BalBatch))                        

                        };
                        threadscluster.ForEach(t => t.Start());
                        threadscluster.ForEach(t => t.Join());
                        threadscluster.ForEach(t => t.Abort());
                        lstnew.AddRange(lst1.Concat(lst2).Concat(lst3).Concat(lst4).Concat(lst5).Concat(lst6));
                        hotelsLists.Add(lstnew);
                    }
                    #endregion






                }
                List<List<XElement>> giataHotels = new List<List<XElement>>();
                List<XElement> hotelsWithoutGiata = xmlData.Descendants("Hotel").Where(x => !hotelsAvail.Contains(x.Element("HotelID").Value)).ToList();
                if (number > 0)
                {
                    List<XElement>[] giataListArray = new List<XElement>[6];
                    List<XElement>[] hotelsListArray = new List<XElement>[6];
                    int count = number < 5 ? 1 : 5;
                    for(int i=0;i<giataLists.Count;i++)
                    {
                        giataListArray[i] = giataLists.ElementAt(i).Any() ? giataLists.ElementAt(i) : null;
                        hotelsListArray[i] = hotelsLists.ElementAt(i).Any() ? hotelsLists.ElementAt(i) : null;
                    }
                        //= giataLists.ElementAt(0).Any()? giataLists.ElementAt(0)
                    List<Thread> threadedlist = new List<Thread>
                       {
                           new Thread(()=> tr1 = giataList(giataListArray[0], hotelsListArray[0],HACrncy)),
                           new Thread(()=> tr2 = giataList(giataListArray[1], hotelsListArray[1],HACrncy)),
                           new Thread(()=> tr3 = giataList(giataListArray[2], hotelsListArray[2],HACrncy)),
                           new Thread(()=> tr4 = giataList(giataListArray[3], hotelsListArray[3],HACrncy)),
                           new Thread(()=> tr5 = giataList(giataListArray[4], hotelsListArray[4],HACrncy)),
                           new Thread(()=> tr6 = nonGiata(hotelsWithoutGiata,HACrncy))
                       };
                    threadedlist.ForEach(t => t.Start());
                    threadedlist.ForEach(t => t.Join());
                    threadedlist.ForEach(t => t.Abort());
                    list1.AddRange(tr1.Concat(tr2).Concat(tr3).Concat(tr4).Concat(tr5).Concat(tr6).Concat(tr7));
                    list1.OrderBy(x => x.Element("ConvertedMinRate").Value);
                }
                else
                {
                    list1 = nonGiata(hotelsWithoutGiata, HACrncy);
                    list1.OrderBy(x => x.Element("ConvertedMinRate").Value);
                }

                Response = new XElement(soapenv + "Envelope",

                              new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                              new XElement(soapenv + "Header",
                               new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                               new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                               new XElement(soapenv + "Body",
                               new XElement(xmlData.Descendants("searchRequest").FirstOrDefault()),
                               new XElement("searchResponse",
                              new XElement("Hotels",list1 ))));
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "GiataResponse";
                ex1.PageName = "GiataMapping_Hotel";
                ex1.CustomerID = xmlData.Descendants("CustomerID").Single().Value;
                ex1.TranID = xmlData.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                //List<XElement> ListExc = nonGiata(xmlData.Descendants("Hotel").ToList(),HACrncy);
                //Response = new XElement(soapenv + "Envelope",

                //              new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                //              new XElement(soapenv + "Header",
                //               new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                //               new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                //               new XElement(soapenv + "Body",
                //               new XElement(xmlData.Descendants("searchRequest").FirstOrDefault()),
                //               new XElement("searchResponse",
                //              new XElement("Hotels", ListExc))));
                //return Response;
            }
            return Response;
        }
        public static XElement GiataResponsemulti(XElement xmlData, XElement availResponse, DataTable HACrncy)
        {
            XElement Response = null;
            XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
            try
            {
                List<XElement> list1 = new List<XElement>();
                List<XElement> list2 = new List<XElement>();
                List<XElement> tr1 = new List<XElement>();
                List<XElement> tr2 = new List<XElement>();
                List<XElement> tr3 = new List<XElement>();
                List<XElement> tr4 = new List<XElement>();
                List<XElement> tr5 = new List<XElement>();
                List<XElement> tr6 = new List<XElement>();
                List<XElement> tr7 = new List<XElement>();

                List<XElement> tr8 = new List<XElement>();
                List<XElement> tr9 = new List<XElement>();
                List<XElement> tr10 = new List<XElement>();
                List<XElement> tr11 = new List<XElement>();
                List<XElement> tr12 = new List<XElement>();


                List<XElement> tr13 = new List<XElement>();
                List<XElement> tr14 = new List<XElement>();
                List<XElement> tr15 = new List<XElement>();
                List<XElement> tr16 = new List<XElement>();
                List<XElement> tr17 = new List<XElement>();



                List<string> giataIds = availResponse.Descendants("GiataID").Select(x => x.Value).Distinct().ToList();
                int number = Convert.ToInt32(Math.Round(giataIds.Count() / 15 == 0 ? giataIds.Count : Convert.ToDecimal(giataIds.Count / 15), 0, MidpointRounding.AwayFromZero));

                var giataIdChunk = Partition(giataIds, 15);
                List<List<XElement>> giataLists = new List<List<XElement>>();
                List<List<XElement>> hotelsLists = new List<List<XElement>>();
                var giataGroup = availResponse.Descendants("Table").GroupBy(x => x.Element("GiataID").Value);

                List<string> hotelsAvail = availResponse.Descendants("HotelID").Select(x => x.Value).Distinct().ToList();
                List<XElement> hotelsWithGiata = xmlData.Descendants("Hotel").Where(x => hotelsAvail.Contains(x.Element("HotelID").Value)).ToList();   
                foreach (var list in giataIdChunk)
                {
                    List<XElement> toAdd = availResponse.Descendants("Table").Where(x => list.Contains(x.Element("GiataID").Value)).ToList();
                    giataLists.Add(toAdd);




                    hotelsLists.Add(hotelsWithGiata.Where(x => toAdd.Descendants("HotelID").Select(y => y.Value).ToList().Contains(x.Element("HotelID").Value)).ToList());






                }
                List<List<XElement>> giataHotels = new List<List<XElement>>();
                List<XElement> hotelsWithoutGiata = xmlData.Descendants("Hotel").Where(x => !hotelsAvail.Contains(x.Element("HotelID").Value)).ToList();
                if (number > 0)
                {
                    List<XElement>[] giataListArray = new List<XElement>[16];
                    List<XElement>[] hotelsListArray = new List<XElement>[16];
                    int count = number < 15 ? 1 : 15;
                    for (int i = 0; i < giataLists.Count; i++)
                    {
                        giataListArray[i] = giataLists.ElementAt(i).Any() ? giataLists.ElementAt(i) : null;
                        hotelsListArray[i] = hotelsLists.ElementAt(i).Any() ? hotelsLists.ElementAt(i) : null;
                    }
                    List<Thread> threadedlist = new List<Thread>
                       {
                           new Thread(()=> tr1 = giataList(giataListArray[0], hotelsListArray[0],HACrncy)),
                           new Thread(()=> tr2 = giataList(giataListArray[1], hotelsListArray[1],HACrncy)),
                           new Thread(()=> tr3 = giataList(giataListArray[2], hotelsListArray[2],HACrncy)),
                           new Thread(()=> tr4 = giataList(giataListArray[3], hotelsListArray[3],HACrncy)),
                           new Thread(()=> tr5 = giataList(giataListArray[4], hotelsListArray[4],HACrncy)),

                           new Thread(()=> tr6 = giataList(giataListArray[5], hotelsListArray[5],HACrncy)),
                           new Thread(()=> tr7 = giataList(giataListArray[6], hotelsListArray[6],HACrncy)),
                           new Thread(()=> tr8 = giataList(giataListArray[7], hotelsListArray[7],HACrncy)),

                           new Thread(()=> tr9 = giataList(giataListArray[8], hotelsListArray[8],HACrncy)),
                           new Thread(()=> tr10 = giataList(giataListArray[9], hotelsListArray[9],HACrncy)),

                           new Thread(()=> tr11 = giataList(giataListArray[10], hotelsListArray[10],HACrncy)),
                           new Thread(()=> tr12 = giataList(giataListArray[11], hotelsListArray[11],HACrncy)),
                           new Thread(()=> tr13 = giataList(giataListArray[12], hotelsListArray[12],HACrncy)),
                           new Thread(()=> tr14 = giataList(giataListArray[13], hotelsListArray[13],HACrncy)),
                           new Thread(()=> tr15 = giataList(giataListArray[14], hotelsListArray[14],HACrncy)),

                           new Thread(()=> tr16 = nonGiata(hotelsWithoutGiata,HACrncy))
                       };
                    threadedlist.ForEach(t => t.Start());
                    threadedlist.ForEach(t => t.Join());
                    threadedlist.ForEach(t => t.Abort());
                    list1.AddRange(tr1.Concat(tr2).Concat(tr3).Concat(tr4).Concat(tr5).Concat(tr6).Concat(tr7).Concat(tr8).Concat(tr9).Concat(tr10).Concat(tr11).Concat(tr12).Concat(tr13).Concat(tr14).Concat(tr15).Concat(tr16).Concat(tr17));
                    list1.OrderBy(x => x.Element("ConvertedMinRate").Value);
                }
                else
                {
                    list1 = nonGiata(hotelsWithoutGiata, HACrncy);
                    list1.OrderBy(x => x.Element("ConvertedMinRate").Value);
                }

                Response = new XElement(soapenv + "Envelope",

                              new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                              new XElement(soapenv + "Header",
                               new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                               new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                               new XElement(soapenv + "Body",
                               new XElement(xmlData.Descendants("searchRequest").FirstOrDefault()),
                               new XElement("searchResponse",
                              new XElement("Hotels", list1))));
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "GiataResponse";
                ex1.PageName = "GiataMapping_Hotel";
                ex1.CustomerID = xmlData.Descendants("CustomerID").Single().Value;
                ex1.TranID = xmlData.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
               
            }
            return Response;
        }
        public static List<List<T>> BreakIntoChunks<T>(List<T> list, int chunkSize, int chunkCount)
        {
            List<List<T>> retVal = new List<List<T>>();
            if (chunkSize <= 0)
            {
                //throw new ArgumentException("chunkSize must be greater than 0.");
                return retVal;
            }
            int checkNumber = chunkSize > chunkCount ? chunkCount : chunkSize;
            while (list.Count > 0)
            {
                int count = list.Count() >= chunkSize + checkNumber ? chunkSize : list.Count;
                retVal.Add(list.GetRange(0, count));
                list.RemoveRange(0, count);
            }

            return retVal;
        }

        #region Partition
        public static List<List<T>> Partition<T>(List<T> list, int totalPartitions)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (totalPartitions < 1)
                throw new ArgumentOutOfRangeException("totalPartitions");

            List<T>[] partitions = new List<T>[totalPartitions];

            int maxSize = (int)Math.Ceiling(list.Count / (double)totalPartitions);
            int k = 0;

            for (int i = 0; i < partitions.Length; i++)
            {
                partitions[i] = new List<T>();
                for (int j = k; j < k + maxSize; j++)
                {
                    if (j >= list.Count)
                        break;
                    partitions[i].Add(list[j]);
                }
                k += maxSize;
            }

            return partitions.ToList();
        }

        public static List<List<T>> BreakIntoSlots<T>(List<T> list, int slotSize)
        {
            if (slotSize <= 0)
            {
                throw new ArgumentException("Slot Size must be greater than 0.");
            }
            List<List<T>> retVal = new List<List<T>>();
            while (list.Count > 0)
            {
                int count = list.Count > slotSize ? slotSize : list.Count;
                retVal.Add(list.GetRange(0, count));
                list.RemoveRange(0, count);
            }

            return retVal;
        }

        #endregion
        public static List<XElement> giataList(List<XElement> giataData, List<XElement> hotelsWithGiata, DataTable HACrncy)
        {
            List<XElement> list1 = new List<XElement>();
            //List<string> hotelsInThread = hotelsWithGiata.Descendants("HotelID").Select(x => x.Value).ToList();
            //var giataGroup = giataData.Descendants("Table").Where(x => hotelsInThread.Contains(x.Element("HotelID").Value)).GroupBy(x => x.Element("GiataID").Value);
            try
            {
                if (giataData != null && hotelsWithGiata != null)
                {
                    var giataGroup = giataData.GroupBy(x => x.Element("GiataID").Value);
                    foreach (var giataHotel in giataGroup)
                    {

                        #region added on 31-01-2019
                        List<string> hotelid_SuplID = giataHotel.Select(x => x.Element("HotelID").Value + "_" + x.Element("Suppliers").Value).ToList();
                        List<XElement> currentHotels = hotelsWithGiata.Where(x => hotelid_SuplID.Contains(x.Element("HotelID").Value + "_" + x.Element("SupplierID").Value)).ToList();
                        #endregion

                        #region changed on 31-01-2019 (to remove the duplicate allocation of same hotel id with giata London City Hilton angel and Le Meridien Piccadilly
                        //List<XElement> currentHotels = hotelsWithGiata.Where(x => giataHotel.Descendants("HotelID")
                        //   .Select(y => y.Value).Contains(x.Element("HotelID").Value)).ToList();
                        #endregion

                        if (currentHotels.Count > 0)
                        {
                            foreach (XElement hotel in currentHotels)
                            {

                                decimal SupplierRate = Convert.ToDecimal(hotel.Element("MinRate").Value);
                                string HtlCrncyCode = hotel.Element("Currency").Value;
                                decimal MainAgentBuyingrate = 1;

                                if (HACrncy.Select("crncyCode = '" + HtlCrncyCode + "'").Count() > 0)
                                {
                                    string BuyingRate = HACrncy.Select("crncyCode = '" + HtlCrncyCode + "'")[0][1].ToString();
                                    MainAgentBuyingrate = Convert.ToDecimal(BuyingRate) * SupplierRate;

                                }
                                hotel.Add(new XElement("ConvertedMinRate", MainAgentBuyingrate));
                                //Calculate Converted Price
                            }
                            string minPrice = currentHotels.Descendants("ConvertedMinRate").Select(x => Convert.ToDecimal(x.Value)).Min().ToString();
                            XElement minPriceHotel = currentHotels.Where(x => x.Element("ConvertedMinRate").Value.Equals(minPrice)).FirstOrDefault();
                            //string minPrice = currentHotels.Descendants("MinRate").Select(x => Convert.ToDecimal(x.Value)).Min().ToString();
                            //XElement minPriceHotel = currentHotels.Where(x => x.Element("MinRate").Value.Equals(minPrice)).FirstOrDefault();
                            minPriceHotel.Add(new XElement("GiataID", giataHotel.Key));
                            string giataHotelName = giataHotel.FirstOrDefault().Element("hotelname").Value;
                            try
                            {                                
                                byte[] TemporaryBytes = System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(giataHotelName);
                                giataHotelName = System.Text.Encoding.ASCII.GetString(TemporaryBytes);
                            }
                            catch { }
                            if (!string.IsNullOrEmpty(giataHotelName))
                                minPriceHotel.Descendants("HotelName").FirstOrDefault().SetValue(giataHotelName);

                            XElement giataList = new XElement("GiataList");
                            foreach (XElement hotel in currentHotels)
                            {
                                //string xmlout = hotel.Element("DMC").Value.Equals("HA") ? "true" : "false";
                                giataList.Add(
                                    new XElement("GiataHotelList",
                                        new XAttribute("GHtlID", hotel.Element("HotelID").Value),
                                        new XAttribute("GSupID", hotel.Element("SupplierID").Value),
                                        new XAttribute("xmlout", hotel.Element("xmlouttype").Value),
                                        new XAttribute("custID", hotel.Element("xmloutcustid").Value),
                                        new XAttribute("custName", hotel.Element("DMC").Value),
                                        new XAttribute("GRequestID", hotel.Element("RequestID").Value),
                                        new XAttribute("sessionKey", hotel.Element("sessionKey") == null ? "" : hotel.Element("sessionKey").Value),
                                        new XAttribute("sourcekey", hotel.Element("sourcekey") == null ? "" : hotel.Element("sourcekey").Value),
                                        new XAttribute("publishedkey", hotel.Element("publishedkey") == null ? "" : hotel.Element("publishedkey").Value)
                                        ));
                            }
                            minPriceHotel.Descendants("GiataID").FirstOrDefault().AddAfterSelf(giataList);
                            list1.Add(minPriceHotel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "GiataResponse";
                ex1.PageName = "GiataMapping_Hotel";
                ex1.CustomerID = "10000";
                ex1.TranID = "dacbaa8e-3581-41fc-ba38-fd048516ddc7-test";
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                list1 = nonGiata(hotelsWithGiata, HACrncy);
                return list1;
            }           
                return list1;
        }
        public static List<XElement> nonGiata(List<XElement> hotelsWithoutGiata, DataTable HACrncy)
        {
            List<XElement> list2 = new List<XElement>();
            foreach (XElement hotel in hotelsWithoutGiata)
            {
                decimal SupplierRate = Convert.ToDecimal(hotel.Element("MinRate").Value);
                string HtlCrncyCode = hotel.Element("Currency").Value;
                decimal MainAgentBuyingrate = 1;

                if (HACrncy.Select("crncyCode = '" + HtlCrncyCode + "'").Count() > 0)
                {
                    string BuyingRate = HACrncy.Select("crncyCode = '" + HtlCrncyCode + "'")[0][1].ToString();
                    MainAgentBuyingrate = Convert.ToDecimal(BuyingRate) * SupplierRate;

                }
                //string xmlout = hotel.Element("DMC").Value.Equals("HA") ? "true" : "false";
                hotel.Add(new XElement("ConvertedMinRate", MainAgentBuyingrate));
                hotel.Add(new XElement("GiataID", "0"));
                hotel.Add(new XElement("GiataList",
                       new XElement("GiataHotelList",
                           new XAttribute("GHtlID", hotel.Element("HotelID").Value),
                           new XAttribute("GSupID", hotel.Element("SupplierID").Value),
                           new XAttribute("xmlout", hotel.Element("xmlouttype").Value),
                           new XAttribute("custID", hotel.Element("xmloutcustid").Value),
                           new XAttribute("custName", hotel.Element("DMC").Value),
                           new XAttribute("GRequestID", hotel.Element("RequestID").Value),
                           new XAttribute("sessionKey", hotel.Element("sessionKey") == null ? "" : hotel.Element("sessionKey").Value),
                           new XAttribute("sourcekey", hotel.Element("sourcekey") == null ? "" : hotel.Element("sourcekey").Value),
                           new XAttribute("publishedkey", hotel.Element("publishedkey") == null ? "" : hotel.Element("publishedkey").Value)
                           )));
                list2.Add(hotel);
            }
            return list2;
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
