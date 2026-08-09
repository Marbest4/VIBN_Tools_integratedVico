using System.Text.RegularExpressions;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic
{
    /// <summary>
    /// Data class defining a grouping rule.
    /// </summary>
    public class GroupingRule
    {
        /// <summary>
        /// Gets or sets the list of regular expression patterns used for grouping.
        /// </summary>
        public List<Regex> PatternList { get; set; } = [];

        /// <summary>
        /// Gets or sets the function to retrieve the target field from matching data.
        /// </summary>
        public required Func<MatchingData, string> TargetField { get; set; }

        /// <summary>
        /// Gets or sets the group order indicating on which position the rule should be applied in comparison to other grouping rules.
        /// </summary>
        public int GroupOrder { get; set; }
    }
}
