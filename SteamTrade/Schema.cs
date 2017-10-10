using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using log4net;
using SourceSchemaParser.Utilities;

namespace SteamTrade
{
    /// <summary>
    /// This class represents the Dota2 Item schema as deserialized from its
    /// JSON representation.
    /// </summary>
    public class Schema
    {
        private static Schema _schema = null;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Inventory));
        private const string SchemaApiUrlBase = "https://api.steampowered.com/IEconItems_570/GetSchemaURL/v0001/?key=";
        private const string Cachefile = "cache/dota2_schema.cache";

        public static Schema GetSchema()
        {
            return _schema;
        }
        /// <summary>
        /// Fetches the Tf2 Item schema.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        /// <returns>A  deserialized instance of the Item Schema.</returns>
        /// <remarks>
        /// The schema will be cached for future use if it is updated.
        /// </remarks>
        public static Schema Init(string apiKey, string schemaLang = null)
        {
            if (_schema != null)
            {
                return _schema;
            }
            var url = SchemaApiUrlBase + apiKey;
            if (schemaLang != null)
                url += "&language=" + schemaLang;
            // Get Schema URL
            Regex regex = new Regex("https.*txt");
            DateTime schemaLastModified;
            using (HttpWebResponse response = new SteamWeb().Request(url, "GET"))
            {
                schemaLastModified = response.LastModified;

                string result;
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    result = reader.ReadToEnd();
                }
                url = regex.Match(result).Value;
            }
            _schema = GetSchema(url, schemaLastModified);
            return _schema;
        }

        // Gets the schema from the web or from the cached file.
        private static Schema GetSchema(string url, DateTime schemaLastModified)
        {
            bool mustUpdateCache = !File.Exists(Cachefile) || schemaLastModified > File.GetCreationTime(Cachefile);
            var items = ParseLocalSchema();
            List<string> lines = new List<string>();
            Dictionary<string, Item> newItems = new Dictionary<string, Item>();
            if (mustUpdateCache)
            {

                using (HttpWebResponse response = new SteamWeb().Request(url, "GET"))
                {

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        while (!reader.EndOfStream)
                        {
                            lines.Add(reader.ReadLine());
                        }
                    }
                }
                newItems = ParseWebSchema(VDFConvert.ToJson(lines.ToArray()));
            }
            if (items == null)
            {
                items = newItems;
            }
            else
            {
                foreach (var item in newItems)
                {
                    if (!items.ContainsKey(item.Key))
                    {
                        items[item.Key] = item.Value;
                    }
                }
            }
            var schema = new Schema()
            {
                _items = items
            };
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(Cachefile)??"");
            File.WriteAllText(Cachefile, new JavaScriptSerializer { MaxJsonLength = 62914560 }.Serialize(schema.GetItems()));
            return schema;
        }

        private static Dictionary<string, Item> ParseWebSchema(string json)
        {
            var jss = new JavaScriptSerializer {MaxJsonLength = 62914560};
            var dict = jss.Deserialize<Dictionary<string, dynamic>>(json);
            var itemDic = dict["items_game"]["items"];
            var items = new Dictionary<string, Item>();
            foreach (var key in itemDic.Keys)
            {
                if (itemDic[key] != null)
                {
                    items[key] = new Item()
                    {
                        Name  = itemDic[key]["name"],
                        Price = 10000.0,
                        Index = key
                    };
                }
            }
            return items;
        }

        private static Dictionary<string, Item> ParseLocalSchema()
        {
            if (!File.Exists(Cachefile))
            {
                return null;
            }
            try
            {
                string cache;
                using (TextReader reader = new StreamReader(Cachefile))
                {
                    cache = reader.ReadToEnd();
                }
                var jss = new JavaScriptSerializer { MaxJsonLength = 62914560 };
                var dict = jss.Deserialize<List<dynamic>>(cache);
                var items = new Dictionary<string, Item>();
                foreach (var item in dict)
                {
                    if (item != null)
                    {
                        items[item["Index"]] = new Item()
                        {
                            Name = item["Name"],
                            Price = item["Price"],
                            Index = item["Index"]
                        };
                    }
                }
                return items;
            }
            catch (Exception e)
            {
                Log.Error("Cache File Parse Fails " + e.Message);
                throw;
            }
        }

        private Dictionary<string, Item> _items;


        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Item GetItem (int defindex)
        {
            Item item = null;
            _items.TryGetValue(defindex.ToString(), out item);
            return item;
        }



        public List<Item> GetItems()
        {
            return _items.Values.ToList();
        }


        public class Item
        {
            public string Name { get; set; }

            public double Price { get; set; }

            public string Index { get; set; }

            public override string ToString()
            {
                return "[Name:  " + Name + "]" + "[Price:  $" + Price + "]" + "[Index:  " + Index + "]";
            }
        }

    }
}

