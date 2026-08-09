using FS.SDK.Components.Label;
using FS.SDK.Mathematics;
using FS.SDK.Scene.Objects;
using System.Xml.Linq;
using VIBN_Tools.ModelValidation;
using static VIBN_Tools.GlobalClasses.Interfaces;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeLabel : FeeAbstractObject, IPlausibilityCheck
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================


        private string _text;
        public string Text
        {
            get => _text;
            set => SetPropertyChange(ref _text, value);
        }

        private float _textScale;
        public float TextScale
        {
            get => _textScale;
            set => SetPropertyChange(ref _textScale, value);
        }

        private bool _enableSlot;
        public bool EnableSlot
        {
            get => _enableSlot;
            set => SetPropertyChange(ref _enableSlot, value);
        }


        private bool _enableFaceCamera;
        public bool EnableFaceCamera
        {
            get => _enableFaceCamera;
            set => SetPropertyChange(ref _enableFaceCamera, value);
        }


        public Vector3 TextPosition { get; set; }
        public Matrix TextRotation { get; set; }
        public Color TextColor { get; set; }
        public Color BackgroundColor { get; set; }




        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeLabel()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(LabelObject);
            Scale = new Vector3(0.1f, 0.1f, 0.1f);
            Visible = false;
            TextColor = Color.FromARGB(255, 0, 0, 0);
            BackgroundColor = Color.FromARGB(0, 0, 0, 0);
        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelComponent.Text), Text, "Label");
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelComponent.Scale), TextScale, "Label");
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelComponent.Offset), TextPosition, "Label");
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelComponent.RotationOffset), TextRotation, "Label");
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelComponent.ForeColor), TextColor, "Label");
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelComponent.BackColor), BackgroundColor, "Label");
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelComponent.FaceCamera), EnableFaceCamera, "Label");
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(LabelObject.EnableSlot), EnableSlot, "Label");


            return true;
        }



        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            var label = xElement.Element("Label");

            Text = (string)label.Element("Text")?.Value ?? string.Empty;
            TextScale = (float?)label.Element("Scale") ?? 0f;

            EnableFaceCamera = (bool?)label.Element("FaceCamera") ?? false;
            EnableSlot = (bool?)xElement.Element("EnableSlot") ?? false;

            TextPosition = ParseVector3(label.Element("Offset"));
            TextRotation = new Matrix(
                    M11: (float?)label.Element("RotationOffset")?.Attribute("M11") ?? 0f,
                    M12: (float?)label.Element("RotationOffset")?.Attribute("M12") ?? 0f,
                    M13: (float?)label.Element("RotationOffset")?.Attribute("M13") ?? 0f,
                    M14: (float?)label.Element("RotationOffset")?.Attribute("M14") ?? 0f,
                    M21: (float?)label.Element("RotationOffset")?.Attribute("M21") ?? 0f,
                    M22: (float?)label.Element("RotationOffset")?.Attribute("M22") ?? 0f,
                    M23: (float?)label.Element("RotationOffset")?.Attribute("M23") ?? 0f,
                    M24: (float?)label.Element("RotationOffset")?.Attribute("M24") ?? 0f,
                    M31: (float?)label.Element("RotationOffset")?.Attribute("M31") ?? 0f,
                    M32: (float?)label.Element("RotationOffset")?.Attribute("M32") ?? 0f,
                    M33: (float?)label.Element("RotationOffset")?.Attribute("M33") ?? 0f,
                    M34: (float?)label.Element("RotationOffset")?.Attribute("M34") ?? 0f,
                    M41: (float?)label.Element("RotationOffset")?.Attribute("M41") ?? 0f,
                    M42: (float?)label.Element("RotationOffset")?.Attribute("M42") ?? 0f,
                    M43: (float?)label.Element("RotationOffset")?.Attribute("M43") ?? 0f,
                    M44: (float?)label.Element("RotationOffset")?.Attribute("M44") ?? 0f
                );

            TextColor = Color.FromARGB(
                    alpha: (int?)label.Element("ForeColor")?.Attribute("A") ?? 0,
                    red: (int?)label.Element("ForeColor")?.Attribute("R") ?? 0,
                    green: (int?)label.Element("ForeColor")?.Attribute("G") ?? 0,
                    blue: (int?)label.Element("ForeColor")?.Attribute("B") ?? 0
                );

            BackgroundColor = Color.FromARGB(
                    alpha: (int?)label.Element("BackColor")?.Attribute("A") ?? 0,
                    red: (int?)label.Element("BackColor")?.Attribute("R") ?? 0,
                    green: (int?)label.Element("BackColor")?.Attribute("G") ?? 0,
                    blue: (int?)label.Element("BackColor")?.Attribute("B") ?? 0
                );
        }

        public override void ApplyBatchData(FeePropertyBatchData data)
        {
            base.ApplyBatchData(data);
        }



        public async Task CheckObjectIssuesAsync(IEnumerable<FeeAbstractObject> newObjects)
        {
            // Check connected slots
            Guid guid;
            var textSlotConnected = Slots.TryGetValue("Text", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (EnableSlot && !textSlotConnected)
                PlausibilityIssues.Add(new PlausibilityIssue($"Text Slot aktiv aber Slot 'Text' nicht verbunden", Severity.Warning));

            if (string.IsNullOrEmpty(Text))
                PlausibilityIssues.Add(new PlausibilityIssue($"Kein Label Text hinterlegt", Severity.Warning));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));


        }
    }
}
