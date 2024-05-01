using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Getresort
    {


        public delegate void HtlstDelegate(List<XElement> lst);
        public event HtlstDelegate MyEvent;
        public void GetResortData(XElement HtlStatic, XElement Req, XElement mealtype, int duration, XElement Facility, string Usrname, string Password, int RegionID,string dmc,int supid,string custID)
        {
            IEnumerable<XElement> lst = from sts in HtlStatic.Descendants("Property")                                       
                                        select new XElement("PropertyReferenceID", sts.Element("PropertyReferenceID").Value);

            List<List<XElement>> prolst = BindResort(lst);
           CreateMultipleResortSearch(prolst, Req, HtlStatic, mealtype, duration, Facility, Usrname, Password, RegionID,dmc,supid,custID);
           
          // return reqlst;
        }


        List<List<XElement>> BindResort(IEnumerable<XElement> lst)
        {
            List<List<XElement>> Reqlst = new List<List<XElement>>();
            List<XElement> local = new List<XElement>();

            foreach (XElement item in lst)
            {
                if (local.Count < 100)
                {
                    local.Add(item);

                }
                else
                {
                    Reqlst.Add(local);
                    local = null;
                    local = new List<XElement>();
                    local.Add(item);

                }
            }
            if (local.Count>0)
            {
                Reqlst.Add(local);
            }

            return Reqlst;
        }

        void CreateMultipleResortSearch(List<List<XElement>> prolst, XElement req, XElement HtlStatic, XElement mealtype, int duration, XElement Facility, string UsrName, string Password, int RegionID,string dmc,int supid,string custID)
        {
            int value = 0;
             int TotalRoom = 0;
           
            TotalRoom = Convert.ToInt32(req.Descendants("RoomPax").Count());
            List<Thread> thlst = new List<Thread>();
            string Starttime = string.Empty;
            string endtime = string.Empty;
            foreach (var item in prolst)
            {
                IEnumerable<XElement> ele = from htl in req.Descendants("searchRequest")
                                   select new XElement("SearchRequest",
                                new XElement("LoginDetails",
                                    new XElement("Login", UsrName),
                                     new XElement("Password", Password),
                                      new XElement("CurrencyID", "2")),
                                      new XElement("SearchDetails",
                                          new XElement("ArrivalDate", JacHelper.MyDate(htl.Element("FromDate").Value)),
                                           new XElement("Duration", JacHelper.GetDuration(htl.Element("ToDate").Value, htl.Element("FromDate").Value, out value)),
                                               new XElement("PropertyReferenceIDs", item),
                                               new XElement("MealBasisID", "0"),
                                               new XElement("MinStarRating", htl.Element("MinStarRating").Value),
                                              new XElement("RoomRequests", from room in htl.Element("Rooms").Elements("RoomPax")
                                                            select new XElement("RoomRequest",
                                                                new XElement("Adults", room.Element("Adult").Value),
                                                                 new XElement("Children", JacHelper.GetChildCount(room.Elements("ChildAge"))),
                                                                 new XElement("Infants", JacHelper.GetInfantsCount(room.Elements("ChildAge"))),
                                                                 JacHelper.BindChild(room.Elements("ChildAge"))))));
                if (ele.FirstOrDefault()!=null)
                {
                    string Request = ele.FirstOrDefault().ToString();
                    RequestClass requobj = new RequestClass();
                    requobj.MyEvent += requobj_MyEvent;
                    Thread th = new Thread(new ThreadStart(() => requobj.MultiplePropetySearch(Request, duration, HtlStatic, mealtype, TotalRoom, Facility, req,RegionID,dmc,supid,custID)));
                    th.Start();
                    thlst.Add(th);
                    
                    //th.Join();
                    //th.Abort(); 
                }

                
              

           }


            foreach (Thread item in thlst)
            {
                item.Join();
            }
            foreach (Thread item in thlst)
            {
                item.Abort();
            }
            
           
        }

        void requobj_MyEvent(List<XElement> lst)
        {
            if (MyEvent!=null)
            {
                MyEvent(lst);
            }
        }


        public void GetResortData(XElement Req, string custID, string supid, string dmc)
        {
            using (JacService jacSrv = new JacService(custID, supid, dmc))
            {

                var list = jacSrv.SearchByHotel(Req);
                if (MyEvent != null)
                {
                    MyEvent(list.Descendants("Hotel").ToList());
                }
            }

        }

       
       

    }
}