using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FS.Components.SimulationSceneObjects.SimpleLogicObjects.Implementations.Mover;
using FS.SDK.Extensibility.Contracts;
using FS.SDK.Io;
using VIBN_Tools.GlobalClasses;
using VIBN_Tools.GlobalClasses.FeeObjects;

namespace VIBN_Tools.InterfaceOperation
{
    public class InterfaceOperationService
    {


        ////===========================================================================================================================
        //// P R O P E R T I E S
        ////===========================================================================================================================


        private readonly Dictionary<InterfaceConnectViewModel, InterfaceAddressPrefixes> _addressPrefixes = new Dictionary<InterfaceConnectViewModel, InterfaceAddressPrefixes>();





        //===========================================================================================================================
        // C O N N E C T   I N T E R F A C E S
        //===========================================================================================================================


        public async Task ConnectInterfacesAsync(InterfaceConnectViewModel interface1, InterfaceConnectViewModel interface2)
        {
            GetAddressPrefixes(interface1, interface2);

            int count = interface1.ByteCount;

            var tasks = Enumerable.Range(0, count).Select(async i =>
            {
                switch (interface1.SelectedMode)
                {
                    case InterfaceConnectMode.SendReceive:
                        await CreateSignalConnectionAsync(interface1, interface2, i);
                        await CreateSignalConnectionAsync(interface2, interface1, i);
                        break;

                    case InterfaceConnectMode.SendOnly:
                        await CreateSignalConnectionAsync(interface1, interface2, i);
                        break;

                    case InterfaceConnectMode.ReceiveOnly:
                        await CreateSignalConnectionAsync(interface2, interface1, i);
                        break;

                    default:
                        break;
                }
            });

            await Task.WhenAll(tasks);

        }




        private void GetAddressPrefixes(InterfaceConnectViewModel interfaceVM1, InterfaceConnectViewModel interfaceVM2)
        {
            _addressPrefixes[interfaceVM1] = new InterfaceAddressPrefixes
            {
                Send = BuildAddressPrefixes(interfaceVM1).Send,
                Receive = BuildAddressPrefixes(interfaceVM1).Receive,
            };

            _addressPrefixes[interfaceVM2] = new InterfaceAddressPrefixes
            {
                Send = BuildAddressPrefixes(interfaceVM2).Send,
                Receive = BuildAddressPrefixes(interfaceVM2).Receive,
            };

        }


        private (string Send, string Receive) BuildAddressPrefixes(InterfaceConnectViewModel interfaceVM)
        {
            var providerGuid = interfaceVM.SelectedInterface.ProviderGuid;

            bool isSiemens =
                providerGuid == PluginGuids.SiemensPLCSIMAdvanced ||
                providerGuid == PluginGuids.SiemensPLCSIMAdvancedNetwork ||
                providerGuid == PluginGuids.SiemensS7Online ||
                providerGuid == PluginGuids.SiemensSinumerikOne ||
                providerGuid == PluginGuids.SiemensSinumerikOneNetwork;

            if (isSiemens)
            {
                if (interfaceVM.UseDbAddressing)
                    return ($"DB{interfaceVM.DbOutNumber}.DBB", $"DB{interfaceVM.DbInNumber}.DBB");

                else
                    return ("AB", "EB");
            }


            if (providerGuid == PluginGuids.KukaRobotInterface
                || providerGuid == PluginGuids.FanucRobotInterface)
            {
                return ("Byte_", "Byte_");
            }

            return ("", "");

        }


        private async Task CreateSignalConnectionAsync(InterfaceConnectViewModel sendInterface, InterfaceConnectViewModel receiveInterface, int offset)
        {

            var sendPrefix = _addressPrefixes[sendInterface].Send;
            var receivePrefix = _addressPrefixes[receiveInterface].Receive;

            FeeInterfaceSignal sendSignal = new FeeInterfaceSignal()
            {
                Tag = $"SendTo_{receiveInterface.SelectedInterface.Name}_Byte{offset}",
                Address = $"{sendPrefix}{sendInterface.StartByte + offset}",
                IOType = IOType.Byte,
                Usage = IOMode.Read,
            };
            await sendSignal.CreateSignalAsync(sendInterface.SelectedInterface);

            FeeInterfaceSignal receiveSignal = new FeeInterfaceSignal()
            {
                Tag = $"ReceiveFrom_{sendInterface.SelectedInterface.Name}_Byte{offset}",
                Address = $"{receivePrefix}{receiveInterface.StartByte + offset}",
                IOType = IOType.Byte,
                Usage = IOMode.Write,
            };
            await receiveSignal.CreateSignalAsync(receiveInterface.SelectedInterface);

            FeeSimpleMove mover = new FeeSimpleMove(nameof(MoveByte));
            await mover.CreateAsync();
            await mover.SendAndWaitAsync();

            var assignments = new (Guid, string, Guid)[]
            {
                (mover.Guid, "Input 01", sendSignal.Guid),
                (mover.Guid, "Output 01", receiveSignal.Guid),
            };

            await Services.ApiInstance.Interface.SendMultipleSlotVarAssignmentsAsync(assignments, true);


        }








        //===========================================================================================================================
        // M E R G E   I N T E R F A C E S
        //===========================================================================================================================

        public async Task MergeSignalsAsync(List<FeeInterfaceSignal> signals)
        {
            var groupedSignals = signals.GroupBy(x => x.ParentInterface.Guid);

            if (!groupedSignals.Any())
                return;

            FeeInterface targetInterface = new FeeInterface()
            {
                Name = "SignalMerge",
            };

            if (!await targetInterface.CreateInterfaceAsync())
                return;


            var tasks = groupedSignals.Select(group =>
            {
                var sourceInterface = group.Key;
                var signalGuidsArray = group.Select(x => x.Guid).ToArray();

                return Services.ApiInstance.Interface.MoveVariablesAsync(sourceInterface, targetInterface.Guid, signalGuidsArray);
            });

            await Task.WhenAll(tasks);

        }



    }




    public class InterfaceAddressPrefixes
    {
        public string Send {  get; set; }
        public string Receive { get; set; }
    }
}
