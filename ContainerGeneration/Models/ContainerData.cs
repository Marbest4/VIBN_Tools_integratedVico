using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
using VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData;
using VIBN_Tools.ContainerGeneration.BusinessLogic.RequirementsXml;

namespace VIBN_Tools.ContainerGeneration.Models
{
    /// <summary>
    /// Represents container data with validation and property change notification for ViewModels.
    /// Inherits from <see cref="ComponentContainer"/> and implements <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    public class ContainerData : ComponentContainer, INotifyPropertyChanged
    {
        private bool _isValid;

        private string _validationError = string.Empty;

        private bool _manuallyChecked;
        private string _reimportStatusText = string.Empty;
        private string _reimportDetails = string.Empty;
        private string _id = string.Empty;
        private string _component = string.Empty;


        /// <summary>
        /// Gets or sets the list of slots.
        /// </summary>
        [XmlElement]
        public ObservableCollection<string> Slots { get; set; } = [];



        private string _type = string.Empty;

        [XmlAttribute("id")]
        public new string Id
        {
            get => _id;
            set => SetWorkspaceValue(ref _id, value);
        }

        [XmlElement("Component")]
        public new string Component
        {
            get => _component;
            set => SetWorkspaceValue(ref _component, value);
        }

        [XmlElement("Type")]
        public new string Type
        {
            get => _type;
            set
            {
                value ??= string.Empty;
                if (string.Equals(_type, value, StringComparison.Ordinal))
                    return;

                WorkspaceValueChanging?.Invoke(
                    this,
                    new WorkspaceValueChangingEventArgs(nameof(Type), _type, value));
                _type = value;
                NotifyOfPropertyChange(nameof(Type));
                UpdateTypeDependencies();
                NotifyReviewProperties();
            }
        }

        [XmlIgnore]






        /// <summary>
        /// Gets or sets a value indicating whether the container data is valid.
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (_isValid == value)
                    return;

                _isValid = value;
                NotifyOfPropertyChange(nameof(IsValid));
                NotifyReviewProperties();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the container was manually checked.
        /// </summary>
        public bool ManuallyChecked
        {
            get => _manuallyChecked;
            set
            {
                if (_manuallyChecked == value)
                    return;

                WorkspaceValueChanging?.Invoke(
                    this,
                    new WorkspaceValueChangingEventArgs(
                        nameof(ManuallyChecked),
                        _manuallyChecked,
                        value));
                _manuallyChecked = value;
                NotifyOfPropertyChange(nameof(ManuallyChecked));
                NotifyReviewProperties();
            }
        }

        [XmlIgnore]
        public bool HasDetectedChanges =>
            DataList.Any(entry =>
                entry.ReviewState is not ContainerEntryReviewState.None and
                    not ContainerEntryReviewState.Preserved);

        [XmlIgnore]
        public bool RequiresReview =>
            !ManuallyChecked &&
            (!IsValid ||
             DataList.Any(entry =>
                 entry.ReviewState is ContainerEntryReviewState.NeedsReview or
                     ContainerEntryReviewState.NewFromSource or
                     ContainerEntryReviewState.NewlyRecognized or
                     ContainerEntryReviewState.SourceChanged or
                     ContainerEntryReviewState.ManuallyEdited));

        [XmlIgnore]
        public string ReimportStatusText
        {
            get => _reimportStatusText;
            private set
            {
                if (_reimportStatusText == value)
                    return;

                _reimportStatusText = value;
                NotifyOfPropertyChange(nameof(ReimportStatusText));
            }
        }

        [XmlIgnore]
        public string ReimportDetails
        {
            get => _reimportDetails;
            private set
            {
                if (_reimportDetails == value)
                    return;

                _reimportDetails = value;
                NotifyOfPropertyChange(nameof(ReimportDetails));
            }
        }


        /// <summary>
        /// Gets or sets the validation error text.
        /// </summary>
        [XmlIgnore]
        public string ValidationError
        {
            get => _validationError;
            set
            {
                _validationError = value;
                NotifyOfPropertyChange(nameof(ValidationError));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerData"/> class.
        /// </summary>
        public ContainerData() : base()
        {
            DataList.CollectionChanged += DataList_CollectionChanged;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerData"/> class with the specified component container.
        /// </summary>
        /// <param name="container">The component container to initialize from.</param>
        public ContainerData(ComponentContainer container) : base()
        {
            Id = container.Id;
            Component = container.Component;
            Type = container.Type;
            DataList = new ObservableCollection<ContainerEntry>(container.DataList);

            foreach (var entry in DataList)
            {
                entry.SlotChanged += Entry_SlotChanged;
                entry.PropertyChanged += Entry_PropertyChanged;
            }

            DataList.CollectionChanged += DataList_CollectionChanged;
            RefreshReimportStatus();
        }

        /// <summary>
        /// Validates the container data.
        /// This method checks for empty or duplicate key slots and updates the <see cref="IsValid"/> property.
        /// </summary>
        public void Validate()
        {
            bool TempValid = true;
            StringBuilder ErrorBuilder = new StringBuilder();
            // check for empty or duplicate key slots
            if (DataList.Count == 0)
            {
                TempValid = false;
                ErrorBuilder.Append("No signals attached." + Environment.NewLine);
            }
            else
            {
                if (!DataList.All(entry => !string.IsNullOrWhiteSpace(entry.Slot)))
                {
                    TempValid = false;
                    ErrorBuilder.Append("Slots need filled out." + Environment.NewLine);
                }
                else if (DataList.GroupBy(item => item.Slot).Where(group => group.Count() > 1).ToList().Count != 0)
                {
                    TempValid = false;
                    ErrorBuilder.Append("Duplicate slots found." + Environment.NewLine);
                }
                if (!(MaxSignals == null || DataList.Count <= MaxSignals))
                {
                    TempValid = false;
                    ErrorBuilder.Append("Too many signals attached (Max " + MaxSignals + ")." + Environment.NewLine);
                }
                if (!(MinSignals == null || DataList.Count >= MinSignals))
                {
                    TempValid = false;
                    ErrorBuilder.Append("Not enough Signals attached (Min " + MinSignals + ")." + Environment.NewLine);
                }
            }


            IsValid = TempValid;
            ValidationError = ErrorBuilder.ToString().Trim();
        }

        /// <summary>
        /// Adds a container entry and validates the container data.
        /// </summary>
        /// <param name="entry">The container entry to add.</param>
        public void AddEntry(ContainerEntry entry)
        {
            DataList.Add(entry);
            Validate();
        }

        /// <summary>
        /// Removes a container entry and validates the container data.
        /// </summary>
        /// <param name="entry">The container entry to remove.</param>
        public void RemoveEntry(ContainerEntry entry)
        {
            DataList.Remove(entry);
            Validate();
        }

        /// <summary>
        /// Converts a list of component containers to a list of container data.
        /// </summary>
        /// <param name="dataList">The list of component containers.</param>
        /// <returns>A list of container data.</returns
        public static List<ContainerData> FromList(List<ComponentContainer> dataList)
        {
            return dataList.Select(item => new ContainerData(item)).ToList();
        }

        /// <summary>
        /// Converts a list of container data to a list of component containers.
        /// </summary>
        /// <param name="dataList">The list of container data.</param>
        /// <returns>A list of component containers.</returns
        public static List<ComponentContainer> ToComponentContainerList(IList<ContainerData> dataList)
        {
            return dataList.Select(item => new ComponentContainer
            {
                Id = item.Id,
                Component = item.Component,
                Type = item.Type,
                DataList = new ObservableCollection<ContainerEntry>(item.DataList)
            }).ToList();
        }

        /// <summary>
        /// Event which occurs when a property value changes.
        /// </summary
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<WorkspaceValueChangingEventArgs>? WorkspaceValueChanging;

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void NotifyOfPropertyChange(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        private void DataList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (ContainerEntry entry in e.NewItems)
                {
                    entry.SlotChanged += Entry_SlotChanged;
                    entry.PropertyChanged += Entry_PropertyChanged;
                }

            if (e.OldItems != null)
                foreach (ContainerEntry entry in e.OldItems)
                {
                    entry.SlotChanged -= Entry_SlotChanged;
                    entry.PropertyChanged -= Entry_PropertyChanged;
                }

            Validate();
            RefreshReimportStatus();
            NotifyReviewProperties();
        }

        private void Entry_SlotChanged(object? sender, EventArgs e)
        {
            Validate();
            RefreshReimportStatus();
            NotifyReviewProperties();
        }

        private void Entry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ContainerEntry.ReviewState) or
                nameof(ContainerEntry.ReviewMessage) or
                nameof(ContainerEntry.IsManuallyEdited) or
                nameof(ContainerEntry.Signal))
            {
                RefreshReimportStatus();
                NotifyReviewProperties();
            }
        }

        public void RefreshReimportStatus()
        {
            var states = DataList
                .Select(entry => entry.ReviewStateText)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            ReimportStatusText = states.Count switch
            {
                0 => string.Empty,
                1 => states[0],
                _ => "Gemischt"
            };

            ReimportDetails = string.Join(
                Environment.NewLine,
                DataList
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.ReviewMessage))
                    .Select(entry =>
                        $"{(!string.IsNullOrWhiteSpace(entry.ID) ? entry.ID : entry.Signal)}: {entry.ReviewMessage}")
                    .Distinct(StringComparer.Ordinal));

            if (!IsValid && !string.IsNullOrWhiteSpace(ValidationError))
            {
                ReimportDetails = string.IsNullOrWhiteSpace(ReimportDetails)
                    ? ValidationError
                    : ValidationError + Environment.NewLine + ReimportDetails;
            }

            NotifyReviewProperties();
        }


        private void UpdateTypeDependencies()
        {
            var req = RequirementsProvider.RequirementsFile;

            if (req == null)
                return;

            // Slots aktualisieren
            Slots.Clear();
            foreach (var slot in req.GetSlotNames(Type))
                Slots.Add(slot);

            // Min/Max aktualisieren
            MinSignals = req.GetMinSignals(Type);
            MaxSignals = req.GetMaxSignals(Type);

            // Validierung ausführen
            Validate();
        }

        private void SetWorkspaceValue(
            ref string field,
            string? value,
            [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            value ??= string.Empty;
            if (string.Equals(field, value, StringComparison.Ordinal))
                return;

            WorkspaceValueChanging?.Invoke(
                this,
                new WorkspaceValueChangingEventArgs(propertyName, field, value));
            field = value;
            NotifyOfPropertyChange(propertyName);
            NotifyReviewProperties();
        }

        private void NotifyReviewProperties()
        {
            NotifyOfPropertyChange(nameof(HasDetectedChanges));
            NotifyOfPropertyChange(nameof(RequiresReview));
        }


    }
}
