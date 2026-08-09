using System.Text.RegularExpressions;
using System.Xml.Linq;
using VIBN_Tools.ContainerGeneration.BusinessLogic;
using VIBN_Tools.ContainerGeneration.Utils;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.ContainerGeneration.Models
{
    /// <summary>
    /// Class for UI settings and methods to process them. 
    /// </summary>
    public class ContainerGenerationSettings : NotifyBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private const string ROOT_ELEMENT = "CAASettings";
        private const string REGEX_SEPARATOR = ", ";

        private string _pathZuli = string.Empty;
        private string _PathRequirementsXml = string.Empty;

        private string _regexAddress = string.Empty;
        private string _regexId = string.Empty;
        private string _regexSubstitution = string.Empty;

        private bool _groupByComponent = false;
        private bool _groupByType = false;
        private bool _groupById = false;
        private bool _groupByAddress = false;

        private string _selectedOption = ComponentOption;

        /// <summary>
        /// Option identifier specifying the Component property.
        /// </summary>
        public const string ComponentOption = "Component";
        /// <summary>
        /// Option identifier specifying the ID property.
        /// </summary>
        public const string IdOption = "ID";

        /// <summary>
        /// Gets or sets the path for the ZuLi file. Notifies the UI on change.
        /// </summary
        public string PathZuli
        {
            get => _pathZuli;
            set
            {
                _pathZuli = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the path for the AutoCreate file. Notifies the UI on change.
        /// </summary
        public string PathRequirementsXml
        {
            get => _PathRequirementsXml;
            set
            {
                _PathRequirementsXml = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the regex used when generating a grouping rule for the Address property. Notifies the UI on change.
        /// </summary
        public string RegexAddress
        {
            get => _regexAddress;
            set
            {
                _regexAddress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the regex used when generating a grouping rule for the ID property. Notifies the UI on change.
        /// </summary
        public string RegexId
        {
            get => _regexId;
            set
            {
                _regexId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the regex used when generating a substitution rule for the selected option. Notifies the UI on change.
        /// </summary
        public string RegexSubstitution
        {
            get => _regexSubstitution;
            set
            {
                _regexSubstitution = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the value if the Component property should be used when creating grouping rules. Notifies the UI on change.
        /// </summary
        public bool GroupByComponent
        {
            get => _groupByComponent;
            set
            {
                _groupByComponent = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the value if the Type property should be used when creating grouping rules. Notifies the UI on change.
        /// </summary
        public bool GroupByType
        {
            get => _groupByType;
            set
            {
                _groupByType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the value if the ID property should be used when creating grouping rules. Notifies the UI on change.
        /// </summary
        public bool GroupById
        {
            get => _groupById;
            set
            {
                _groupById = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the value if the Address property should be used when creating grouping rules. Notifies the UI on change.
        /// </summary
        public bool GroupByAddress
        {
            get => _groupByAddress;
            set
            {
                _groupByAddress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the option to use when generating the substitution rule. Notifies the UI on change.
        /// </summary
        public string SelectedOption
        {
            get => _selectedOption;
            set
            {
                _selectedOption = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public ContainerGenerationSettings() { }

        /// <summary>
        /// Get all user relevant UI settings.
        /// </summary>
        /// <returns><see cref="XDocument"/> containing the current settings.</returns>
        public XDocument GetSettings()
        {
            XDocument doc = new XDocument(
                new XElement(ROOT_ELEMENT,
                    new XElement("PathZuli", PathZuli),
                    new XElement("PathAutoCreate", PathRequirementsXml),
                    new XElement("RegexAddress", RegexAddress),
                    new XElement("RegexSubstitution", RegexSubstitution),
                    new XElement("RegexId", RegexId),
                    new XElement("GroupByComponent", GroupByComponent),
                    new XElement("GroupByType", GroupByType),
                    new XElement("GroupById", GroupById),
                    new XElement("GroupByAddress", GroupByAddress),
                    new XElement("SelectedOption", SelectedOption)
                )
            );
            return doc;
        }

        /// <summary>
        /// Set the settings from the given settings file.
        /// </summary>
        /// <param name="settings"><see cref="XDocument"/> containing settings.</param>
        /// <returns><c>true</c> if the settings could be applied; otherwise <c>false</c></returns>
        public bool SetSettings(XDocument settings)
        {
            XElement? root = settings?.Element(ROOT_ELEMENT);

            if (root != null)
            {
                // Textfields
                var value = root.Element("PathZuli")?.Value;
                PathZuli = string.IsNullOrEmpty(value) ? _pathZuli : value;

                value = root.Element("PathAutoCreate")?.Value;
                PathRequirementsXml = string.IsNullOrEmpty(value) ? _PathRequirementsXml : value;

                value = root.Element("RegexAddress")?.Value;
                RegexAddress = string.IsNullOrEmpty(value) ? _regexAddress : value;

                value = root.Element("RegexId")?.Value;
                RegexId = string.IsNullOrEmpty(value) ? _regexId : value;

                value = root.Element("RegexSubstitution")?.Value;
                RegexSubstitution = string.IsNullOrEmpty(value) ? _regexSubstitution : value;

                // Checkboxes
                bool boolVal;
                bool result = bool.TryParse(root.Element("GroupByComponent")?.Value, out boolVal);
                GroupByComponent = result ? boolVal : _groupByComponent;

                result = bool.TryParse(root.Element("GroupByType")?.Value, out boolVal);
                GroupByType = result ? boolVal : _groupByType;

                result = bool.TryParse(root.Element("GroupById")?.Value, out boolVal);
                GroupById = result ? boolVal : _groupById;

                result = bool.TryParse(root.Element("GroupByAddress")?.Value, out boolVal);
                GroupByAddress = result ? boolVal : _groupByAddress;

                // RadioButton
                value = root.Element("SelectedOption")?.Value;
                SelectedOption = string.IsNullOrEmpty(value) ? _selectedOption : value;

                return true;
            }
            else
                Logger.Error("Could not load settings");
            return false;
        }

        /// <summary>
        /// Generates a list of grouping rules based on the current settings.
        /// This method creates grouping rules for components, types, IDs, and addresses and returns the result of the operation.
        /// </summary>
        /// <returns>A <see cref="Result{T}"/> containing the list of grouping rules or an error message.</returns>
        public Result<List<GroupingRule>> GenerateGroupingRules()
        {
            List<GroupingRule> rules = new List<GroupingRule>();

            if (GroupByComponent)
                rules.Add(new GroupingRule { TargetField = matchingData => matchingData.ContainerName, GroupOrder = rules.Count });

            if (GroupByType)
                rules.Add(new GroupingRule { TargetField = matchingData => matchingData.ComponentType, GroupOrder = rules.Count });

            if (GroupById && !string.IsNullOrWhiteSpace(RegexId))
            {
                List<Regex> regexes = new List<Regex>();
                if (TryParseRegexString(RegexId, REGEX_SEPARATOR, ref regexes))
                    rules.Add(new GroupingRule { PatternList = regexes, TargetField = matchingData => matchingData.ContainerEntry.ID, GroupOrder = rules.Count });
                else
                    return Result<List<GroupingRule>>.Failure("Invalid regex specified for ID!");
            }

            if (GroupByAddress && !string.IsNullOrWhiteSpace(RegexAddress))
            {
                List<Regex> regexes = new List<Regex>();
                if (TryParseRegexString(RegexAddress, REGEX_SEPARATOR, ref regexes))
                    rules.Add(new GroupingRule { PatternList = regexes, TargetField = matchingData => matchingData.ContainerEntry.Address, GroupOrder = rules.Count });
                else
                    return Result<List<GroupingRule>>.Failure("Invalid regex specified for Address!");
            }
            return Result<List<GroupingRule>>.Success(rules);
        }

        /// <summary>
        /// Generates a substitution rule based on the current settings.
        /// This method creates a substitution rule for the component name or ID and returns the result of the operation.
        /// </summary>
        /// <returns>A <see cref="Result{T}"/> containing the substitution rule or an error message.</returns>
        public Result<SubstitutionRule> GenerateSubstitutionRule()
        {
            SubstitutionRule rule = new SubstitutionRule();

            List<Regex> regexes = new List<Regex>();
            if (TryParseRegexString(RegexSubstitution, REGEX_SEPARATOR, ref regexes))
                rule.PatternList = regexes;
            else
                return Result<SubstitutionRule>.Failure("Invalid regex specified for RegexSubstitution!");

            switch (SelectedOption)
            {
                case ComponentOption:
                    rule.TargetField = matchingData => matchingData.ContainerName;
                    break;
                case IdOption:
                    rule.TargetField = matchingData => matchingData.ContainerEntry.ID;
                    break;
                default:
                    return Result<SubstitutionRule>.Failure($"Invalid option for substitution rule: {SelectedOption}");
            }
            return Result<SubstitutionRule>.Success(rule);
        }

        /// <summary>
        /// Tries to parse a string of regular expressions separated by a specified separator.
        /// This method clears the provided list, splits the input string, and attempts to create a <see cref="Regex"/> for each part.
        /// If any regex is invalid, it logs a warning and returns <c>false</c>.
        /// </summary>
        /// <param name="regexString">The string containing the regular expressions.</param>
        /// <param name="separator">The separator used to split the regular expressions.</param>
        /// <param name="regexList">The list to store the parsed <see cref="Regex"/> objects.</param>
        /// <returns><c>true</c> if all regular expressions are valid; otherwise, <c>false</c>.</returns>
        private bool TryParseRegexString(string regexString, string separator, ref List<Regex> regexList)
        {
            regexList.Clear();
            List<string> regexStrings = regexString.Split(REGEX_SEPARATOR, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            bool isValid = true;
            foreach (string item in regexStrings)
            {
                try
                {
                    Regex regex = SafeRegex.Create(item);
                    regexList.Add(regex);
                }
                catch (ArgumentException e)
                {
                    Logger.Warn(e, "Invalid regex specified: {regex}", item);
                    isValid = false;
                }

                if (!isValid)
                    break;
            }
            return isValid;
        }
    }
}
