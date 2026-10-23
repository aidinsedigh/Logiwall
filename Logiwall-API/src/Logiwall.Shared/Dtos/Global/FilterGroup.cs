using static Logiwall.Shared.Enums.Constants;

namespace Logiwall.Shared.Dtos.Global;
public class FilterGroup
{
    public List<FilterCondition> Conditions { get; set; } = new List<FilterCondition>();
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.And;
}
