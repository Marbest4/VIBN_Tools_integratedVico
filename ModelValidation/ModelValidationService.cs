using FS.SDK;
using FS.SDK.Components;
using FS.SDK.Scene.Objects;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.ModelValidation
{

    public class ModelValidationService
    {
        private static readonly ObjectChangedRouter _router = new ObjectChangedRouter(new IChangeHandler[]
        {
            new FeeButtonChangeHandler(),
            new FeeDetectionFlagChangeHandler(),
            new FeeFloorChangeHandler(),
            new FeeInserterChangeHandler(),
            new FeeInterfaceChangeHandler(),
            new FeeInterfaceSignalChangeHandler(),
            new FeeJointChangeHandler(),
            new FeeLabelChangeHandler(),
            new FeeLogicChangeHandler(),
            new FeePickAndPlaceChangeHandler(),
            new FeeReadingUnitChangeHandler(),
            new FeeRemoverChangeHandler(),
            new FeeReparenterChangeHandler(),
            new FeeSensorChangeHandler(),
            new FeeSurfaceChangeHandler(),
            new FeeWritingUnitChangeHandler(),

        });

        public static ObjectChangedRouter Router => _router;

    }




    //public class ProjectValidation
    //{
    //    // Check Workpieces
    //    public static async Task<List<FeeDetectionFlag>> CheckWorkpiecesAsync(List<FeeAbstractObject> objects)
    //    {
    //        var workpieces = new ConcurrentBag<FeeDetectionFlag>();

    //        var workpieceFrames = objects.OfType<FeeBasicFrame>().Where(x => x.Name == "Workpieces").ToList();

    //        var allChildrenGuids = new ConcurrentBag<string>();

    //        await Parallel.ForEachAsync(workpieceFrames, async (obj, ct) =>
    //        {
    //            var children = await Services.ApiInstance.Object.GetAllChildrenFromSceneObjectAsync(obj.Guid.ToString());

    //            foreach (var guid in children)
    //            {
    //                var type = await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(SceneObject.Type));
    //                if (type != nameof(BasicFrame))
    //                {
    //                    allChildrenGuids.Add(guid);
    //                }
    //            }
    //        });

    //        await Parallel.ForEachAsync(allChildrenGuids, async (guid, ct) =>
    //        {
    //            var issues = new List<PlausibilityIssue>();

    //            var name = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(SceneObject.Name)));
    //            var mark = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(MarkComponent.Mark), nameof(MarkComponent)));
    //            var type = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(SceneObject.Type)));
    //            var visible = Services.ApiInstance.XmlHelper.ConvertToBool(await Services.ApiInstance.Object.GetPropertyAsync(guid, "IsComponentActive", "Model"));

    //            var modelName = Services.ApiInstance.XmlHelper.ConvertToString(await Services.ApiInstance.Object.GetPropertyAsync(guid, nameof(ModelComponent.ModelName), "Model"));
    //            var model = Services.ApiInstance.Content.ModelContentManager.GetOrLoad(modelName);

    //            var timeout = DateTime.Now + TimeSpan.FromSeconds(1);
    //            while (model == null && DateTime.Now < timeout)
    //            {
    //                Thread.Sleep(50);
    //                model = Services.ApiInstance.Content.ModelContentManager.GetOrLoad(modelName);
    //            }

    //            if (model == null)
    //            {
    //                return;
    //            }


    //            var numberOfColliders = model.Meshes.Select(mesh => mesh.BoundingShapesList.Count).Sum();

    //            if (numberOfColliders > 10)
    //                issues.Add(new PlausibilityIssue($"Anzahl Collider zu hoch", Severity.Warning));

    //            if (numberOfColliders == 0)
    //                issues.Add(new PlausibilityIssue($"Keine Collider vorhanden", Severity.Error));


    //            var result = new FeeDetectionFlag()
    //            {
    //                Name = name,
    //                GuidString = guid,
    //                FeeType = type,
    //                Mark = mark,
    //                Visible = visible,
    //                PlausibilityIssues = issues,

    //                NumberOfColliders = numberOfColliders,
    //            };

    //            workpieces.Add(result);

    //        });

    //        return workpieces.ToList();

    //    }

    //}






    public class ValidationItemsTemplateSelector : DataTemplateSelector
    {
        // Templates
        public DataTemplate AllObjectsTemplate { get; set; }
        public DataTemplate ButtonsTemplate { get; set; }
        public DataTemplate ConveyorsTemplate { get; set; }
        public DataTemplate DetectionsFlagsTemplate { get; set; }
        public DataTemplate InsertersTemplate { get; set; }
        public DataTemplate InterfacesTemplate { get; set; }
        public DataTemplate JointsTemplate { get; set; }
        public DataTemplate LabelsTemplate { get; set; }
        public DataTemplate LogicsTemplate { get; set; }
        public DataTemplate MarksTemplate { get; set; }
        public DataTemplate PickAndPlacersTemplate { get; set; }
        public DataTemplate ReadingWritingUnitsTemplate { get; set; }
        public DataTemplate RemoversTemplate { get; set; }
        public DataTemplate ReparentersTemplate { get; set; }
        public DataTemplate SensorsTemplate { get; set; }
        public DataTemplate SignalsTemplate { get; set; }
        public DataTemplate StoppersTemplate { get; set; }



        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ValidationGroupViewModel group)
            {
                if (group.IsAllObjectsGroup)
                    return AllObjectsTemplate;

                if(group.IsMarksGroup)
                    return MarksTemplate;

                var first = group.Items.FirstOrDefault();
                switch (first)
                {
                    case FeeButton: return ButtonsTemplate;
                    case FeeDetectionFlag: return DetectionsFlagsTemplate;
                    case FeeFloor: return StoppersTemplate;
                    case FeeInserter: return InsertersTemplate;
                    case FeeInterface: return InterfacesTemplate;
                    case FeeInterfaceSignal: return SignalsTemplate;
                    case FeeJoint: return JointsTemplate;
                    case FeeLabel: return LabelsTemplate;
                    case FeeLogic: return LogicsTemplate;
                    case FeePickAndPlace: return PickAndPlacersTemplate;
                    case FeeReadingUnit: return ReadingWritingUnitsTemplate;
                    case FeeWritingUnit: return ReadingWritingUnitsTemplate;
                    case FeeRemover: return RemoversTemplate;
                    case FeeReparenter: return ReparentersTemplate;
                    case FeeSensor: return SensorsTemplate;
                    case FeeSurface: return ConveyorsTemplate;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }




    public class ValidationGroupViewModel : NotifyBase
    {
        public string GroupName { get; set; }
        public ObservableCollection<FeeAbstractObject> Items { get; set; }

        public bool IsAllObjectsGroup { get; set; }
        public bool IsMarksGroup { get; set; }


        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }


        private string _currentFilter;

        public ICollectionView ItemsView { get; set; }

        public bool HasItems => ItemsView?.Cast<object>().Any() == true;








        // Constructor
        public ValidationGroupViewModel(ObservableCollection<FeeAbstractObject> items = null)
        {
            Items = items ?? new ObservableCollection<FeeAbstractObject>();

            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = FilterItems;
        }


        // Methods
        public void ApplyFilter(string filter)
        {
            _currentFilter = filter;

            if (!IsAllObjectsGroup)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ItemsView.Refresh();
                    OnPropertyChanged(nameof(HasItems));
                }), DispatcherPriority.Background);
            }

        }


        private bool FilterItems(object obj)
        {
            if (obj is not FeeAbstractObject item)
                return false;

            if (string.IsNullOrWhiteSpace(_currentFilter))
                return true;

            string f = _currentFilter;

            return (item.Name?.Contains(f, StringComparison.OrdinalIgnoreCase) ?? false)
                || (item.PlausibilityIssues?.Any(x => x.Message?.Contains(f, StringComparison.OrdinalIgnoreCase) == true) ?? false);

        }
    }


    public class PlausibilityIssue : NotifyBase
    {
        public string Message { get; set; }
        public Severity Severity { get; set; } // Error, Warning


        private bool _isAcknowledged;
        public bool IsAcknowledged
        {
            get => _isAcknowledged;
            set => SetPropertyChange(ref _isAcknowledged, value);
        }

        public PlausibilityIssue()
        {
            IsAcknowledged = false;
        }

        public PlausibilityIssue(string message, Severity severity)
        {
            Message = message;
            Severity = severity;
        }


        public new event PropertyChangedEventHandler PropertyChanged;
    }

}
