  using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;


namespace TravillioXMLOutService.Supplier.TravelGate
{
    
    public partial class Welcome
    {
        [JsonProperty("criteria")]
        public Criteria Criteria { get; set; }
    }

    public partial class Criteria
    {
        [JsonProperty("checkIn")]
        public string CheckIn { get; set; }

        [JsonProperty("checkOut")]
        public string CheckOut { get; set; }

        [JsonProperty("hotels")]
        [JsonConverter(typeof(DecodeArrayConverter))]
        public string[] Hotels { get; set; }

        [JsonProperty("occupancies")]
        public Occupancy[] Occupancies { get; set; }
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("nationality")]
        public string Nationality { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("market")]
        public string Market { get; set; }
    }
    public partial class Settings
    {
        [JsonProperty("client")]
        public string Client { get; set; }
        [JsonProperty("testMode")]
        public bool TestMode { get; set; }
        [JsonProperty("context")]
        public string Context { get; set; }
        [JsonProperty("auditTransactions")]
        public Boolean AuditTransactions { get; set; }
        [JsonProperty("timeout")]
        public int TimeOut { get; set; }
    }
    public partial class Occupancy
    {
        [JsonProperty("paxes")]
        public List<Pax> Paxes { get; set; }
        public Occupancy()
        {
            Paxes = new List<Pax>();
        }
    }
    public partial class Pax
    {
        [JsonProperty("age")]
        public long Age { get; set; }
    }
    public partial class Filter
    {
        [JsonProperty("filter")]
        public Access filter { get; set; }
    }
    public partial class Access
    {
        [JsonProperty("access")]
        public Includes access { get; set; }
    }
    public partial class Includes
    {
        [JsonProperty("includes")]
        public string includes { get; set; }
    }
    public partial class CriteriaQuote
    {
        [JsonProperty("optionRefId")]
        public string OptionRefID { get; set; }
        [JsonProperty("language")]
        public string Language { get; set; }
    }
    public partial class BookInput
    {
        [JsonProperty("optionRefId")]
        public string OptionReferenceID { get; set; }
        [JsonProperty("clientReference")]
        public string ClientReference { get; set; }
        [JsonProperty("deltaPrice")]        
        public DeltaPrice DeltaPrice { get; set; }
        [JsonProperty("remarks")]
        public string specialRequest { get; set; }
        [JsonProperty("holder")]
        public Holder LeadPax { get; set; }
        [JsonProperty("rooms")]
        public Rooms[] Rooms { get; set; }

    }
    public partial class DeltaPrice
    {
        [JsonProperty("amount")]
        public double Amount { get; set; }
        [JsonProperty("percent")]
        public double PercentageAmount { get; set; }
        [JsonProperty("applyBoth")]
        public bool ApplyBoth { get; set; }
    }
    public partial class Holder
    {
        [JsonProperty("name")]
        public string FirstName { get; set; }
        [JsonProperty("surname")]
        public string LastName { get; set; }
    }
    public partial class Rooms
    {
        [JsonProperty("occupancyRefId")]
        public long OccupancyReferenceId { get; set; }
        [JsonProperty("paxes")]
        public PaxDetail[] Paxes { get; set; }
    }
    public partial class PaxDetail
    {
        [JsonProperty("name")]
        public string FirstName { get; set; }
        [JsonProperty("surname")]
        public string LastName { get; set; }
        [JsonProperty("age")]
        public long Age { get; set; }

    }
    public partial class CancelInput
    {
        [JsonProperty("accessCode")]
        public string AccessCode { get; set; }
        [JsonProperty("language")]
        public string Language { get; set; }
        [JsonProperty("hotelCode")]
        public string HotelID { get; set; }
        [JsonProperty("reference")]
        public CancelReference ReferenceIds { get; set; }
    }
    public partial class CancelReference
    {
        [JsonProperty("client")]
        public string ClientReference{get;set;}
        [JsonProperty("supplier")]
        public string SupplierReference{get;set;}
    }
    public partial class BookingListReferences
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }
      
        [JsonProperty("hotelCode")]
        public string HotelCode { get; set; }
        [JsonProperty("references")]
        public List<CancelReference> ReferenceIds { get; set; }
    }

    public partial class BookingListCriteria
    {
        [JsonProperty("accessCode")]
        public string AccessCode { get; set; }

        [JsonProperty("language")]
        public string language { get; set; }
        [JsonProperty("references")]
        public BookingListReferences References { get; set; }
        [JsonProperty("typeSearch")]
        [JsonConverter(typeof(PlainJsonStringConverter))]
        
        public string typeSearch { get; set; }
    }

    public partial class Welcome
    {
        public static Welcome FromJson(string json)
        {
            return JsonConvert.DeserializeObject<Welcome>(json, TravillioXMLOutService.Supplier.TravelGate.Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this Welcome self)
        {
            return JsonConvert.SerializeObject(self, TravillioXMLOutService.Supplier.TravelGate.Converter.Settings);
        }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            //MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class DecodeArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(long[]);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            reader.Read();
            var value = new List<string>();
            while (reader.TokenType != JsonToken.EndArray)
            {
                var converter = ParseStringConverter.Singleton;
                var arrayItem = (string)converter.ReadJson(reader, typeof(string), null, serializer);
                value.Add(arrayItem);
                reader.Read();
            }
            return value.ToArray();
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            var value = (string[])untypedValue;
            writer.WriteStartArray();
            foreach (var arrayItem in value)
            {
                var converter = ParseStringConverter.Singleton;
                converter.WriteJson(writer, arrayItem, serializer);
            }
            writer.WriteEndArray();
            return;
        }

        public static readonly DecodeArrayConverter Singleton = new DecodeArrayConverter();
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t)
        {
            return t == typeof(long) || t == typeof(long?);
        }

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (string)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
    public class PlainJsonStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue((string)value);
        }
    }
}


    