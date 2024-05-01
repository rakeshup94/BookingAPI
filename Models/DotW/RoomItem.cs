using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Models.DotW
{
    public class RoomItem
    {
        public long roomId
        {
            get;
            set;
        }
        public string roomKey
        {
            get;
            set;
        }
        public int mealId
        {
            get;
            set;
        }
        public decimal roomRate
        {
            get;
            set;
        }
        public bool refundable
        {
            get;
            set;
        }
        
    }

    public class RoomType
    {
    
        public string groupKey
        {
            get;
            set;
        }
        public int mealId
        {
            get;
            set;
        }
        public bool refundable
        {
            get;
            set;
        }
        public decimal groupRate
        {
            get;
            set;
        }

        public ICollection<string> roomKeys
        {
            get;
            set;
        }
        
       

    }
}