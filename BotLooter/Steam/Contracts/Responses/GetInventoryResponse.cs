using System.Text.Json.Serialization;

namespace BotLooter.Steam.Contracts.Responses;

public class Asset
{
    [JsonPropertyName("appid")]
    public int Appid { get; set; }

    [JsonPropertyName("contextid")]
    public string Contextid { get; set; }

    [JsonPropertyName("assetid")]
    public string Assetid { get; set; }

    [JsonPropertyName("classid")]
    public string Classid { get; set; }

    [JsonPropertyName("instanceid")]
    public string Instanceid { get; set; }

    [JsonPropertyName("amount")]
    public string Amount { get; set; }
}

public class Description
{
    [JsonPropertyName("appid")]
    public int Appid { get; set; }

    [JsonPropertyName("classid")]
    public string Classid { get; set; }

    [JsonPropertyName("instanceid")]
    public string Instanceid { get; set; }

    [JsonPropertyName("currency")]
    public int Currency { get; set; }

    [JsonPropertyName("background_color")]
    public string BackgroundColor { get; set; }

    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    [JsonPropertyName("icon_url_large")]
    public string IconUrlLarge { get; set; }

    [JsonPropertyName("descriptions")]
    public List<Description> Descriptions { get; set; }

    [JsonPropertyName("tradable")]
    public int Tradable { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("market_name")]
    public string MarketName { get; set; }

    [JsonPropertyName("market_hash_name")]
    public string MarketHashName { get; set; }

    [JsonPropertyName("market_fee_app")]
    public int MarketFeeApp { get; set; }

    [JsonPropertyName("commodity")]
    public int Commodity { get; set; }

    [JsonPropertyName("market_tradable_restriction")]
    public int MarketTradableRestriction { get; set; }

    [JsonPropertyName("market_marketable_restriction")]
    public int MarketMarketableRestriction { get; set; }

    [JsonPropertyName("marketable")]
    public int Marketable { get; set; }

    [JsonPropertyName("tags")]
    public List<Tag> Tags { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class GetInventoryResponse
{
    [JsonPropertyName("assets")]
    public List<Asset>? Assets { get; set; }

    [JsonPropertyName("descriptions")]
    public List<Description> Descriptions { get; set; }

    [JsonPropertyName("more_items")]
    public int MoreItems { get; set; }

    [JsonPropertyName("last_assetid")]
    public string LastAssetid { get; set; }

    [JsonPropertyName("total_inventory_count")]
    public int TotalInventoryCount { get; set; }

    [JsonPropertyName("success")]
    public int Success { get; set; }

    [JsonPropertyName("rwgrsn")]
    public int Rwgrsn { get; set; }
}

public class Tag
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("internal_name")]
    public string InternalName { get; set; }

    [JsonPropertyName("localized_category_name")]
    public string LocalizedCategoryName { get; set; }

    [JsonPropertyName("localized_tag_name")]
    public string LocalizedTagName { get; set; }
}