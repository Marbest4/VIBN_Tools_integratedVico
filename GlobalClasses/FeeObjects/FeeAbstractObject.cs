using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using FS.SDK;
using FS.SDK.Components;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using VIBN_Tools.ModelValidation;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{

    public class FeeAbstractObject : NotifyBase
    {

        //===================================================================================================================
        // C L A S S   P R O P E R T I E S
        //===================================================================================================================


        private string _guidString;
        public string GuidString
        {
            get => _guidString;
            set
            {
                _guidString = value;
                _guid = Guid.Parse(value);
            }
        }

        private Guid _guid;
        public Guid Guid
        {
            get { return _guid; }
            set
            {
                _guid = value;
                _guidString = value.ToString();
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetPropertyChange(ref _name, value);
        }        

        private bool _visible;
        public bool Visible
        {
            get => _visible;
            set => SetPropertyChange(ref _visible, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get =>  _isSelected;
            set => SetPropertyChange(ref _isSelected, value);
        }



        public List<string> Marks { get; set; } = new List<string>();

        private string _marksString;
        public string MarksString
        {
            get => string.Join(";", Marks);
            set
            {
                Marks = string.IsNullOrWhiteSpace(value)
                    ? new List<string>()
                    : value.Split(";").ToList();

                SetPropertyChange(ref _marksString, value);
            }

        }



        private FeeAbstractObject _parent;
        public FeeAbstractObject Parent
        {
            get => _parent;
            set 
            { 
                if(SetPropertyChange(ref _parent, value))
                {
                    OnParentChanged();
                }
            }
        }

        public List<Guid> ChildrenGuids { get; set; }


        public string FeeType { get; set; }
        public string TypeName => GetType().Name;

        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }


        public Dictionary<string, Guid> Slots { get; set; }


        public List<PlausibilityIssue> PlausibilityIssues { get; set; } = new List<PlausibilityIssue>();


        public bool IsPlausible => PlausibilityIssues?.Count == 0;
        public bool HasIssues => PlausibilityIssues?.Count > 0;
        public int IssueCount => PlausibilityIssues?.Count ?? 0;

        public int IssueSortKey => HasIssues ? -IssueCount : int.MaxValue;


        public bool HasError => PlausibilityIssues.Any(i => i.Severity == Severity.Error && !i.IsAcknowledged);
        public bool HasWarning => PlausibilityIssues.Any(i => i.Severity == Severity.Warning && !i.IsAcknowledged);
        public bool IsAcknowledged => PlausibilityIssues.All(i => i.IsAcknowledged);



        public bool EnableManualControl { get; set; }




        //===================================================================================================================
        // C O N S T R U C T O R
        //===================================================================================================================

        //public FeeAbstractObject()
        //{
        //    PlausibilityIssues = new List<PlausibilityIssue>();
        //}




        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================


        /// <summary>
        /// Create a SceneObject with its properties and returns true
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> CreateAsync()
        {
            // Create object
            Services.ApiInstance.Object.CreateObject(FeeType, Guid);
            // Set Properties
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Name), Name);

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Transform.Position), Position, nameof(SceneObject.Transform));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Transform.Rotation), Rotation, nameof(SceneObject.Transform));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.Transform.LocalScale), Scale, nameof(SceneObject.Transform));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(SceneObject.MarkComponent.Mark), MarksString, nameof(SceneObject.MarkComponent));

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(ModelComponent.IsComponentActive), Visible, "Model");

            return true;
        }


        public async virtual Task<bool> SendAndWaitAsync()
        {
            Services.ApiInstance.Object.Send(Guid);
            if (await Services.ApiInstance.Object.WaitForSceneObjectAsync(Guid.ToString()))
            {
                if (Parent != null)
                {
                    await Services.ApiInstance.Object.AddChildToParentAsync(Parent.Guid, Guid);
                }
                return true;
            }
            await SendAndWaitAsync();
            return false;
        }





        public bool SetMark(string newMark)
        {
            // Create object
            Services.ApiInstance.Object.CreateObject(FeeType, Guid);
            // Set Properties
            Services.ApiInstance.Object.SetProperty(Guid, nameof(SceneObject.MarkComponent.Mark), newMark, nameof(SceneObject.MarkComponent));

            Services.ApiInstance.Object.Send(Guid);

            return true;
        }





        public virtual void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            GuidString = xElement.Attribute("Guid")?.Value;
            Name = xElement.Attribute("Name")?.Value;
            FeeType = xElement.Attribute("Type")?.Value ?? xElement.Name.LocalName;

            Scale = ParseVector3(xElement.Element("Transform")?.Element("LocalScale"));

            Visible = bool.Parse(xElement.Element("Model")?.Element("IsComponentActive")?.Value ?? "false");
            MarksString = xElement.Element("MarkComponent")?.Element("Mark")?.Value;

            var slots = xElement.Element("Slots") ?? xElement.Element("IOSlots");
            Slots = slots != null
                ? slots?.Descendants("Assignment")
                    .ToDictionary(
                        a => (string)a.Element("SlotName") ?? string.Empty,
                        a => Guid.Parse((string)a.Element("AssignedGuid") ?? Guid.Empty.ToString())
                    )
                : null;


            ChildrenGuids = xElement.Element("Children")?.Elements().Select(e => Guid.Parse((string)e.Attribute("Guid").Value)).ToList() ?? new List<Guid>();

        }

        public virtual void ApplyBatchData(FeePropertyBatchData data)
        {
            if (data.Position is Vector3 position)
                Position = position;

            if (data.Rotation is Vector3 rotation)
                Rotation = rotation;
        }




        protected Vector3 ParseVector3(XElement el)
        {
            if (el == null) return Vector3.Zero;
            return new Vector3(
                float.Parse(el.Attribute("X")?.Value ?? "0"),
                float.Parse(el.Attribute("Y")?.Value ?? "0"),
                float.Parse(el.Attribute("Z")?.Value ?? "0")
            );
        }


        public void NotifyIssueStateChanged()
        {
            OnPropertyChanged(nameof(HasError));
            OnPropertyChanged(nameof(HasWarning));
            OnPropertyChanged(nameof(IsAcknowledged));
            OnPropertyChanged(nameof(IsPlausible));
        }

        protected virtual void OnParentChanged() { }










        //===================================================================================================================
        // E V E N T S
        //===================================================================================================================







        //===================================================================================================================
        // A D D I T I O N A L S :   C O N S T A N T S ,   D E F I N E S ,   E T C .
        //===================================================================================================================





        /// <summary>
        /// Mapping Dictionaries to map FeeAbstractObject type to corresponding FEE SceneObject name and vise versa
        /// </summary>
        public static readonly Dictionary<Type, string> TypeToNameMap = new Dictionary<Type, string>()
        {
            { typeof(FeeSurface), nameof(Surface) },
            { typeof(FeeJoint), nameof(MotionJoint) },
            { typeof(FeeSensor), nameof(SafetySensor) },
            { typeof(FeeFloor), nameof(Floor) },
            { typeof(FeePickAndPlace), nameof(PickAndPlace) },
        };

        public static readonly Dictionary<string, Type> NameToTypeMap = new Dictionary<string, Type>()
        {
            { nameof(Surface), typeof(FeeSurface)},
            { nameof(MotionJoint), typeof(FeeJoint) },
            { nameof(SafetySensor), typeof(FeeSensor) },
            { nameof(Floor), typeof(FeeFloor) },
            { nameof(PickAndPlace), typeof(FeePickAndPlace) },
        };


    }


}
