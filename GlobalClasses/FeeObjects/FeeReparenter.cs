using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Xml.Linq;
using FS.SDK.Components;
using FS.SDK.Scene.Objects;
using FS.SDK.SceneObjectCommands;
using FS.SDK.Utilities;
using VIBN_Tools.ModelValidation;

namespace VIBN_Tools.GlobalClasses.FeeObjects
{
    public class FeeReparenter : FeeAbstractObject
    {

        //===================================================================================================================
        // C L A S S   S P E C I F I C   P R O P E R T I E S
        //===================================================================================================================

        private float _reparentRange;
        public float ReparentRange
        {
            get => _reparentRange;
            set => SetPropertyChange(ref _reparentRange, value);
        }

        private string _childrenMark;
        public string ChildrenMark
        {
            get => _childrenMark;
            set => SetPropertyChange(ref _childrenMark, value);
        }

        private string _parentMark;
        public string ParentMark
        {
            get => _parentMark;
            set => SetPropertyChange(ref _parentMark, value);
        }

        private ReparentMode _reparentMode;
        public ReparentMode ReparentMode
        {
            get => _reparentMode;
            set => SetPropertyChange(ref _reparentMode, value);
        }

        private bool _isReparenting;
        public bool IsReparenting
        {
            get => _isReparenting;
            set => SetPropertyChange(ref _isReparenting, value);
        }





        //===================================================================================================================
        // C O N S T R U C T O R S
        //===================================================================================================================

        public FeeReparenter()
        {
            Guid = Guid.NewGuid();
            FeeType = nameof(Reparenter);
            Visible = true;

        }



        //===================================================================================================================
        // M E T H O D S
        //===================================================================================================================

        public override async Task<bool> CreateAsync()
        {
            await base.CreateAsync();

            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(ReparenterComponent.ChildrenMark), ChildrenMark, nameof(ReparenterComponent));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(ReparenterComponent.ParentMark), ParentMark, nameof(ReparenterComponent));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(ReparenterComponent.Range), ReparentRange, nameof(ReparenterComponent));
            await Services.ApiInstance.Object.SetPropertyAsync(Guid, nameof(ReparenterComponent.ReparentMode), ReparentMode, nameof(ReparenterComponent));

            return true;
        }


        public override void StoreXmlObjectProperties(XElement xElement, Guid guid)
        {
            base.StoreXmlObjectProperties(xElement, guid);

            var reparentComponent = xElement.Element("ReparenterComponent");

            ReparentRange = (float?)reparentComponent.Element("Range") ?? 1f;
            ChildrenMark = (string?)reparentComponent.Element("ChildrenMark") ?? string.Empty;
            ParentMark = (string?)reparentComponent.Element("ParentMark") ?? string.Empty;

            ReparentMode = Enum.TryParse<ReparentMode>((string)reparentComponent.Element("ReparentMode") ?? string.Empty, out var mode) ? mode : ReparentMode.ForceGlobalValues;

        }


        public async Task CheckObjectIssuesAsync()
        {

            // Check connected slots
            Guid guid;
            var connectedReparentSlot = Slots.TryGetValue("Reparent", out guid) && guid != Guid.Empty;

            int countZero = new[] { Position.X, Position.Y, Position.Z }.Count(x => x == 0f);
            bool maxOneZero = countZero == 1;
            bool twoOrMoreZero = countZero >= 2;

            if (ReparentRange <= 0)
                PlausibilityIssues.Add(new PlausibilityIssue($"Range ist fehlerhaft", Severity.Error));

            if (!connectedReparentSlot)
                PlausibilityIssues.Add(new PlausibilityIssue($"Reparent Slot nicht verbunden", Severity.Error));

            if (maxOneZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Warning));
            else if (twoOrMoreZero)
                PlausibilityIssues.Add(new PlausibilityIssue($"Position liegt auf 0: {Position}", Severity.Error));

            if (string.IsNullOrWhiteSpace(ChildrenMark))
                PlausibilityIssues.Add(new PlausibilityIssue($"Kein ChildrenMark angegeben", Severity.Error));

            if (string.IsNullOrWhiteSpace(ParentMark))
                PlausibilityIssues.Add(new PlausibilityIssue($"Kein ParentMark angegeben", Severity.Error));

        }



        //===================================================================================================================
        // M A N U A L   C O N T R O L
        //===================================================================================================================


        public async Task SetReparentAsync()
        {
            var guids = new[] { Guid };
            var names = new[] { "Reparent" };
            var values = new object[] { IsReparenting };

            await Services.ApiInstance.Object.SetSlotValuesAsync(guids, names, values);
        }


    }










    public static class ReparentModesEnumValues
    {
        public static Array All => Enum.GetValues(typeof(ReparentMode));
    }
}
