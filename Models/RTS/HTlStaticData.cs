using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Xml.Linq;
using System.Web;

namespace TravillioXMLOutService.Models
{
    public class HTlStaticData
    {

        public string connecttion()
        {
            return ConfigurationManager.ConnectionStrings["RTSHtl_Static"].ToString();
            //con = new SqlConnection(constr);
            //if (con.State != ConnectionState.Open)
            //{
            //    con.Open();
            //}
        }



        public string ServiceConn()
        {
            return ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            //con = new SqlConnection(constr);
            //if (con.State != ConnectionState.Open)
            //{
            //    con.Open();
            //}
        }
        public XElement GetData(string cityCode)
        {

            //string query = "select inv.ItemCode,Address,SmallFileName,BusinessCenter,FitnessCenter,Swimmingpool,Sauna,Spa,Restaurant,Tennis,Lotion.LocationName,";
            //query = query + "Golf,Disabledfacilities,laundry";
            //query = query + ",Babysitting,Porter,Parking,Roomservice,Carrenting,TourService,Exchange,Shop,BarLounge,Freenewspaper,Meetingroom,KidsClub,Luggagestorage ";
            //query = query + "from tblRTS_Inventory inv left join tblRTS_htlFacility faci on inv.ItemCode=faci.ItemCode";
            //query = query + " left join tblRTS_Img rtsimg on faci.ItemCode=rtsimg.ItemCode";
            //query = query + " left join tblRTS_LocationMapping Lotion on rtsimg.ItemCode=Lotion.ItemCode where inv.CityCode=@code"; 
            string connectionString = ServiceConn();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("usp_getrtsdata", connection);
                command.Parameters.AddWithValue("@code", cityCode);

                command.CommandType = CommandType.StoredProcedure;

                connection.Open();

                SqlDataReader reader = command.ExecuteReader();
                XElement ele1 = new XElement("htldatalst", string.Empty);

                try
                {
                    if (reader.HasRows)
                    {

                        while (reader.Read())
                        {
                            XElement chkele = ele1.Descendants("HtlData").Where(x => x.Element("ItemCode").Value == reader["ItemCode"].ToString()).FirstOrDefault();
                            if (chkele == null)
                            {
                                XElement ele = new XElement("HtlData",
                                      new XElement("ItemCode", reader["ItemCode"]),
                                      new XElement("Address", reader["Address"]),
                                      new XElement("Location",reader["LocationName"]!=null?reader["LocationName"]:string.Empty),
                                      new XElement("Img", reader["SmallFileName"].ToString() == string.Empty ? "" : "http://images.rts.co.kr/Images/" + reader["SmallFileName"].ToString())
                                    
                                                           );
                                ele1.Add(ele);
                            }

                        }

                        return ele1;
                    }


                }
                catch (Exception EX)
                {
                    return null;
                }
                finally
                {
                    // Always call Close when done reading.
                    // reader.Close();
                    command.Dispose();
                    connection.Close();
                }
            }
            return null;
        }



        public XElement GetHtlDetail(string ItemCode)
        {
            XElement HtlDetail = null;




            // string query = "select ItemCode,ZipCode,PhoneNo,FaxNo,DisCription,Img from tblRTS_Inventory where ItemCode=@code";
            //string query = "select inv.ItemCode,ZipCode,PhoneNo,FaxNo,Genenal,Howtogetthere,AttractionNearby";
            //query = query + " from tblRTS_Inventory inv left join RTS_HotelDescription faci on inv.ItemCode=faci.ItemCode  where inv.ItemCode=@code";

            string query = "select inv.ItemCode,Address,BusinessCenter,FitnessCenter,Swimmingpool,Sauna,Spa,Restaurant,Tennis,";
            query = query + "Golf,Disabledfacilities,laundry,ZipCode,PhoneNo,FaxNo,Genenal,Howtogetthere,AttractionNearby";
            query = query + ",Babysitting,Porter,Parking,Roomservice,Carrenting,TourService,Exchange,Shop,BarLounge,Freenewspaper,Meetingroom,KidsClub,Luggagestorage ";
            query = query + "from tblRTS_Inventory inv left join tblRTS_htlFacility faci on inv.ItemCode=faci.ItemCode";
            query = query + " left join RTS_HotelDescription Detail on faci.ItemCode=Detail.ItemCode where inv.ItemCode=@code";

            string connectionString = connecttion();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@code", ItemCode);
                connection.Open();
                //SqlDataReader reader = command.ExecuteReader();
                SqlDataAdapter adepter = new SqlDataAdapter();
                adepter.SelectCommand = command;
                DataSet ds = new DataSet();
                adepter.Fill(ds);

                try
                {
                    if (ds.Tables.Count > 0)
                    {
                        DataTable dt = ds.Tables[0];
                        foreach (DataRow row in dt.Rows)
                        {
                            string Genenal = string.Empty;
                            string Gethere = string.Empty;
                            string Attraction = string.Empty;
                            if (!string.IsNullOrEmpty(row["Genenal"].ToString()))
                            {
                                Genenal = "<li>" + row["Genenal"] + "</li>";
                            }

                            if (!string.IsNullOrEmpty(row["Howtogetthere"].ToString()))
                            {
                                Gethere = "<li>" + row["Howtogetthere"] + "</li>";
                            }

                            if (!string.IsNullOrEmpty(row["AttractionNearby"].ToString()))
                            {
                                Attraction = "<li>" + row["AttractionNearby"] + "</li>";
                            }
                            string totalcase = Genenal + Gethere + Attraction;
                            XElement Respon = new XElement("hoteldescResponse",
                    HtlDetail = new XElement("Hotels",
                   new XElement("Hotel",
                     new XElement("HotelID", row["ItemCode"]),
                     new XElement("Description", totalcase != null ? totalcase : string.Empty),
                   BindRTSImg(ItemCode, connection),
                   new XElement("Facilities",
                                       row["BusinessCenter"].ToString() == "Y" ? new XElement("Facility", "BusinessCenter") : null,
                                          row["FitnessCenter"].ToString() == "Y" ? new XElement("Facility", "FitnessCenter") : null,
                                             row["Swimmingpool"].ToString() == "Y" ? new XElement("Facility", "Swimmingpool") : null,
                                         row["Sauna"].ToString() == "Y" ? new XElement("Facility", "Sauna") : null,
                                         row["Spa"].ToString() == "Y" ? new XElement("Facility", "Spa") : null,
                                           row["Restaurant"].ToString() == "Y" ? new XElement("Facility", "Restaurant") : null,
                                             row["Tennis"].ToString() == "Y" ? new XElement("Facility", "Tennis") : null,
                                              row["Golf"].ToString() == "Y" ? new XElement("Facility", "Golf") : null,
                                              row["Disabledfacilities"].ToString() == "Y" ? new XElement("Facility", "Disabledfacilities") : null,
                                                 row["laundry"].ToString() == "Y" ? new XElement("Facility", "laundry") : null,
                                                  row["Babysitting"].ToString() == "Y" ? new XElement("Facility", "Babysitting") : null,
                                                row["Porter"].ToString() == "Y" ? new XElement("Facility", "Porter") : null,
                                                  row["Parking"].ToString() == "Y" ? new XElement("Facility", "Parking") : null,
                                                    row["Roomservice"].ToString() == "Y" ? new XElement("Facility", "Roomservice") : null,
                                                     row["Carrenting"].ToString() == "Y" ? new XElement("Facility", "Carrenting") : null,
                                                       row["TourService"].ToString() == "Y" ? new XElement("Facility", "TourService") : null,
                                                      row["Exchange"].ToString() == "Y" ? new XElement("Facility", "Exchange") : null,
                                                        row["Shop"].ToString() == "Y" ? new XElement("Facility", "Shop") : null,
                                                         row["BarLounge"].ToString() == "Y" ? new XElement("Facility", "BarLounge") : null,
                                                        row["Freenewspaper"].ToString() == "Y" ? new XElement("Facility", "Freenewspaper") : null,
                                                         row["Meetingroom"].ToString() == "Y" ? new XElement("Facility", "Meetingroom") : null,
                                                        row["KidsClub"].ToString() == "Y" ? new XElement("Facility", "KidsClub") : null,
                                                         row["Luggagestorage"].ToString() == "Y" ? new XElement("Facility", "Luggagestorage") : null
                                                           ),
                   new XElement("ContactDetails",
                       new XElement("Phone", row["PhoneNo"] != null ? row["PhoneNo"] : string.Empty),
                       new XElement("Fax", row["FaxNo"] != null ? row["FaxNo"] : string.Empty)))));
                        }
                    }




                }
                catch (Exception EX)
                {
                    return null;
                }
                finally
                {

                    command.Dispose();
                    connection.Close();
                }

            }

            return HtlDetail;
        }


        public XDocument GetRTSRoomAvailable(XElement req, int SupplierId)
        {
            XDocument responce = null;
            foreach (XElement item in req.Descendants("searchRequest"))
            {
                string TransID = item.Element("TransID").Value;

                string query = "select logrequestXML from tblapilog with(nolock) where TrackNumber=@code and SupplierID=@ID and logType=@Search";

                string connectionString = ServiceConn();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@code", TransID);
                    command.Parameters.AddWithValue("@Search", "Search");
                    command.Parameters.AddWithValue("@ID", SupplierId);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        while (reader.Read())
                        {
                            responce = XDocument.Parse(reader["logrequestXML"].ToString());
                        }

                        return responce;
                    }
                    catch (Exception EX)
                    {
                        return responce;
                    }
                    finally
                    {
                        // Always call Close when done reading.
                        reader.Close();
                    }
                }
            }
            return responce;

        }


        public IEnumerable<XElement> GetJacRoomAvailable(XElement req, int SupplierId,string hotelid)
        {
            int supid = SupplierId;
            foreach (XElement item in req.Descendants("searchRequest"))
            {
                string TransID = item.Element("TransID").Value;

                string query = "select logresponseXML from tblapilog with(nolock) where TrackNumber=@code and SupplierID=@ID and logType=@Search";

                string connectionString = ServiceConn();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@code", TransID);
                    command.Parameters.AddWithValue("@Search", "Search");
                    command.Parameters.AddWithValue("@ID", SupplierId);

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        while (reader.Read())
                        {
                          XElement  responce = XElement.Parse(reader["logresponseXML"].ToString());
                            XElement ele = responce.Descendants("Success").FirstOrDefault();
                            //if (SupplierId == supid && ele.Value == "true")
                            //{
                            //    IEnumerable<XElement> ele1 = responce.Descendants("PropertyResult").Where(x => x.Element("PropertyID").Value == hotelid);
                            //    return ele1;
                            //}
                            if (SupplierId == supid && ele.Value == "true")
                            {
                                XElement property = responce.Descendants("PropertyResult").Where(x => x.Element("PropertyReferenceID").Value == hotelid).FirstOrDefault();
                                if (property != null)
                                {
                                    IEnumerable<XElement> ele1 = responce.Descendants("PropertyResult").Where(x => x.Element("PropertyReferenceID").Value == hotelid);
                                    return ele1;
                                }
                            }

                        }

                       
                    }
                    catch (Exception EX)
                    {
                        return null;
                    }
                    finally
                    {
                        // Always call Close when done reading.
                        reader.Close();
                    }
                }
            }
            return null;

        }

        public List<List<XElement>> GetHtlCode(string cityCode)
        {
            try
            {


                string query = "select ItemCode from tblRTS_Inventory where CityCode=@code";


                string connectionString = connecttion();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@code", cityCode);
                    connection.Open();
                    List<XElement> lst1 = new List<XElement>();
                    SqlDataReader reader = command.ExecuteReader();
                    List<List<XElement>> lst = new List<List<XElement>>();
                    XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                    XNamespace rts = "http://www.rts.co.kr/";
                    try
                    {
                        while (reader.Read())
                        {

                            XElement ele = new XElement(soap + "Envelope",
                                                                                new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                                                 new XAttribute(XNamespace.Xmlns + "rts", rts),
                                                                                       new XElement(rts + "ItemCodeInfo",
                                                                                           new XElement(rts + "ItemCode", reader["ItemCode"]),
                                               new XElement(rts + "ItemNo", 1)));



                            if (lst1.Count == 15)
                            {
                                lst.Add(lst1);
                                lst1 = null;
                                lst1 = new List<XElement>();
                                lst1.Add(ele);
                            }
                            else
                            {
                                lst1.Add(ele);
                            }
                        }
                        return lst;

                    }
                    catch (Exception EX)
                    {
                        return null;
                    }
                    finally
                    {
                        // Always call Close when done reading.
                        reader.Close();
                    }
                }
            }
            finally
            {
                //com.Dispose();
                //con.Close();
            }
        }

        IEnumerable<XElement> jacRomavail(XDocument res, XElement req)
        {
            foreach (XElement item in req.Descendants("searchRequest"))
            {
                IEnumerable<XElement> ele = res.Descendants("PropertyResult").Where(x => x.Element("PropertyID").Value == item.Element("HotelID").Value);
                return ele;
            }
            return null;
        }




        XElement BindRTSImg(string HtlCode, SqlConnection connection)
        {
            string query = "select SmallFileName from tblRTS_Img where ItemCode=@code";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@code", HtlCode);
            //connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            XElement imgs = new XElement("Images", "");
            while (reader.Read())
            {
                XElement img = new XElement("Image",
                      new XAttribute("Path", "http://images.rts.co.kr/Images/" + reader["SmallFileName"]));
                imgs.Add(img);
            }
            if (imgs.Descendants("Image") == null)
            {
                XElement img = new XElement("Image",
                  new XAttribute("Path", string.Empty));
                imgs.Add(img);
            }
            reader.Close();
            return imgs;

        }




    }
}
