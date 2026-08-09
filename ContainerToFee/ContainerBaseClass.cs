using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ContainerToFee
{
    public abstract class ContainerBaseClass
    {
        public string ComponentName { get; set; }
        public Dictionary<string, PropertyInfo> SlotAssignment { get; set; }






        public void StoreContainerInformation(XElement containerElement, string componentName)
        {
            ComponentName = componentName;

            var entriesGrouped = containerElement.Descendants("Entry")
                .GroupBy(x => x.Element("Slot")?.Value)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var key in entriesGrouped.Keys)
            {
                int counter = 1;

                foreach (var entry in entriesGrouped[key])
                {
                    var tempSignal = new FeeInterfaceSignal
                    {
                        Tag = entry.Element("Signal")?.Value,
                        Path = entry.Element("Address")?.Value?.Contains("GVL_IO") == true
                            ? entry.Element("Address")?.Value
                            : string.Empty,
                        Address = entry.Element("Address")?.Value?.Contains("GVL_IO") == false
                            ? entry.Element("Address")?.Value
                            : string.Empty,
                        Comment = entry.Element("ID")?.Value,
                        IOTypeString = entry.Element("DataType")?.Value
                    };
                    tempSignal.SetIoMode();

                    var slotName = key;
                    var property = SlotAssignment.TryGetValue(slotName, out PropertyInfo? value) ? value : null;

                    if (property == null)
                    {
                        MessageBox.Show($"Slot '{slotName}' in Component '{ComponentName}' does not exist!");
                        continue;
                    }

                    if (property.PropertyType == typeof(FeeInterfaceSignal))
                    {
                        // Multiple entries -> add counter
                        if (entriesGrouped[key].Count > 1)
                        {
                            slotName = string.Concat(slotName, counter);
                            counter++;
                            property = SlotAssignment.TryGetValue(slotName, out PropertyInfo? indexedValue) ? indexedValue : null;
                        }
                        try
                        {
                            property?.SetValue(this, tempSignal);
                        }
                        catch (Exception)
                        {

                            MessageBox.Show($"Property: {property.Name}, DeclaringType: {property.DeclaringType}, TargetType: {this.GetType()}, ");
                        }

                    }
                    else if (property.PropertyType == typeof(List<FeeInterfaceSignal>))
                    {
                        // Add slot to list
                        var list = (List<FeeInterfaceSignal>)property.GetValue(this);
                        if (list == null)
                        {
                            list = new List<FeeInterfaceSignal>();
                            property.SetValue(this, list);
                        }
                        list.Add(tempSignal);
                    }
                }
            }

        }



        protected List<T> FindSimObjectsByNameAndType<T>(ObservableCollection<FeeAbstractObject> mappableSimObjects) where T : FeeAbstractObject, new()
        {
            // Get expected type name from Dictionary
            if (!FeeAbstractObject.TypeToNameMap.TryGetValue(typeof(T), out var expectedTypeName))
            {
                return new List<T>();
            }

            var matches = mappableSimObjects
                                .Where(obj => 
                                    string.Equals(obj.Name, this.ComponentName, StringComparison.OrdinalIgnoreCase) &&
                                    obj.FeeType == expectedTypeName)
                                .OfType<T>()
                                .ToList();

            foreach (var match in matches)
            {
                if (match is IAssignableSimObject assignableSimObject)
                {
                    assignableSimObject.AssignedContainer = (ISimObjectFindOrSelect)this;
                }
            }

            return matches;

        }





        public int CountNonNullSignals()
        {
            var singleSignals = this.GetType()
                                    .GetProperties()
                                    .Where(p => p.PropertyType == typeof(FeeInterfaceSignal))
                                    .Select(s => s.GetValue(this))
                                    .Count(v => v != null);
            var listSignals = this.GetType()
                                  .GetProperties()
                                  .Where(p => p.PropertyType == typeof(List<FeeInterfaceSignal>))
                                  .Select(s => s.GetValue(this) as List<FeeInterfaceSignal>)
                                  .Where(list => list != null)
                                  .Sum(list => list.Count(v => v != null));

            return singleSignals + listSignals;
        }

    }



    public class SimObjectTarget
    {
        public string DisplayName { get; set; }

        public Type AllowedType { get; set; }

        public bool AllowMultiSelect { get; set; }

        public string DisplayNameWithSelection => $"{DisplayName} ({(AllowMultiSelect ? "Multi Select" : "Single Select")})";

        public Action<IEnumerable<FeeAbstractObject>> AssignObjects { get; set; }

        public Func<IEnumerable<FeeAbstractObject>> GetObjects { get; set; }
    }



}
