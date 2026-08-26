using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using FS.SDK;
using FS.SDK.API;
using FS.SDK.Components;
using FS.SDK.Extensibility.Interfaces;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using ReadingUnitPlugin.SO;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeObjectService : IFeeObjectService
    {
        public IReadOnlyList<FeeAbstractObject> AllFeeObjects { get; private set; }

        //public event Action FeeObjectsUpdated;

        public event EventHandler<FeeObjectsUpdatedEventargs> FeeObjectsUpdated;

        // Debounce Timer for user input
        private readonly Dictionary<(object obj, string property), CancellationTokenSource> _debounceUserInput = new Dictionary<(object obj, string property), CancellationTokenSource>();

        private readonly HashSet<FeeAbstractObject> _subscribedObjects = new HashSet<FeeAbstractObject>();

        private readonly List<Task> _debounceTasks = new();


        private bool _isLoadingFeeData = false;




        public async Task GetInitialFeeDataAsync()
        {
            ResetFeeData();

            await UpdateFeeDataAsync();
        }



        //public async Task UpdateFeeDataAsync()
        //{
        //    var startTime = DateTime.Now;

        //    _isLoadingFeeData = true;

        //    // Load all FEE objects
        //    AllFeeObjects = await GetAllFeeObjectsAsync();

        //    // Parent-Mapping
        //    FindAndAssignParents(AllFeeObjects);

        //    // Subscripe to PropertyChanged
        //    SubscribePropertyChanges(AllFeeObjects);

        //    // Plausibility checks of every object
        //    await RunPlausibilityChecks(AllFeeObjects);

        //    _isLoadingFeeData = false;

        //    //==================================================================================
        //    await Task.WhenAll(_debounceTasks);
        //    _debounceTasks.Clear();


        //    var stopTime = DateTime.Now;

        //    // Inform ViewModels
        //    await Task.Yield();       // let UI breath :)

        //    //await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        //    //{
        //    //    FeeObjectsUpdated?.Invoke(this, new FeeObjectsUpdatedEventargs
        //    //    {
        //    //        ElapsedTime = stopTime - startTime,
        //    //    });
        //    //}, System.Windows.Threading.DispatcherPriority.Background);

        //    FeeObjectsUpdated?.Invoke(this, new FeeObjectsUpdatedEventargs
        //    {
        //        ElapsedTime = stopTime - startTime,
        //    });

        //}





        public async Task UpdateFeeDataAsync()
        {
            var startTime = DateTime.Now;            

            _isLoadingFeeData = true;

            // Load all FEE objects
            var oldObjects = AllFeeObjects;
            var newObjects = await GetAllFeeObjectsAsync();

            // Parent-Mapping
            FindAndAssignParents(newObjects);

            // Subscripe to PropertyChanged
            SubscribePropertyChanges(newObjects);

            // Plausibility checks of every object
            await RunPlausibilityChecks(newObjects);

            // Save Acknowledge status
            AllFeeObjects = MergeAcknowledgeInformation(oldObjects, newObjects);

            _isLoadingFeeData = false;

            //==================================================================================
            await Task.WhenAll(_debounceTasks);
            _debounceTasks.Clear();


            var stopTime = DateTime.Now;

            // Inform ViewModels
            await Task.Yield();       // let UI breath :)


            FeeObjectsUpdated?.Invoke(this, new FeeObjectsUpdatedEventargs
            {
                ElapsedTime = stopTime - startTime,
            });

        }





        private async Task<List<FeeAbstractObject>> GetAllFeeObjectsAsync()
        {
            // Create Guid Batch Tasks
            var guidsTask = Services.ApiInstance.Object.GetSceneObjectGuidsAsync();
            var guidsJointsTask = Services.ApiInstance.Object.GetSceneObjectGuidsOfTypeAsync(nameof(MotionJoint));
            var guidsSurfacesTask = Services.ApiInstance.Object.GetSceneObjectGuidsOfTypeAsync(nameof(Surface));
            var guidsPickPlacesTask = Services.ApiInstance.Object.GetSceneObjectGuidsOfTypeAsync(nameof(PickAndPlace));

            var logicDefsTask = Services.ApiInstance.Logic.GetAllAvailableLogicDefinitionsAsync();

            // Start Tasks
            await Task.WhenAll(guidsTask, guidsJointsTask, guidsSurfacesTask, guidsPickPlacesTask);

            string[] stringGuids = (await guidsTask).ToArray();
            Guid[] guids = stringGuids.Select(x => Guid.Parse(x)).ToArray();

            string[] stringGuidsJoints = (await guidsJointsTask).ToArray();
            Guid[] guidsJoints = stringGuidsJoints.Select(x => Guid.Parse(x)).ToArray();

            string[] stringGuidsSurfaces = (await guidsSurfacesTask).ToArray();
            Guid[] guidsSurfaces = stringGuidsSurfaces.Select(x => Guid.Parse(x)).ToArray();

            string[] stringGuidsPickPlaces = (await guidsPickPlacesTask).ToArray();
            Guid[] guidsPickPLaces = stringGuidsPickPlaces.Select(x => Guid.Parse(x)).ToArray();


            // Create Global Batch Tasks
            var xmlTask = Services.ApiInstance.Object.GetSceneObjectsAsXmlAsync(stringGuids);

            // Properties
            var posTask = Services.ApiInstance.Object.GetPropertiesAsync(stringGuids, nameof(SceneObject.Transform.Position), nameof(SceneObject.Transform));
            var rotTask = Services.ApiInstance.Object.GetPropertiesAsync(stringGuids, nameof(SceneObject.Transform.Rotation), nameof(SceneObject.Transform));

            // Start Tasks
            await Task.WhenAll(logicDefsTask, xmlTask, posTask, rotTask);



            // Create Specific Batch Tasks
            var jointManualTask = Services.ApiInstance.Object.GetPropertiesAsync(stringGuidsJoints, nameof(JointControllerComponent.IsManualModeEnabled), "Controller");
            var surfaceManualTask = Services.ApiInstance.Object.GetPropertiesAsync(stringGuidsSurfaces, "IsManualModeEnabled");

            // Slot Values
            var jointPosTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsJoints, Enumerable.Repeat("InValue", guidsJoints.Length).ToArray());
            var jointVelTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsJoints, Enumerable.Repeat("InVelocity", guidsJoints.Length).ToArray());
            var jointTargetTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsJoints, Enumerable.Repeat("InTarget", guidsJoints.Length).ToArray());
            var jointActualTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsJoints, Enumerable.Repeat("OutValue", guidsJoints.Length).ToArray());


            var pickTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsPickPLaces, Enumerable.Repeat("Pick", guidsPickPLaces.Length).ToArray());
            var dropTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsPickPLaces, Enumerable.Repeat("Drop", guidsPickPLaces.Length).ToArray());

            var surfVelXTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsSurfaces, Enumerable.Repeat("InVelocityX", guidsSurfaces.Length).ToArray());
            var surfVelYTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsSurfaces, Enumerable.Repeat("InVelocityY", guidsSurfaces.Length).ToArray());
            var surfVelZTask = Services.ApiInstance.Object.GetSlotValuesAsync(guidsSurfaces, Enumerable.Repeat("InVelocityZ", guidsSurfaces.Length).ToArray());

            // Start Tasks
            await Task.WhenAll(jointManualTask, surfaceManualTask, jointPosTask, jointVelTask, jointTargetTask, jointActualTask, pickTask, dropTask, surfVelXTask, surfVelYTask, surfVelZTask);



            // Store data
            var xmlList = (await xmlTask).ToList();
            var xmlElements = xmlList.Select(XElement.Parse).ToList();

            var positions = (await posTask).Select(x => Services.ApiInstance.XmlHelper.ConvertToVector3(x)).ToList();
            var rotations = (await rotTask).Select(x => Services.ApiInstance.XmlHelper.ConvertToVector3(x)).ToList();

            var allLogicDefinitions = await logicDefsTask;



            // Create Dictionaries for later mapping to global data
            // joint
            var jointManualDict = guidsJoints.Zip(await jointManualTask).ToDictionary(x => x.First, x => Services.ApiInstance.XmlHelper.ConvertToBool(x.Second));
            var jointPosDict = guidsJoints.Zip(await jointPosTask).ToDictionary(x => x.First, x => Convert.ToSingle(x.Second));
            var jointVelDict = guidsJoints.Zip(await jointVelTask).ToDictionary(x => x.First, x => Convert.ToSingle(x.Second));
            var jointTargetDict = guidsJoints.Zip(await jointTargetTask).ToDictionary(x => x.First, x => Convert.ToSingle(x.Second));
            var jointActualDict = guidsJoints.Zip(await jointActualTask).ToDictionary(x => x.First, x => Convert.ToSingle(x.Second));

            // Surface
            var surfaceManualDict = guidsSurfaces.Zip(await surfaceManualTask).ToDictionary(x => x.First, x => Services.ApiInstance.XmlHelper.ConvertToBool(x.Second));
            var surfaceVelXDict = guidsSurfaces.Zip(await surfVelXTask).ToDictionary(x => x.First, x => Convert.ToSingle(x.Second));
            var surfaceVelYDict = guidsSurfaces.Zip(await surfVelYTask).ToDictionary(x => x.First, x => Convert.ToSingle(x.Second));
            var surfaceVelZDict = guidsSurfaces.Zip(await surfVelZTask).ToDictionary(x => x.First, x => Convert.ToSingle(x.Second));

            // Pick and Place
            var pickPlacePickDict = guidsPickPLaces.Zip(await pickTask).ToDictionary(x => x.First, x => Convert.ToBoolean(x.Second));
            var pickPlaceDropDict = guidsPickPLaces.Zip(await dropTask).ToDictionary(x => x.First, x => Convert.ToBoolean(x.Second));


            // List of BatchData
            var batchData = new FeePropertyBatchData[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                var id = guids[i];

                batchData[i] = new FeePropertyBatchData
                {
                    Position = positions[i],
                    Rotation = rotations[i],

                    // Joint
                    JointManualModeactive = jointManualDict.TryGetValue(id, out var isJointManual) ? isJointManual : null,
                    PositionValue = jointPosDict.TryGetValue(id, out var posValue) ? posValue : null,
                    VelocityValue = jointVelDict.TryGetValue(id, out var velValue) ? velValue : null,
                    TargetPositionValue = jointTargetDict.TryGetValue(id, out var targetValue) ? targetValue : null,
                    IsActualPosition = jointActualDict.TryGetValue(id, out var actualValue) ? actualValue : null,

                    // Surface
                    SurfaceManualModeActive = surfaceManualDict.TryGetValue(id, out var isSurfaceManual) ? isSurfaceManual : null,
                    IsActualVelocityX = surfaceVelXDict.TryGetValue(id, out var velXValue) ? velXValue : null,
                    IsActualVelocityY = surfaceVelYDict.TryGetValue(id, out var velYValue) ? velYValue : null,
                    IsActualVelocityZ = surfaceVelZDict.TryGetValue(id, out var velZValue) ? velZValue : null,

                    // Pick and Place
                    IsPick = pickPlacePickDict.TryGetValue(id, out var isPick) ? isPick : null,
                    IsDrop = pickPlaceDropDict.TryGetValue(id, out var isDrop) ? isDrop : null,

                    AllLogicDefinitions = allLogicDefinitions.ToList(),
                };
            }



            var sceneObjects = new FeeAbstractObject[stringGuids.Length];

            await Parallel.ForEachAsync(Enumerable.Range(0, stringGuids.Length), async (i, _) =>
            {
                var guid = stringGuids[i];
                var xElmt = xmlElements[i];

                var name = xElmt.Attribute("Name")?.Value;
                var type = xElmt.Attribute("Type")?.Value ?? xElmt.Name.LocalName;

                var obj = FeeObjectFactory.Create(type, name, guid);
                if (obj == null)
                    return;

                obj.StoreXmlObjectProperties(xElmt, Guid.Parse(guid));
                obj.ApplyBatchData(batchData[i]);

                sceneObjects[i] = obj;
            });

            var result = sceneObjects.Where(o => o != null).ToList();

            // Load Interfaces & Signals
            var interfaces = await FeeInterface.GetAllInterfacesAsync();

            result.AddRange(interfaces);
            return result;

        }





        private List<FeeAbstractObject> MergeAcknowledgeInformation(IReadOnlyList<FeeAbstractObject> oldObjects, List<FeeAbstractObject> newObjects)
        {
            if (oldObjects == null || oldObjects.Count == 0)
                return newObjects;

            var oldLookup = oldObjects.ToDictionary(o => o.Guid);


            foreach (var newObj in newObjects)
            {
                // Skip, if new object is completely new
                if (!oldLookup.TryGetValue(newObj.Guid, out var oldObj))
                    continue;

                // Skip if new object has no issues
                if (newObj.PlausibilityIssues.Count == 0)
                    continue;

                foreach (var newIssue in newObj.PlausibilityIssues)
                {
                    var oldIssue = oldObj.PlausibilityIssues.FirstOrDefault(i => i.Message == newIssue.Message);

                    // Merge only, if issue existed in old object AND was acknowledged
                    if (oldIssue != null && oldIssue.IsAcknowledged)
                        newIssue.IsAcknowledged = true;
                }
            }

            return newObjects;

        }

        







        private void FindAndAssignParents(IEnumerable<FeeAbstractObject> feeObjects)
        {
            var lookup = feeObjects.ToDictionary(x => x.Guid);

            foreach (var obj in feeObjects)
            {
                if (obj.ChildrenGuids == null)
                    continue;

                foreach (var childGuid in obj.ChildrenGuids)
                {
                    if (lookup.TryGetValue(childGuid, out var child))
                    {
                        child.Parent = obj;
                    }
                }
            }

            //AllFeeObjects = feeObjects;
        }



        //private void SubscribePropertyChanges(IEnumerable<FeeAbstractObject> feeObjects)
        //{
        //    // Subscribe to PropertyChanged of every object
        //    foreach (var obj in feeObjects)
        //    {
        //        // Check if already subscribed
        //        if (_subscribedObjects.Contains(obj))
        //            continue;

        //        _subscribedObjects.Add(obj);

        //        PropertyChangedEventManager.AddHandler(obj, OnFeeObjectChanged, string.Empty);

        //        foreach (var issue in obj.PlausibilityIssues)
        //        {
        //            PropertyChangedEventManager.AddHandler(issue, OnFeeObjectChanged, string.Empty);
        //        }

        //        if (obj is FeeInterface iface)
        //        {
        //            foreach (var signal in iface.Signals)
        //            {
        //                PropertyChangedEventManager.AddHandler(signal, OnFeeObjectChanged, string.Empty);
        //            }
        //        }
        //    }
        //}



        private void SubscribePropertyChanges(IEnumerable<FeeAbstractObject> feeObjects)
        {
            // Subscribe to PropertyChanged of every object
            foreach (var obj in feeObjects)
            {
                WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.AddHandler(obj, nameof(obj.PropertyChanged), OnFeeObjectChanged);

                foreach(var issue in obj.PlausibilityIssues)
                {
                    WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.AddHandler(issue, nameof(issue.PropertyChanged), OnFeeObjectChanged);
                }

                if(obj is FeeInterface iface)
                {
                    foreach(var signal in iface.Signals)
                    {
                        WeakEventManager<INotifyPropertyChanged, PropertyChangedEventArgs>.AddHandler(signal, nameof(signal.PropertyChanged), OnFeeObjectChanged);
                    }
                }

            }
        }


        private async void OnFeeObjectChanged(object sender, PropertyChangedEventArgs e)
        {

            // No validation when loading all Fee objects
            if (_isLoadingFeeData) return;


            var task = HandleFeeObjectChangedAsync(sender, e);

            _debounceTasks.Add(task);
            _ = task.ContinueWith(t => _debounceTasks.Remove(t));

            _ = task;

        }

        private async Task HandleFeeObjectChangedAsync(object sender, PropertyChangedEventArgs e)
        {

            var key = (sender, e.PropertyName);

            if (_debounceUserInput.TryGetValue(key, out var existingCts))
                existingCts.Cancel();

            var cts = new CancellationTokenSource();
            _debounceUserInput[key] = cts;

            try
            {
                await Task.Delay(250, cts.Token);

                _debounceUserInput.Remove(key);

                var obj = (FeeAbstractObject)sender;
                await ModelValidationService.Router.HandleChangeAsync(obj, e.PropertyName);
            }
            catch (TaskCanceledException)
            {
                // Debounced
            }
        }




        private async Task RunPlausibilityChecks(IEnumerable<FeeAbstractObject> feeObjects)
        {
            var basicFrames = feeObjects.Where(x => x.FeeType == nameof(FeeBasicFrame)).OfType<FeeBasicFrame>().ToList();

            await Parallel.ForEachAsync(feeObjects, async (obj, token) =>
            {
                // Delete all issues before
                obj.PlausibilityIssues.Clear();

                // Basic Check
                if (obj is IPlausibilityCheck checker)
                {
                    await checker.CheckObjectIssuesAsync(feeObjects);
                }

                // Special Check (BasicFrames involved)
                if (obj is IPlausibilityCheck<List<FeeBasicFrame>> frameChecker)
                {
                    await frameChecker.CheckObjectIssuesAsync(basicFrames);
                }
            });
        }




        private void ResetFeeData()
        {
            AllFeeObjects = Array.Empty<FeeAbstractObject>();
            _subscribedObjects.Clear();

            foreach (var cts in _debounceUserInput.Values)
                cts.Cancel();

            _debounceUserInput.Clear();
            _debounceTasks.Clear();
        }



    }







    public class FeeObjectsUpdatedEventargs : EventArgs
    {
        public TimeSpan ElapsedTime { get; set; }
    }





    public static class FeeObjectFactory
    {
        private static readonly Dictionary<string, Func<string, string, FeeAbstractObject>> _map = new Dictionary<string, Func<string, string, FeeAbstractObject>>()
        {
            { nameof(BasicFrame), (name,guid) => new FeeBasicFrame { Name = name, GuidString = guid } },
            { nameof(Button), (name,guid) => new FeeButton { Name = name, GuidString = guid } },
            { nameof(Decoration), (name,guid) => new FeeDecoration { Name = name, GuidString = guid } },
            { nameof(DetectFlag), (name,guid) => new FeeDetectionFlag { Name = name, GuidString = guid } },
            { nameof(Floor), (name,guid) => new FeeFloor { Name = name, GuidString = guid } },
            { nameof(SequenceInserter), (name,guid) => new FeeInserter { Name = name, GuidString = guid } },
            { nameof(MotionJoint), (name,guid) => new FeeJoint { Name = name, GuidString = guid } },
            { nameof(LabelObject), (name,guid) => new FeeLabel { Name = name, GuidString = guid } },
            { nameof(LogicObject), (name,guid) => new FeeLogic { Name = name, GuidString = guid } },
            { nameof(PickAndPlace), (name,guid) => new FeePickAndPlace { Name = name, GuidString = guid } },
            { nameof(ReadingUnitUdt), (name,guid) => new FeeReadingUnit { Name = name, GuidString = guid } },
            { nameof(Remover), (name,guid) => new FeeRemover { Name = name, GuidString = guid } },
            { nameof(Reparenter), (name,guid) => new FeeReparenter { Name = name, GuidString = guid } },
            { nameof(SegmentedLamp), (name,guid) => new FeeSegmentedLamp { Name = name, GuidString = guid } },
            { nameof(Sensor), (name,guid) => new FeeSensor { Name = name, GuidString = guid } },
            { nameof(SafetySensor), (name,guid) => new FeeSensor { Name = name, GuidString = guid } },
            { nameof(Surface), (name,guid) => new FeeSurface { Name = name, GuidString = guid } },
            { nameof(WritingUnitUdt), (name,guid) => new FeeWritingUnit { Name = name, GuidString = guid } },
            { nameof(KinematicFrame), (name,guid) => new FeeKinematicFrame { Name = name, GuidString = guid } },
        };

        public static FeeAbstractObject Create(string type, string name, string guid)
        {
            if (_map.TryGetValue(type, out var ctor))
            {
                return ctor(name, guid);
            }

            return null;

            ////Fallback with FeeAbstractObject as object
            //return new FeeAbstractObject()
            //{
            //    Name = name,
            //    GuidString = guid,
            //    Type = type,
            //};
        }
    }


    public class FeePropertyBatchData
    {
        // General
        public Vector3? Position { get; set; }
        public Vector3? Rotation { get; set; }

        // Joint
        public bool? JointManualModeactive { get; set; }
        public float? PositionValue { get; set; }
        public float? VelocityValue { get; set; }
        public float? TargetPositionValue { get; set; }
        public float? IsActualPosition { get; set; }

        // Logic
        public List<ApiLogicDefinition> AllLogicDefinitions { get; set; }

        // Surface
        public bool? SurfaceManualModeActive { get; set; }
        public float? IsActualVelocityX { get; set; }
        public float? IsActualVelocityY { get; set; }
        public float? IsActualVelocityZ { get; set; }

        // Pick and Place
        public bool? IsPick { get; set; }
        public bool? IsDrop { get; set; }
    }



}
