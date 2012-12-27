using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SteamTrade
{
    public class Schema
    {

        public static Schema FetchSchema (string apiKey)
        {
            var url = "http://api.steampowered.com/IEconItems_440/GetSchema/v0001/?key=" + apiKey;
            string schemaFile = "schema_440.json";
            string result = "";
            DateTime dateTimeUTCNow = DateTime.UtcNow;
            SchemaResult schemaResult;
            if (System.IO.File.Exists(schemaFile))
            {
                schemaResult = JsonConvert.DeserializeObject<SchemaResult>(System.IO.File.ReadAllText("schema_440.json"));
                //return JsonConvert.DeserializeObject<SchemaResult>(System.IO.File.ReadAllText("schema_440.json")).result;
                result = SteamWeb.Fetch(url, "GET", null, null, true, schemaResult.result.DateLastUpdated);
                if (result == "not changed")
                {
                    schemaResult.result.Updated = false;
                    return schemaResult.result;
                }
                else
                {
                    schemaResult = JsonConvert.DeserializeObject<SchemaResult>(result);
                    schemaResult.result.DateLastUpdated = dateTimeUTCNow;
                    System.IO.File.WriteAllText(schemaFile, JsonConvert.SerializeObject(schemaResult, Formatting.Indented));
                    schemaResult.result.Updated = true;
                    return schemaResult.result;
                }
            }
            else
            {
                result = SteamWeb.Fetch(url, "GET");
                schemaResult = JsonConvert.DeserializeObject<SchemaResult>(result);
                schemaResult.result.DateLastUpdated = dateTimeUTCNow;
                schemaResult.result.Updated = true;
                System.IO.File.WriteAllText(schemaFile, JsonConvert.SerializeObject(schemaResult, Formatting.Indented));
            }
            return schemaResult.result ?? null;
        }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("items_game_url")]
        public string ItemsGameUrl { get; set; }

        [JsonProperty("items")]
        public Item[] Items { get; set; }

        [JsonProperty("originNames")]
        public ItemOrigin[] OriginNames { get; set; }

        public DateTime DateLastUpdated { get; set; }

        public bool Updated = false;

        /// <summary>
        /// Find an SchemaItem by it's defindex.
        /// </summary>
        public Item GetItem (int defindex)
        {
            foreach (Item item in Items)
            {
                if (item.Defindex == defindex)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Returns all Items of the given crafting material.
        /// </summary>
        /// <param name="material">Item's craft_material_type JSON property.</param>
        /// <seealso cref="Item"/>
        public List<Item> GetItemsByCraftingMaterial(string material)
        {
            return Items.Where(item => item.CraftMaterialType == material).ToList();
        }

        public class ItemOrigin
        {
            [JsonProperty("origin")]
            public int Origin { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class Item
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("defindex")]
            public ushort Defindex { get; set; }

            [JsonProperty("item_class")]
            public string ItemClass { get; set; }

            [JsonProperty("item_type_name")]
            public string ItemTypeName { get; set; }

            [JsonProperty("item_name")]
            public string ItemName { get; set; }

            [JsonProperty("craft_material_type")]
            public string CraftMaterialType { get; set; }

            [JsonProperty("used_by_classes")]
            public string[] UsableByClasses { get; set; }

            [JsonProperty("item_slot")]
            public string ItemSlot { get; set; }

            [JsonProperty("craft_class")]
            public string CraftClass { get; set; }

            [JsonProperty("item_quality")]
            public int ItemQuality { get; set; }
        }

        protected class SchemaResult
        {
            public Schema result { get; set; }
        }

    }
}

