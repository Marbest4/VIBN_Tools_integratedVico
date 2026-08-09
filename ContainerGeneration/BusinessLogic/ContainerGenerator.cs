using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Concurrent;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic
{
    public class ContainerGenerator
    {
        private readonly ConcurrentDictionary<(string Key, bool IgnoreCase), Regex> _keyRegexCache = new();

        /// <summary>
        /// Gets or sets the property indicating if key matches should be case-sensitive or not.
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        /// <summary>
        /// Gets or sets the property indicating if the list of signals (e.g. read from ZuLi) should be filtered
        /// before processing (e.g. by filter list defined by AutoCreate XML).
        /// </summary>
        public bool UseFilterList { get; set; } = true;

        /// <summary>
        /// List of generated <see cref="ComponentContainer"/>.
        /// </summary>
        public List<ComponentContainer> GeneratedContainer { get; private set; } = new List<ComponentContainer>();

        /// <summary>
        /// List of signal which could not be assigned to a component slot.
        /// </summary>
        public List<ContainerEntry> NotMatchingSignals { get; private set; } = new List<ContainerEntry> { };

        /// <summary>
        /// List of signal which were filtered.
        /// </summary>
        public List<ContainerEntry> FilteredSignals { get; private set; } = new List<ContainerEntry> { };

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerGenerator"/> class.
        /// </summary>
        public ContainerGenerator() { }

        /// <summary>
        /// Asynchronously generates containers based on the provided signal list, AutoCreate document, grouping rules, and optional substitution rule.
        /// </summary>
        /// <param name="signalList">List of signals to be processed.</param>
        /// <param name="autoCreateDoc">XML document used for auto-creation of components.</param>
        /// <param name="rules">List of rules for grouping components.</param>
        /// <param name="substRule">Optional substitution rule for container names.</param>
        /// <returns>A task representing the asynchronous operation.</returns
        public async Task GenerateAsync(
            List<ContainerEntry> signalList,
            XDocument autoCreateDoc,
            List<GroupingRule> rules,
            SubstitutionRule? substRule = null)
        {
            var result = await GenerateAsync(
                new ContainerGenerationRequest(
                    signalList,
                    autoCreateDoc,
                    rules,
                    substRule,
                    IgnoreCase,
                    UseFilterList));

            ApplyResult(result);
        }

        public Task<ContainerGenerationResult> GenerateAsync(
            ContainerGenerationRequest request,
            CancellationToken cancellationToken = default) =>
            Task.Run(() => Generate(request, cancellationToken), cancellationToken);

        /// <summary>
        /// Generates containers based on the provided signal list, AutoCreate document, grouping rules, and optional substitution rule.
        /// </summary>
        /// <param name="signalList">List of signals to be processed.</param>
        /// <param name="autoCreateDoc">XML document used for auto-creation of components.</param>
        /// <param name="rules">List of rules for grouping components.</param>
        /// <param name="substRule">Optional substitution rule for container names.</param>
        /// <remarks>
        /// This method performs the following steps:
        /// 1. Clears the <see cref="GeneratedContainer"/> and <see cref="NotMatchingSignals"/> lists.
        /// 2. Filters the <paramref name="signalList"/> based on ignore keys if <see cref="UseFilterList"/> is true.
        /// 3. Generates flat matching data by iterating through the filtered signal list and identifying matching components.
        ///    - Adds signals with no matching components or multiple matching components to <see cref="NotMatchingSignals"/>.
        ///    - Adds signals with a unique matching component to the ungrouped matches list.
        /// 4. Substitutes container names in the ungrouped matches if <paramref name="substRule"/> is provided.
        /// 5. Groups the ungrouped matches based on the provided <paramref name="rules"/> and creates containers.
        ///    - Adds the created containers to <see cref="GeneratedContainer"/>.
        /// 6. Adds remaining ungrouped matches to <see cref="NotMatchingSignals"/>.
        /// 7. Logs the generation data including total signals, filtered signals, matches, and generated containers.
        /// </remarks>
        public void Generate(List<ContainerEntry> signalList, XDocument autoCreateDoc, List<GroupingRule> rules, SubstitutionRule? substRule = null)
        {
            var result = Generate(
                new ContainerGenerationRequest(
                    signalList,
                    autoCreateDoc,
                    rules,
                    substRule,
                    IgnoreCase,
                    UseFilterList));

            ApplyResult(result);
        }

        public ContainerGenerationResult Generate(
            ContainerGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Requirements);
            _keyRegexCache.Clear();

            var generatedContainers = new List<ComponentContainer>();
            var notMatchingSignals = new List<ContainerEntry>();
            var filteredSignals = new List<ContainerEntry>();
            var signalList = request.Signals.Select(entry => entry.Clone()).ToList();
            List<MatchingData> ungroupedMatches = [];

            // filter signalList based on ignore keys
            List<ContainerEntry> filteredSignalList = request.UseFilterList
                ? FilterEntriesBySignal(request.Requirements, signalList, request.IgnoreCase)
                : signalList;

            filteredSignals.AddRange(signalList.Where(x => !filteredSignalList.Contains(x)));

            // generate flat matching data
            foreach (ContainerEntry entry in filteredSignalList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // var signalParts = entry.Signal.Split(" ", StringSplitOptions.TrimEntries);
                var matchingComponents = GetMatchingComponents(
                    request.Requirements,
                    entry,
                    request.IgnoreCase);

                if (matchingComponents.Count == 0)
                {
                    notMatchingSignals.Add(entry);
                    if (entry.Note == "")
                    {
                        entry.Note = "No matching components.";
                    }
                }
                else if (matchingComponents.Count > 1)
                {
                    // add to not matching signals if no unique match to single component is possible
                    string slotNames = string.Join(" ", matchingComponents.Select(e => e.ContainerEntry.Slot));
                    Trace.TraceWarning(
                        "Multiple slots {0} found for signal '{1}'.",
                        slotNames,
                        entry.Signal);
                    entry.Note = "Multiple slots " + slotNames + " found for signal: " + entry.Signal + "'";
                    notMatchingSignals.Add(entry);
                }
                else
                {
                    var matchingData = matchingComponents[0];
                    ungroupedMatches.Add(matchingData);
                }
            }

            // Substitute container name
            if (request.SubstitutionRule != null)
                SubstituteContainerNames(ungroupedMatches, request.SubstitutionRule);

            // Create containers based on groups
            var groupedItems = GroupItems(ref ungroupedMatches, request.GroupingRules.ToList());
            foreach (var group in groupedItems)
            {
                ComponentContainer container = new()
                {
                    //Id = group.Value[0].ComponentName,
                    Component = group.Value[0].ContainerName,
                    Type = group.Value[0].ComponentType,
                    MinSignals = group.Value[0].MinSignals,
                    MaxSignals = group.Value[0].MaxSignals,
                    DataList = []
                };

                foreach (var item in group.Value)
                    container.DataList.Add(item.ContainerEntry);

                generatedContainers.Add(container);
            }

            // Add remaining not grouped matches to NotMatchingSignals
            foreach (var item in ungroupedMatches)
                notMatchingSignals.Add(item.ContainerEntry);

            int matches = filteredSignalList.Count - notMatchingSignals.Count;
            int filtered = signalList.Count - filteredSignalList.Count;
            var statistics = new ContainerGenerationStatistics(
                signalList.Count,
                filtered,
                matches,
                notMatchingSignals.Count,
                generatedContainers.Count);

            Trace.TraceInformation(
                "Generation data: Total signals: {0} - Filtered: {1} - Matches: {2} ({3:P2}) - Generated containers: {4}",
                statistics.TotalSignals,
                statistics.FilteredSignals,
                statistics.MatchedSignals,
                statistics.MatchRate,
                statistics.GeneratedContainers);

            return new ContainerGenerationResult(
                generatedContainers,
                notMatchingSignals,
                filteredSignals,
                statistics);
        }

        private void ApplyResult(ContainerGenerationResult result)
        {
            GeneratedContainer = result.Containers.ToList();
            NotMatchingSignals = result.UnassignedSignals.ToList();
            FilteredSignals = result.FilteredSignals.ToList();
        }

        /// <summary>
        /// Filters the list of container entries by their signal values based on the provided XML document.
        /// </summary>
        /// <param name="doc">The XML document containing filter keys.</param>
        /// <param name="signalList">The list of container entries to filter.</param>
        /// <param name="ignoreCase">Indicates whether to ignore case in the matching process.</param>
        /// <returns>A filtered list of container entries.</returns>
        /// <remarks>
        /// This method filters the provided list of container entries by checking their signal values against the filter keys
        /// specified in the XML document. It performs the following steps:
        /// 1. Retrieves all filter keys from the XML document.
        /// 2. Iterates through the list of container entries and checks if any filter key matches the signal value of the entry.
        /// 3. If a match is found, the entry is excluded from the filtered list.
        /// 4. Returns the filtered list of container entries that do not match any filter key.
        /// </remarks>
        private List<ContainerEntry> FilterEntriesBySignal(XDocument doc, List<ContainerEntry> signalList, bool ignoreCase)
        {
            var filterKeys = doc.Descendants("FilterList").Descendants("Key").ToList();
            var filteredList = signalList.Where(signal =>
            {
                return !filterKeys.Any(key => MatchKeyPattern(signal.Signal, key.Value, ignoreCase));
            });

            return filteredList.ToList();
        }

        /// <summary>
        /// Substitutes the container names in the list of matching data based on the provided substitution rule.
        /// </summary>
        /// <param name="items">The list of matching data items.</param>
        /// <param name="rule">The substitution rule to apply.</param>
        /// <remarks>
        /// This method substitutes the container names in the provided list of matching data items using the specified substitution rule.
        /// It performs the following steps:
        /// 1. Iterates through the list of matching data items.
        /// 2. Sets the container name of each item to the value obtained from the substitution rule's target field.
        /// 3. Checks if the container name matches any pattern in the substitution rule's pattern list.
        /// 4. If a match is found, concatenates the matching groups and sets the container name to the concatenated value.
        /// 5. Continues to the next item if no match is found.
        /// </remarks>
        private void SubstituteContainerNames(List<MatchingData> items, SubstitutionRule rule)
        {
            foreach (MatchingData item in items)
            {
                item.ContainerName = rule.TargetField(item);
                foreach (Regex pattern in rule.PatternList)
                {
                    Match match = pattern.Match(item.ContainerName);
                    if (match.Success)
                    {
                        string concatenatedGroups = string.Join("",
                            match.Groups.Cast<Group>()
                                        .Skip(1) // Skip the first group as it contains the whole match
                                        .Select(group => group.Value));
                        item.ContainerName = concatenatedGroups;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Generates a matrix of grouping rule combinations.
        /// </summary>
        /// <param name="rules">The list of grouping rules.</param>
        /// <param name="index">The current index in the rules list.</param>
        /// <param name="current">The current list of grouping rules being processed.</param>
        /// <param name="result">The list to store the resulting combinations of grouping rules.</param>
        /// <remarks>
        /// This method recursively generates all possible combinations of the provided grouping rules and stores them in the result list.
        /// It handles rules with and without regular expression patterns differently:
        /// 
        /// 1. **Rules without Patterns**: If a rule does not have any patterns, it is added to the current list and the method recurses to the next index.
        /// 2. **Rules with Patterns**: If a rule has patterns, each pattern is processed individually. A new rule is created for each pattern,
        ///    added to the current list, and the method recurses to the next index.
        /// 
        /// The process continues until all rules have been processed, at which point the current combination of rules is added to the result list.
        /// This ensures that all possible combinations of grouping rules are generated and stored.
        /// </remarks>
        private void GenerateGroupMatrix(List<GroupingRule> rules, int index, List<GroupingRule> current, List<List<GroupingRule>> result)
        {
            if (index == rules.Count)
            {
                result.Add(new List<GroupingRule>(current));
                return;
            }

            if (rules[index].PatternList.Count == 0)
            {
                var newRule = new GroupingRule
                {
                    PatternList = new List<Regex>(),
                    TargetField = rules[index].TargetField,
                    GroupOrder = rules[index].GroupOrder
                };
                current.Add(newRule);
                GenerateGroupMatrix(rules, index + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
            else
            {
                foreach (var pattern in rules[index].PatternList)
                {
                    var newRule = new GroupingRule
                    {
                        PatternList = new List<Regex> { pattern },
                        TargetField = rules[index].TargetField,
                        GroupOrder = rules[index].GroupOrder
                    };
                    current.Add(newRule);
                    GenerateGroupMatrix(rules, index + 1, current, result);
                    current.RemoveAt(current.Count - 1);
                }
            }
        }

        /// <summary>
        /// Groups items based on the provided grouping rules.
        /// </summary>
        /// <param name="items">The list of items to group.</param>
        /// <param name="rules">The list of grouping rules.</param>
        /// <returns>A dictionary where the keys are group identifiers and the values are lists of grouped items.</returns>
        /// <remarks>
        /// This method groups items according to the specified rules by generating a matrix of rule combinations.
        /// It iterates through the items and applies the rules to create group keys. If a rule has associated regular expressions,
        /// it uses the concatenated matching groups instead of the target field value. The method continues to group items until
        /// all rules are applied, and then merges all grouped dictionaries into a final dictionary.
        /// 
        /// The process involves:
        /// 1. **Generating Group Matrix**: Creates combinations of grouping rules.
        /// 2. **Grouping Items**: Iterates through items and applies the rules to generate group keys.
        ///    - If a rule has regular expressions, it matches the target field value and concatenates the matching groups.
        ///    - If no regular expressions are present, it uses the target field value directly.
        /// 3. **Removing Processed Rules**: Removes rules that have been applied and regenerates the group matrix.
        /// 4. **Merging Grouped Dictionaries**: Combines all grouped dictionaries into a single final dictionary.
        /// </remarks>
        public Dictionary<string, List<MatchingData>> GroupItems(ref List<MatchingData> items, List<GroupingRule> rules)
        {
            var groupedDictionaries = new List<Dictionary<string, List<MatchingData>>>();
            var workingRules = rules
                .Select(rule => new GroupingRule
                {
                    PatternList = rule.PatternList.ToList(),
                    TargetField = rule.TargetField,
                    GroupOrder = rule.GroupOrder
                })
                .ToList();

            List<List<GroupingRule>> groupMatrix = new List<List<GroupingRule>>();
            GenerateGroupMatrix(workingRules, 0, new List<GroupingRule>(), groupMatrix);

            while (workingRules.Any())
            {
                var groupedItems = new Dictionary<string, List<MatchingData>>();

                foreach (var item in items.ToList())
                {
                    foreach (List<GroupingRule> ruleSet in groupMatrix)
                    {
                        var currentRules = ruleSet.OrderBy(r => r.GroupOrder).ToList();
                        var keyParts = currentRules.Select(rule =>
                        {
                            var value = string.Empty;
                            // if regex is available, use the concatinated matching groups instead of the target field value
                            if (rule.PatternList.Count > 0)
                            {
                                Match match = rule.PatternList[0].Match(rule.TargetField(item));
                                if (match.Success)
                                {
                                    string concatenatedGroups = string.Join("-",
                                        match.Groups.Cast<Group>()
                                                    .Skip(1) // Skip the first group as it contains the whole match
                                                    .Where(group => group.Success)
                                                    .Select(group => group.Value));
                                    value = concatenatedGroups;
                                }
                            }
                            else
                                value = rule.TargetField(item);
                            return value;
                        }).Where(keyPart => !string.IsNullOrEmpty(keyPart)).ToArray();

                        if (keyParts.Length == currentRules.Count)
                        {
                            // A container cannot safely combine different component
                            // types even if user-defined grouping keys collide.
                            var key = $"{item.ComponentType}\u001F{string.Join("_", keyParts)}";

                            if (!groupedItems.ContainsKey(key))
                            {
                                groupedItems[key] = new List<MatchingData>();
                            }
                            groupedItems[key].Add(item);
                            items.Remove(item);
                            break;
                        }
                    }
                }

                groupedDictionaries.Add(groupedItems);
                var highestOrder = workingRules.Max(rule => rule.GroupOrder);
                workingRules.RemoveAll(rule => rule.GroupOrder == highestOrder);
                groupMatrix = new List<List<GroupingRule>>();
                GenerateGroupMatrix(workingRules, 0, new List<GroupingRule>(), groupMatrix);
            }

            // Merge all grouped dictionaries into one
            var finalGroupedDictionary = new Dictionary<string, List<MatchingData>>();
            foreach (var dict in groupedDictionaries)
            {
                foreach (var kvp in dict)
                {
                    if (!finalGroupedDictionary.ContainsKey(kvp.Key))
                    {
                        finalGroupedDictionary[kvp.Key] = new List<MatchingData>();
                    }
                    finalGroupedDictionary[kvp.Key].AddRange(kvp.Value);
                }
            }

            return finalGroupedDictionary;
        }

        /// <summary>
        /// Retrieves a list of matching components based on the provided XML document and container entry.
        /// </summary>
        /// <param name="doc">The XML document containing component definitions.</param>
        /// <param name="cEntry">The container entry to match against the components.</param>
        /// <param name="ignoreCase">Indicates whether to ignore case in the matching process.</param>
        /// <returns>A list of <see cref="MatchingData"/> representing the matched components.</returns>
        /// <remarks>
        /// This method iterates over all components in the XML document and their respective slots to find matches for the given container entry.
        /// It combines keygroups from both components and slots, and checks for matches based on the following criteria:
        /// 
        /// 1. **Exclude Keygroups**: If any keygroup of type "exclude" contains a key that matches the container entry signal, the slot is skipped.
        /// 2. **Required Keygroups**: All keygroups of type "required" must have at least one keyset that matches the container entry signal.
        ///    - If the keygroup operator is "OR" or not specified, any keyset match is sufficient.
        ///    - If the keygroup operator is "AND", all keysets must match.
        /// 
        /// The method constructs a dictionary of key data from the matched keysets and uses it to filter the container entry signal.
        /// It then creates a new <see cref="MatchingData"/> object for each valid match and adds it to the result list.
        /// </remarks>
        private List<MatchingData> GetMatchingComponents(XDocument doc, ContainerEntry cEntry, bool ignoreCase)
        {
            List<MatchingData> matchingDataList = [];

            var components = doc.Descendants("Component");

            // iterate over all components
            foreach (var component in components)
            {
                string? componentName = component.Attribute("name")?.Value;
                string? componentType = component.Attribute("type")?.Value;
                if (string.IsNullOrWhiteSpace(componentName) ||
                    string.IsNullOrWhiteSpace(componentType))
                {
                    Trace.TraceWarning("Skipped component without name or type.");
                    continue;
                }
                int? maxSignals = null;
                if (int.TryParse(component.Attribute("maxSignals")?.Value, out int convertedMaxInt))
                {
                    maxSignals = convertedMaxInt;
                }
                int? minSignals = null;
                if (int.TryParse(component.Attribute("minSignals")?.Value, out int convertedminInt))
                {
                    minSignals = convertedminInt;
                }

                if (maxSignals != null && minSignals != null)
                {
                    if (maxSignals < minSignals)
                    {
                        Trace.TraceWarning(
                            "Conflicting Min/Max signal settings for {0}.",
                            componentName);
                    }
                }

                // select only the direct child Keygroups of the Component element
                var componentKeygroups = component.Elements("Keygroup").ToList();
                var slots = component.Descendants("Slot");

                // iterate over all slots
                foreach (var slot in slots)
                {
                    Dictionary<string, bool> keyData = [];

                    string? slotName = slot.Attribute("name")?.Value;
                    if (string.IsNullOrWhiteSpace(slotName))
                    {
                        Trace.TraceWarning(
                            "Skipped unnamed slot in component {0}.",
                            componentName);
                        continue;
                    }
                    var slotKeygroups = slot.Descendants("Keygroup").ToList();

                    // combine component and slot keygroups
                    var mergedKeygroups = componentKeygroups.Concat(slotKeygroups).ToList();

                    // no match if keyword in exclude group found
                    bool excludeMatch = mergedKeygroups.Any(keyGroup => keyGroup.Attribute("type")?.Value == "exclude" && keyGroup.Descendants("Key").Any(k => MatchKeyPattern(cEntry.Signal, k.Value, ignoreCase)));
                    if (excludeMatch)
                    {
                        cEntry.Note = "Excluded";
                        continue;
                    }
                    // get all keygroups with the required attribute
                    var requiredKeygroups = mergedKeygroups.Where(keyGroup => keyGroup.Attribute("type")?.Value == "required").ToList();

                    bool requiredMatch = requiredKeygroups.All(keyGroup =>
                    {
                        var keySets = keyGroup.Descendants("KeySet").ToList();
                        string? operatorType = keyGroup.Attribute("operator")?.Value;

                        // check for matching keys based on the keySet operator
                        if (operatorType == "OR" || operatorType == null)
                        {
                            return keySets.Any(keySet =>
                            {
                                (string Value, bool Keep) = GetMatchingKey(keySet, cEntry.Signal, ignoreCase);
                                if (string.IsNullOrEmpty(Value))
                                    return false;

                                if (!keyData.TryAdd(Value, Keep))
                                    Trace.TraceInformation(
                                        "Duplicate keywords defined for {0} - {1}.",
                                        componentName,
                                        slotName);
                                //keyData.Add(Value, Keep);
                                return true;
                            });
                        }
                        else if (operatorType == "AND")
                        {
                            return keySets.All(keySet =>
                            {
                                (string Value, bool Keep) = GetMatchingKey(keySet, cEntry.Signal, ignoreCase);
                                if (string.IsNullOrEmpty(Value))
                                    return false;

                                if (!keyData.TryAdd(Value, Keep))
                                    Trace.TraceInformation(
                                        "Duplicate keywords defined for {0} - {1}.",
                                        componentName,
                                        slotName);
                                return true;
                            });
                        }
                        return false;
                    });                    

                    if (requiredMatch)
                    {

                        // Check optional keygroups to remove (only for tuning the slot name)
                        var optionalKeygroups = mergedKeygroups.Where(keyGroup => keyGroup.Attribute("type")?.Value == "optional");

                        foreach (var keyGroup in optionalKeygroups)
                        {
                            foreach (var keySet in keyGroup.Descendants("KeySet"))
                            {
                                (string Value, bool Keep) = GetMatchingKey(keySet, cEntry.Signal, ignoreCase);

                                if (!string.IsNullOrEmpty(Value))
                                {
                                    if (!keyData.TryAdd(Value, Keep))
                                        Trace.TraceInformation(
                                            "Duplicate keywords defined for {0} - {1}.",
                                            componentName,
                                            slotName);
                                }
                            }
                        }



                        ContainerEntry entry = cEntry.Clone();
                        string containerName = FilterTextByKeyData(entry.Signal.Trim(), keyData);
                        entry.Slot = slotName;
                        matchingDataList.Add(new MatchingData(componentName, componentType, containerName, minSignals, maxSignals, entry, keyData));
                    }
                }
            }
            return matchingDataList;
        }

        /// <summary>
        /// Matches a text against a key pattern with optional case sensitivity.
        /// </summary>
        /// <param name="text">The text to match.</param>
        /// <param name="key">The key pattern to match against.</param>
        /// <param name="ignoreCase">Indicates whether to ignore case in the match.</param>
        /// <returns><c>true</c> if the text matches the key pattern; otherwise, <c>false</c>.</returns>
        private bool MatchKeyPattern(string text, string key, bool ignoreCase)
        {
            return GetMatchingKeyRegex(key, ignoreCase).IsMatch(text);
        }

        /// <summary>
        /// Retrieves the matching key and its keep attribute from an XML element.
        /// This method searches for a key in the XML element that matches the text with optional case sensitivity,
        /// and returns the key value and the keep attribute.
        /// </summary>
        /// <param name="keyset">The XML element containing the keys.</param>
        /// <param name="text">The text to match against the keys.</param>
        /// <param name="ignoreCase">Indicates whether to ignore case in the match.</param>
        /// <returns>A tuple containing the key value and the keep attribute.</returns>
        /// <exception cref="FormatException">Thrown if the keep attribute has invalid content.</exception>
        private (string Value, bool Keep) GetMatchingKey(XElement keyset, string text, bool ignoreCase)
        {
            (string Value, bool Keep) keyData = (string.Empty, default);
            var key = keyset.Descendants("Key").FirstOrDefault(k =>
            {
                var keyValue = k.Value;
                return MatchKeyPattern(text, keyValue, ignoreCase);
            });

            if (key != null)
            {
                var keepValue = key.Attribute("keep")?.Value.ToLower();
                if (!bool.TryParse(keepValue, out bool result))
                    throw new FormatException($"Invalid content for Key attribute keep!");
                keyData = (key.Value, result);
            }
            return keyData;
        }

        /// <summary>
        /// Remove keywords from a text defined in a keydata dict.
        /// </summary>
        /// <param name="text">Text to filter.</param>
        /// <param name="keyData">Dictionary with text to search for as key and a bool value indicating if the text should be removed or not.</param>
        /// <returns>Filtered text.</returns>
        private string FilterTextByKeyData(string text, Dictionary<string, bool> keyData)
        {
            // only remove elements where keep is false
            foreach (var key in keyData.Where(item => !item.Value).Select(item => item.Key))
            {
                // perform case-insensitive removal of words while preserving the original casing
                text = GetMatchingKeyRegex(key, ignoreCase: true)
                    .Replace(text, string.Empty);
            }
            // Remove double (or more) empty spaces
            text = StaticRegex.FindWhiteSpaces().Replace(text, " ").Trim();
            return text;
        }

        /// <summary>
        /// Creates a regex to match a given string in a text (e.g. to match a key in the signal text).
        /// </summary>
        /// <param name="key">Text to create the Regex</param>
        /// <returns>Regex to match the given key</returns>
        private string GetMatchingKeyPattern(string key)
        {
            return $@"(?<![\p{{L}}\p{{N}}_]){Regex.Escape(key.Trim())}(?![\p{{L}}\p{{N}}_])";
        }

        private Regex GetMatchingKeyRegex(string key, bool ignoreCase)
        {
            var normalizedKey = key.Trim();
            return _keyRegexCache.GetOrAdd(
                (normalizedKey, ignoreCase),
                cacheKey =>
                {
                    var options = RegexOptions.CultureInvariant;
                    if (cacheKey.IgnoreCase)
                        options |= RegexOptions.IgnoreCase;

                    return SafeRegex.Create(
                        GetMatchingKeyPattern(cacheKey.Key),
                        options);
                });
        }
    }

    /// <summary>
    /// Provides static methods for working with regular expressions.
    /// </summary>
    public static partial class StaticRegex
    {
        /// <summary>
        /// Finds white spaces in a string using a generated regular expression.
        /// </summary>
        /// <returns>A <see cref="Regex"/> object for matching white spaces.</returns>
        [GeneratedRegex(@"\s+")]
        public static partial Regex FindWhiteSpaces();
    }
}
