using System.Collections.ObjectModel;
using System.Windows;
using System.Xml.Linq;
using FS.SDK;
using FS.SDK.Scene.Objects;
using VIBN_Tools.ContainerToFee.General;
using VIBN_Tools.ContainerToFee.GrobStandard;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;
using static VIBN_Tools.GlobalClasses.Interfaces;



namespace VIBN_Tools.ContainerToFee
{
    public class ContainerToFeeService
    {

        private static readonly Dictionary<Type, IContainerFactory> _factories;
        private static readonly CabinetContainerManager _cabinetContainerManager;

        static ContainerToFeeService()
        {
            _cabinetContainerManager = new CabinetContainerManager();

            _factories = new Dictionary<Type, IContainerFactory>
            {
                {typeof(GrobBeltControl_Container), new LogicContainerFactory() },
                {typeof(GrobClamping_Container), new LogicContainerFactory() },
                {typeof(GrobConveyor_Container), new LogicSimObjectContainerFactory() },
                {typeof(GrobCylinder_Container), new LogicSimObjectContainerFactory() },
                {typeof(GrobGripperBasic_Container), new LogicSimObjectContainerFactory() },
                {typeof(GrobGripperVacuum_Container), new LogicSimObjectContainerFactory() },
                {typeof(GrobLiftUnit_Container), new LogicSimObjectContainerFactory() },
                {typeof(GrobPneumaticSupply_Container), new LogicContainerFactory() },
                {typeof(GrobSafetyDoor_Container), new LogicContainerFactory() },
                {typeof(GrobSensor_Container), new LogicSimObjectContainerFactory() },
                {typeof(GrobStop_Container), new LogicSimObjectContainerFactory() },

                {typeof(Button_Container), new SimObjectContainerFactory() },
                {typeof(Stacklight_Container), new SimObjectContainerFactory() },
                {typeof(SimpleMove_Container), new SimObjectContainerFactory() },
                {typeof(SimpleNot_Container), new SimObjectContainerFactory() },

                {typeof(CabinetSwitch_Container), new CabinetElementContainerFactory(_cabinetContainerManager) },
                {typeof(CabinetFuse_Container), new CabinetElementContainerFactory(_cabinetContainerManager) },
                {typeof(CabinetEStop_Container), new CabinetElementContainerFactory(_cabinetContainerManager) },
                {typeof(CabinetLamp_Container), new CabinetElementContainerFactory(_cabinetContainerManager) },
            };
        }




        public static async Task CreateAllContainersAsync(IEnumerable<ContainerBaseClass> containers, FeeInterface targetInterface, FeeAbstractObject parentObject)
        {
            // Split all containers into cabinet containers (no parallel generation) and other containers (parallel generation)
            var cabinetContainers = containers
                .OfType<ICabinetElementOwner>()
                .Cast<ContainerBaseClass>()
                .ToList();

            var otherContainers = containers
                .Except(cabinetContainers)
                .ToList();

            await Parallel.ForEachAsync(otherContainers, async (container, ct) =>
            {
                if (_factories.TryGetValue(container.GetType(), out var factory))
                {
                    await factory.CreateContainerAsync(container, targetInterface, parentObject);
                }
            });


            foreach (var container in cabinetContainers)
            {
                if (_factories.TryGetValue(container.GetType(), out var factory))
                {
                    await factory.CreateContainerAsync(container, targetInterface, parentObject);
                }
            }

            // non parallel stable version
            //foreach (var container in containers)
            //{
            //    if (_factories.TryGetValue(container.GetType(), out var factory))
            //    {
            //        await factory.CreateContainerAsync(container, targetInterface, parentObject);
            //    }
            //}
        }





        public static (List<ContainerBaseClass>, List<FeeInterfaceSignal>) ReadInContainerXmlData(string fileName)
        {
            var listContainerData = new List<ContainerBaseClass>();
            var listUnknownSignals = new List<FeeInterfaceSignal>();


            XDocument containerXml = XDocument.Load(fileName);
            var containers = containerXml.Descendants("Container").ToList();

            foreach (var el in containers)
            {
                string componentName = el.Element("Component")?.Value;
                string type = el.Element("Type")?.Value;

                if (TryCreateContainer(el, type, componentName, out var container))
                {
                    listContainerData.Add(container);
                }
                else
                {
                    // Unknown types -> Convert Entries to FeeInterfaceSignals
                    foreach (var entry in el.Descendants("Entry"))
                    {
                        var signal = new FeeInterfaceSignal
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
                        signal.SetIoMode();
                        listUnknownSignals.Add(signal);
                    }
                }
            }

            return (listContainerData, listUnknownSignals);

        }



        private static bool TryCreateContainer(XElement xmlData, string type, string componentName, out ContainerBaseClass container)
        {
            container = type switch
            {
                "BeltControl" => new GrobBeltControl_Container(),
                "Button" => new Button_Container(),
                "Clamping" => new GrobClamping_Container(),
                "Conveyor" => new GrobConveyor_Container(),
                "Cylinder" or "FeedSafetyDoor" => new GrobCylinder_Container(),
                "GripperBasic" => new GrobGripperBasic_Container(),
                "GripperVacuum" => new GrobGripperVacuum_Container(),
                "LiftUnit" => new GrobLiftUnit_Container(),
                "PneumaticSupply" => new GrobPneumaticSupply_Container()  ,
                "ReturnCircuit" or "SafeArea" => new SimpleNot_Container(),
                "SafetyDoor" => new GrobSafetyDoor_Container(),
                "Sensor" => new GrobSensor_Container(),
                "Stacklight" => new Stacklight_Container(),
                "Stop" => new GrobStop_Container(),

                "CabinetLamp" => new CabinetLamp_Container(),
                "EStop" => new CabinetEStop_Container(),
                "Fuse" => new CabinetFuse_Container(),
                "Switch" => new CabinetSwitch_Container(),
                _ => null
            };

            if (container != null)
            {
                container.StoreContainerInformation(xmlData, componentName);
                return true;
            }

            return false;
        }


        public static void LinkAddonContainers(IEnumerable<ContainerBaseClass> containers)
        {
            var grippers = containers.OfType<GrobGripperBasic_Container>().ToDictionary(x => x.ComponentName);

            foreach (var container in containers)
            {
                if (container is not IAddonContainer addon)
                    continue;

                if (!grippers.TryGetValue(container.ComponentName, out var parent))
                {
                    MessageBox.Show($"No GripperBasic container found for Addon '{container.ComponentName}'");

                    continue;
                }

                addon.ParentContainer = parent;
                parent.Addons.Add(addon);
            }

        }


        public async static Task<ObservableCollection<FeeAbstractObject>> GetSimObjectsFromSimultionAsync()
        {
            var myCollection = new ObservableCollection<FeeAbstractObject>();

            var objectTypes = new[] { nameof(MotionJoint), nameof(Surface), nameof(SafetySensor), nameof(Sensor), nameof(Floor), nameof(PickAndPlace), nameof(SegmentedLamp) };

            foreach (var type in objectTypes)
            {

                var guids = await Services.ApiInstance.Object.GetSceneObjectGuidsOfTypeAsync(type);
                var names = await Services.ApiInstance.Object.GetPropertiesAsync(guids.ToArray(), nameof(SceneObject.Name)).ContinueWith(t => t.Result.Select(Services.ApiInstance.XmlHelper.ConvertToString));
                var types = await Services.ApiInstance.Object.GetPropertiesAsync(guids.ToArray(), nameof(SceneObject.Type));

                var simObjects = guids
                    .Zip(names, (guid, name) => new { guid, name })
                    .Zip(types, (guidname, type) =>
                    {
                        return FeeObjectFactory.Create(type, guidname.name, guidname.guid);
                    });

                myCollection = new ObservableCollection<FeeAbstractObject>(myCollection.Concat(simObjects));
            }

            return myCollection;
        }



    }





    public class ContainerSnapshot
    {
        public int SelectionStepIndex { get; set; }
        public ISimObjectFindOrSelect Container { get; set; }
        public SimObjectTarget Target { get; set; }
        public List<FeeAbstractObject> AssignedSimObjects { get; set; }
        public bool IsCreationRequested { get; set; }

    }

    public class SimObjectSelectionStep
    {
        public ISimObjectFindOrSelect Container { get; set; }

        public SimObjectTarget Target { get; set; }
    }




}
