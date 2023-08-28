using Newtonsoft.Json;

namespace BotLooter.Steam.Contracts.Responses;

public class Action
{
    [JsonProperty("link")]
    public string Link { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class Asset
{
    [JsonProperty("appid")]
    public int Appid { get; set; }

    [JsonProperty("contextid")]
    public string Contextid { get; set; }

    [JsonProperty("assetid")]
    public string Assetid { get; set; }

    [JsonProperty("classid")]
    public string Classid { get; set; }

    [JsonProperty("instanceid")]
    public string Instanceid { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    public override int GetHashCode()
    {
        return Assetid.GetHashCode();
    }
}

public class Description
{
    [JsonProperty("appid")]
    public int Appid { get; set; }

    [JsonProperty("classid")]
    public string Classid { get; set; }

    [JsonProperty("instanceid")]
    public string Instanceid { get; set; }

    [JsonProperty("currency")]
    public bool Currency { get; set; }

    [JsonProperty("background_color")]
    public string BackgroundColor { get; set; }

    [JsonProperty("icon_url")]
    public string IconUrl { get; set; }

    [JsonProperty("icon_url_large")]
    public string IconUrlLarge { get; set; }

    [JsonProperty("descriptions")]
    public List<Description> Descriptions { get; set; }

    [JsonProperty("tradable")]
    public bool Tradable { get; set; }

    [JsonProperty("actions")]
    public List<Action> Actions { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("name_color")]
    public string NameColor { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("market_name")]
    public string MarketName { get; set; }

    [JsonProperty("market_hash_name")]
    public string MarketHashName { get; set; }

    [JsonProperty("market_actions")]
    public List<MarketAction> MarketActions { get; set; }

    [JsonProperty("commodity")]
    public bool Commodity { get; set; }

    [JsonProperty("market_tradable_restriction")]
    public int MarketTradableRestriction { get; set; }

    [JsonProperty("marketable")]
    public bool Marketable { get; set; }

    [JsonProperty("tags")]
    public List<Tag> Tags { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }

    public override int GetHashCode()
    {
        return Classid.GetHashCode();
    }
}

public class MarketAction
{
    [JsonProperty("link")]
    public string Link { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class InventoryResponse
{
    [JsonProperty("assets")]
    public List<Asset>? Assets { get; set; }

    [JsonProperty("descriptions")]
    public List<Description>? Descriptions { get; set; }

    [JsonProperty("total_inventory_count")]
    public int TotalInventoryCount { get; set; }

    [JsonProperty("last_assetid")]
    public string? LastAssetId { get; set; }

    [JsonProperty("more_items")]
    public int? MoreItems { get; set; }

    [JsonProperty("success")]
    public int Success { get; set; }
}

public class Tag
{
    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("internal_name")]
    public string InternalName { get; set; }

    [JsonProperty("localized_category_name")]
    public string LocalizedCategoryName { get; set; }

    [JsonProperty("localized_tag_name")]
    public string LocalizedTagName { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }
}
