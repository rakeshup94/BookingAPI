using System;
using System.Collections.Generic;


namespace TravillioXMLOutService.Transfer.Models.HB
{
    public class SearchResponseModel
    {
        public Search search { get; set; }
        public IList<Services> services { get; set; }
    }
    public class Departure
    {
        public string date { get; set; }
        public string time { get; set; }
    }
    public class Occupancy
    {
        public int adults { get; set; }
        public int children { get; set; }
        public int infants { get; set; }
    }
    public class Location
    {
        public string code { get; set; }
        public string description { get; set; }
        public string type { get; set; }
    }
    public class Search
    {
        public string language { get; set; }
        public Departure departure { get; set; }
        public Departure comeBack { get; set; }
        public Occupancy occupancy { get; set; }
        public Location from { get; set; }
        public Location to { get; set; }
    }
    public class Vehicle
    {
        public string code { get; set; }
        public string name { get; set; }

    }
    public class Category
    {
        public string code { get; set; }
        public string name { get; set; }

    }
   
    public class CheckPickup
    {
        public bool mustCheckPickupTime { get; set; }
        public string url { get; set; }
        public int? hoursBeforeConsulting { get; set; }

    }
    public class Pickup
    {
        public string address { get; set; }
        public string number { get; set; }
        public string town { get; set; }
        public string zip { get; set; }
        public string description { get; set; }
        public decimal? altitude { get; set; }
        public decimal? latitude { get; set; }
        public decimal? longitude { get; set; }
        public CheckPickup checkPickup { get; set; }
        public string pickupId { get; set; }
        public string stopName { get; set; }
        public string image { get; set; }

    }
    public class PickupInformation
    {
        public Location from { get; set; }
        public Location to { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public Pickup pickup { get; set; }

    }
 
    public class Images
    {
        public string url { get; set; }
        public string type { get; set; }

    }
    public class TransferDetailInfo
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }

    }
    public class TransferRemarks
    {
        public string type { get; set; }
        public string description { get; set; }
        public bool mandatory { get; set; }

    }
    public class Content
    {
        public Vehicle vehicle { get; set; }
        public Category category { get; set; }
        public IList<Images> images { get; set; }
        public IList<TransferDetailInfo> transferDetailInfo { get; set; }
        public IList<string> customerTransferTimeInfo { get; set; }
        public IList<string> supplierTransferTimeInfo { get; set; }
        public IList<TransferRemarks> transferRemarks { get; set; }

    }
    public class Price
    {
        public double totalAmount { get; set; }
        public double? netAmount { get; set; }
        public string currencyId { get; set; }

    }
    public class CancellationPolicies
    {
        public double amount { get; set; }
        public DateTime from { get; set; }
        public string currencyId { get; set; }
        public string isForceMajeure { get; set; }

    }
    public class Links
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string method { get; set; }

    }
    public class Services
    {
        public int id { get; set; }
        public string direction { get; set; }
        public string transferType { get; set; }
        public Vehicle vehicle { get; set; }
        public Category category { get; set; }
        public PickupInformation pickupInformation { get; set; }
        public int minPaxCapacity { get; set; }
        public int maxPaxCapacity { get; set; }
        public Content content { get; set; }
        public Price price { get; set; }
        public string rateKey { get; set; }
        public IList<CancellationPolicies> cancellationPolicies { get; set; }
        public IList<Links> links { get; set; }
        public int factsheetId { get; set; }

    }

}
