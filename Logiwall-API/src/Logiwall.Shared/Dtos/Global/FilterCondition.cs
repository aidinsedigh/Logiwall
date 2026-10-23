using Logiwall.Shared.Utils;
using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Global;
public class FilterCondition
{
    public string? PropertyName { get; set; }

    [JsonConverter(typeof(ObjectToStringConverter))]
    public object? Value { get; set; }

    public ConditionOperator Operator { get; set; } = ConditionOperator.Equal;
}
