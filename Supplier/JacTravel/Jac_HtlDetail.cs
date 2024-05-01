using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Jac_HtlDetail
    {


        public XElement HtldtlRqst(string Usrname, string Password, XElement Req)
        {
            try
            {
                XElement ele = new XElement("PropertyDetailsRequest",
                                     new XElement("LoginDetails",
                                         new XElement("Login", Usrname),
                                         new XElement("Password", Password),
                                          new XElement("Locale", string.Empty),
                                           new XElement("AgentReference", string.Empty)),
                                          new XElement("PropertyID", Req.Descendants("hoteldescRequest").FirstOrDefault().Element("HotelID").Value));

                if (ele != null)
                {
                    string Responce = ele.ToString();
                    if (!string.IsNullOrEmpty(Responce))
                    {
                        RequestClass requobj = new RequestClass();
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(Req.Descendants("CustomerID").FirstOrDefault().Value, "8");
                        string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                        string detailReq = ele.ToString();
                        Responce = requobj.HttpPostRequest(url, Req, detailReq, "HtlDetail",8,10);                       
                        return BindResponce(Responce);
                    }
                }
            }
            catch
            {


            }


            return null;
        }

        XElement BindResponce(string Responce)
        {
            XElement xele = XElement.Parse(Responce);
            XElement Respon = null;
            string status = (String)xele.Element("ReturnStatus").Element("Success");
            if (status == "true")
            {
                Respon = new XElement("hoteldescResponse",
              new XElement("Hotels",
                  new XElement("Hotel",
                      new XElement("HotelID", xele.Element("PropertyID").Value),
                      new XElement("Description", xele.Element("Description") != null ? xele.Element("Description").Value : string.Empty),
                      new XElement("Images", BindImg(xele.Descendants("Image"), xele.Descendants("CMSBaseURL").FirstOrDefault())),
                                                         new XElement("ContactDetails",
                                                             new XElement("Phone", xele.Element("Telephone") != null ? xele.Element("Telephone").Value : string.Empty),
                                                             new XElement("Fax", xele.Element("Fax") != null ? xele.Element("Fax").Value : string.Empty)))));

            }
            return Respon;
        }


        List<XElement> BindFacilities(IEnumerable<XElement> facilitycoll)
        {
            List<XElement> lst = new List<XElement>();

            foreach (XElement item in facilitycoll)
            {

                if (item.Parent.Name.ToString() != "PropertyRoomType")
                {
                    IEnumerable<XElement> facility = item.Descendants("Facility");
                    string str = string.Empty;
                    foreach (var item1 in facility)
                    {
                        if (!str.Contains(item1.Value) && item1.Element("FacilityType") == null)
                        {
                            str = str + "," + item1.Value;
                            lst.Add(new XElement("Facility", item1.Value));
                        }

                    }

                }

            }
            return lst;
        }

        List<XElement> BindImg(IEnumerable<XElement> Imgcoll, XElement Cmpath)
        {
            string path = string.Empty;
            if (Cmpath != null)
            {
                path = Cmpath.Value;
            }
            List<XElement> lst = new List<XElement>();
            foreach (XElement item in Imgcoll)
            {
                if (item.Element("Image") == null)
                {
                    lst.Add(new XElement("Image",
                   new XAttribute("Path", path + item.Value),
                   new XAttribute("Caption", "")));
                }

            }
            if (lst.Count < 1)
            {
                lst.Add(new XElement("Image",
                 new XAttribute("Path", string.Empty),
                 new XAttribute("Caption", "")));
            }
            return lst;
        }



        public XElement getHotelDetail(XElement Req, XElement Htl_static)
        {
            IEnumerable<XElement> Respon = from htl in Req.Descendants("hoteldescRequest")
                                           join htldesc in Htl_static.Descendants("Property")
                                                                on htl.Element("RequestID").Value equals htldesc.Element("PropertyReferenceID").Value
                                           select new XElement("hoteldescResponse",
                                        new XElement("Hotels",
                                 new XElement("Hotel",
                                     new XElement("HotelID", htl.Element("HotelID").Value),

                                     new XElement("Description", htldesc.Element("Description") != null ? htldesc.Element("Description").Value : string.Empty),
                                     new XElement("Images", BindHTlImg(htldesc)),
                                                                        new XElement("ContactDetails",
                                                                            new XElement("Phone", htldesc.Element("Telephone") != null ? htldesc.Element("Telephone").Value : string.Empty),
                                                                            new XElement("Fax", htldesc.Element("Fax") != null ? htldesc.Element("Fax").Value : string.Empty)))));

            return Respon.FirstOrDefault();

        }

        List<XElement> BindHTlImg(XElement htldesc)
        {
            List<XElement> lst = new List<XElement>();
            for (int i = 1; i < 11; i++)
            {
                string imgurl = "Image" + i.ToString() + "URL";
                if (htldesc.Element(imgurl)!=null)
                {
                    if (!string.IsNullOrEmpty(htldesc.Element(imgurl).Value))
                    {
                        lst.Add(new XElement("Image",
              new XAttribute("Path", htldesc.Element(imgurl).Value != null ? htldesc.Element(imgurl).Value : string.Empty),
               new XAttribute("Caption", "")));
                    }
                   
                }
               
                
            }
            if (lst.Count<1)
            {
                 lst.Add(new XElement("Image",
                 new XAttribute("Path", string.Empty),
                  new XAttribute("Caption",string.Empty)));
            }
           

            return lst;
        }

    }
}
