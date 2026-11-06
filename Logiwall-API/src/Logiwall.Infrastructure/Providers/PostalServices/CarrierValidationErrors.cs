using System.Text.Json;
using Logiwall.Shared.Exceptions;
using Logiwall.Shared.Resources;
using Microsoft.Extensions.Localization;

namespace Logiwall.Infrastructure.Providers.PostalServices;

public static class CarrierValidationErrors
{
    // Only known field identifiers are returned. External text may contain credentials or internal details.
    public static ErrorResourcePayload Parse(string raw, IStringLocalizer<AppStrings> localizer)
    {
        var names = new HashSet<string>();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            Visit(doc.RootElement, 0);
        }
        catch (JsonException) { }
        if (names.Count == 0) names.Add("*");
        return new() { Details = names.Select(name =>
        {
            var field = name.Split('.').Last();
            var key = field switch
            {
                "packagingReferenceId" => "ShippingPackagingRequired",
                "contentReferenceId" => "ShippingContentRequired",
                "weight" => "ShippingCarrierWeightInvalid",
                "length" or "width" or "height" => "ShippingCarrierDimensionsInvalid",
                "parcelValue" => "ShippingCarrierValueInvalid",
                _ => "ShippingCarrierValidationFailed"
            };
            return new PropertyErrorResourceCollection
            { Name = name, Errors = [new() { Key = key, Message = localizer[key] }] };
        }).ToList() };

        void Visit(JsonElement node, int depth)
        {
            if (depth > 6 || names.Count >= 20) return;
            if (node.ValueKind == JsonValueKind.Object)
                foreach (var property in node.EnumerateObject())
                {
                    var path = property.Name.ToLowerInvariant();
                    var field = path.Contains("packing") ? "packagingReferenceId"
                        : path.Contains("content") ? "contentReferenceId"
                        : path.Contains("weight") ? "weight"
                        : path.Contains("length") ? "length"
                        : path.Contains("width") ? "width"
                        : path.Contains("height") ? "height"
                        : path.Contains("postalcode") ? "postalCode"
                        : path.Contains("city") ? "city"
                        : path.Contains("mobile") ? "phoneNumber"
                        : path.Contains("packagevalue") ? "parcelValue" : null;
                    if (field is not null)
                    {
                        var index = System.Text.RegularExpressions.Regex.Match(path, @"(?:packages|packageinputs)\[(\d{1,4})\]");
                        names.Add(index.Success ? $"orderParcels[{index.Groups[1].Value}].{field}" : field);
                    }
                    if (property.Name is "errors" or "validationErrors" or "result" or "data") Visit(property.Value, depth + 1);
                }
            else if (node.ValueKind == JsonValueKind.Array)
                foreach (var child in node.EnumerateArray().Take(20)) Visit(child, depth + 1);
        }
    }
}
