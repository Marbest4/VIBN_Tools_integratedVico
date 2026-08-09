using System.Text.RegularExpressions;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic
{
    /// <summary>
    /// Data class defining a substitution rule.
    /// </summary>
    public class SubstitutionRule
    {
        /// <summary>
        /// Gets or sets the list of regular expression patterns used for the substitution.
        /// </summary>
        public List<Regex> PatternList { get; set; } = [];

        /// <summary>
        /// Gets or sets the function to retrieve the target field from matching data.
        /// </summary>
        public Func<MatchingData, string> TargetField { get; set; } = _ => string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubstitutionRule"/> class.
        /// </summary>
        public SubstitutionRule() { }
    }
}
