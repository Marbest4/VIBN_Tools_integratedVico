using System.Reflection;
using System.Xml.Serialization;
using System.ComponentModel;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.ContainerGeneration.BusinessLogic.ContainerData
{
    /// <summary>
    /// Represents a container entry. Marks properties relevant for XML export using serialization.
    /// </summary>
    public class ContainerEntry : NotifyBase
    {
        private string _id = string.Empty;
        private string _address = string.Empty;
        private string _dataType = string.Empty;
        private string _note = string.Empty;
        private string _signalId = string.Empty;

        /// <summary>
        /// Stable identity of this signal inside a VIBN Tools workspace.
        /// It survives edits, undo/redo, save/load and reimports, but is not
        /// written to the production container XML.
        /// </summary>
        [XmlIgnore]
        [DisplayName("Signal-ID")]
        [ReadOnly(true)]
        public string SignalId
        {
            get => _signalId;
            set
            {
                value ??= string.Empty;
                if (string.Equals(_signalId, value, StringComparison.Ordinal))
                    return;

                _signalId = value;
                OnPropertyChanged();
            }
        }

        public string EnsureSignalId()
        {
            if (string.IsNullOrWhiteSpace(SignalId))
                SignalId = $"SIG-{Guid.NewGuid():N}";

            return SignalId;
        }

        /// <summary>
        /// Gets or sets the ID. Serializes to an element.
        /// </summary>
        [XmlElement("ID")]
        public string ID
        {
            get => _id;
            set => SetWorkspaceValue(ref _id, value);
        }

        /// <summary>
        /// Gets or sets the address. Serializes to an element.
        /// </summary>
        [XmlElement("Address")]
        public string Address
        {
            get => _address;
            set => SetWorkspaceValue(ref _address, value);
        }

        /// <summary>
        /// Gets or sets the data type. Serializes to an element.
        /// </summary>
        [XmlElement("DataType")]
        public string DataType
        {
            get => _dataType;
            set => SetWorkspaceValue(ref _dataType, value);
        }

        /// <summary>
        /// Gets or sets the signal. Serializes to an element.
        /// </summary
        private string _signal = string.Empty;

        [XmlElement("Signal")]
        public string Signal
        {
            get => _signal;
            set
            {
                value ??= string.Empty;
                if (string.Equals(_signal, value, StringComparison.Ordinal))
                    return;

                var previousValue = _signal;
                WorkspaceValueChanging?.Invoke(
                    this,
                    new WorkspaceValueChangingEventArgs(
                        nameof(Signal),
                        previousValue,
                        value));
                _signal = value;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(previousValue) &&
                    string.IsNullOrWhiteSpace(value))
                {
                    SignalCleared?.Invoke(
                        this,
                        new SignalClearedEventArgs(previousValue));
                }
            }
        }

        /// <summary>
        /// Gets or sets the slot. Serializes to an element.
        /// </summary
        private string _slot = string.Empty;

        [XmlElement("Slot")]
        public string Slot
        {
            get => _slot;
            set
            {
                value ??= string.Empty;
                if (string.Equals(_slot, value, StringComparison.Ordinal))
                    return;

                WorkspaceValueChanging?.Invoke(
                    this,
                    new WorkspaceValueChangingEventArgs(
                        nameof(Slot),
                        _slot,
                        value));
                _slot = value;
                OnPropertyChanged();

                // Tell ContainerData that slot has been changed
                SlotChanged?.Invoke(this, EventArgs.Empty);
            }
        }


        /// <summary>
        /// Gets or sets the note. Serializes to an element.
        /// </summary
        [XmlElement("Note")]
        public string Note
        {
            get => _note;
            set => SetWorkspaceValue(ref _note, value);
        }

        private ContainerEntryReviewState _reviewState;
        private string _reviewMessage = string.Empty;
        private bool _isManuallyEdited;

        /// <summary>
        /// Runtime-only provenance used by the safe reimport workflow.
        /// It is intentionally excluded from the production container XML.
        /// </summary>
        [XmlIgnore]
        public ContainerEntryReviewState ReviewState
        {
            get => _reviewState;
            set
            {
                if (SetPropertyChange(ref _reviewState, value))
                    OnPropertyChanged(nameof(ReviewStateText));
            }
        }

        [XmlIgnore]
        [DisplayName("Reimport-Änderung")]
        [ReadOnly(true)]
        public string ReviewMessage
        {
            get => _reviewMessage;
            set => SetPropertyChange(ref _reviewMessage, value);
        }

        [XmlIgnore]
        public bool IsManuallyEdited
        {
            get => _isManuallyEdited;
            set => SetPropertyChange(ref _isManuallyEdited, value);
        }

        [XmlIgnore]
        public string ReviewStateText => ReviewState switch
        {
            ContainerEntryReviewState.ManuallyEdited => "Manuell geändert",
            ContainerEntryReviewState.Preserved => "Übernommen",
            ContainerEntryReviewState.NewlyRecognized => "Neu erkannt",
            ContainerEntryReviewState.NewFromSource => "Neu",
            ContainerEntryReviewState.SourceChanged => "Geändert",
            ContainerEntryReviewState.NeedsReview => "Prüfen",
            _ => string.Empty
        };


        /// <summary>
        /// Creates a clone of the current container entry.
        /// </summary>
        /// <returns>A new <see cref="ContainerEntry"/> instance with the same property values.</returns>
        public ContainerEntry Clone()
        {
            ContainerEntry clone = new ContainerEntry();
            clone.SignalId = EnsureSignalId();
            clone.ID = this.ID;
            clone.Address = this.Address;
            clone.DataType = this.DataType;
            clone.Signal = this.Signal;
            clone.Slot = this.Slot;
            clone.Note = this.Note;
            clone.ReviewState = this.ReviewState;
            clone.ReviewMessage = this.ReviewMessage;
            clone.IsManuallyEdited = this.IsManuallyEdited;

            return clone;
        }

        /// <summary>
        /// Determines whether the container entry is empty.
        /// </summary>
        /// <returns><c>true</c> if all string properties are empty; otherwise, <c>false</c>.</returns
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(ID) &&
                   string.IsNullOrEmpty(Address) &&
                   string.IsNullOrEmpty(DataType) &&
                   string.IsNullOrEmpty(Signal) &&
                   string.IsNullOrEmpty(Slot) &&
                   string.IsNullOrEmpty(Note);
        }



        public event EventHandler? SlotChanged;
        public event EventHandler<SignalClearedEventArgs>? SignalCleared;
        public event EventHandler<WorkspaceValueChangingEventArgs>? WorkspaceValueChanging;

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
            OnPropertyChanged(propertyName);
        }
    }

    public sealed class SignalClearedEventArgs : EventArgs
    {
        public SignalClearedEventArgs(string previousSignal)
        {
            PreviousSignal = previousSignal;
        }

        public string PreviousSignal { get; }
    }

    public sealed class WorkspaceValueChangingEventArgs : EventArgs
    {
        public WorkspaceValueChangingEventArgs(
            string propertyName,
            object? previousValue,
            object? newValue)
        {
            PropertyName = propertyName;
            PreviousValue = previousValue;
            NewValue = newValue;
        }

        public string PropertyName { get; }
        public object? PreviousValue { get; }
        public object? NewValue { get; }
    }
}
