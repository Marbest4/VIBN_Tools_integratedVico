using FS.SDK.Io;
using System.Globalization;
using VIBN_Tools.GlobalClasses;

namespace VIBN_Tools.SpecialDevices
{
    public class SpecialDeviceServices
    {

    }


    public static class PlcAddressCalculator
    {

        private static readonly NumberFormatInfo _numberFormatInfo = new NumberFormatInfo()
        {
            NumberDecimalSeparator = ".",
        };



        public static string Calculate(SpecialDeviceAddresses baseAddresses, double offset, IOMode ioMode, IOType ioType)
        {

            int baseAddress = baseAddresses.GetBaseAddress(ioMode);

            return (ioMode, ioType) switch
            {
                // Read
                (IOMode.Read, IOType.Bool) => CreateBoolAddress("A", baseAddress, offset),
                (IOMode.Read, IOType.Byte or IOType.Char) => $"AB{baseAddress + (int)offset}",
                (IOMode.Read, IOType.Word or IOType.Int) => $"AW{baseAddress + (int)offset}",
                (IOMode.Read, IOType.DWord or IOType.DInt or IOType.Real) => $"AD{baseAddress + (int)offset}",

                // Write
                (IOMode.Write, IOType.Bool) => CreateBoolAddress("E", baseAddress, offset),
                (IOMode.Write, IOType.Byte or IOType.Char) => $"EB{baseAddress + (int)offset}",
                (IOMode.Write, IOType.Word or IOType.Int) => $"EW{baseAddress + (int)offset}",
                (IOMode.Write, IOType.DWord or IOType.DInt or IOType.Real) => $"ED{baseAddress + (int)offset}",

                _ => string.Empty,
            };



            //switch (ioMode)
            //{
            //    case IOMode.Read:
            //        switch (ioType)
            //        {
            //            case IOType.Bool:
            //                string temp;
            //                temp = "A" + (baseAddress + offset).ToString(_numberFormatInfo);
            //                if (!temp.Contains("."))
            //                {
            //                    temp += ".0";
            //                }
            //                return temp;

            //            case IOType.Byte:
            //            case IOType.Char:
            //                return "AB" + ((uint)(baseAddress + (int)offset)).ToString();

            //            case IOType.Word:
            //            case IOType.Int:
            //                return "AW" + ((uint)(baseAddress) + (int)offset).ToString();

            //            case IOType.DWord:
            //            case IOType.DInt:
            //            case IOType.Real:
            //                return "AD" + ((uint)(baseAddress) + (int)offset).ToString();

            //            default:
            //                return String.Empty;
            //        }

            //    case IOMode.Write:
            //        switch (ioType)
            //        {
            //            case IOType.Bool:
            //                string temp;
            //                temp = "E" + (baseAddress + offset).ToString(_numberFormatInfo);
            //                if (!temp.Contains("."))
            //                {
            //                    temp += ".0";
            //                }
            //                return temp;

            //            case IOType.Byte:
            //            case IOType.Char:
            //                return "EB" + ((uint)(baseAddress + (int)offset)).ToString();

            //            case IOType.Word:
            //            case IOType.Int:
            //                return "EW" + ((uint)(baseAddress) + (int)offset).ToString();

            //            case IOType.DWord:
            //            case IOType.DInt:
            //            case IOType.Real:
            //                return "ED" + ((uint)(baseAddress) + (int)offset).ToString();

            //            default:
            //                return String.Empty;
            //        }

            //    default:
            //        return String.Empty;
            //}
        }



        private static string CreateBoolAddress(string io, int baseAddress, double offset)
        {
            var result = $"{io}{(baseAddress + offset).ToString(_numberFormatInfo)}";

            return result.Contains('.') ? result : result + ".0";
        }
    }



    public static class RobotAddressCalculator
    {
        public static string Calculate(SpecialDeviceAddresses baseAddresses, double offset, IOMode ioMode, IOType ioType, RobotType robotType)
        {

            int baseAddress = baseAddresses.GetBaseAddress(ioMode);

            return robotType switch
            {
                RobotType.ABB => CalculateAbb(baseAddress, offset),
                RobotType.Kuka => CalculateKuka(baseAddress, offset, ioMode, ioType),
                RobotType.Fanuc => CalculateFanuc(baseAddress, offset, ioMode, ioType),
                _ => throw new NotSupportedException($"RobotType {robotType} not supported")
            };
        }

        private static string CalculateAbb(int baseAddress, double offset)
        {
            return (baseAddress + offset).ToString();
        }

        private static string CalculateFanuc(int baseAddress, double offset, IOMode ioMode, IOType ioType)
        {
            if (ioMode == IOMode.Read)
            {
                return "DOUT[" + (baseAddress + offset).ToString() + "]";
            }
            else if (ioMode == IOMode.Write)
            {
                return "DIN[" + (baseAddress + offset).ToString() + "]";
            }
            return String.Empty;
        }

        private static string CalculateKuka(int baseAddress, double offset, IOMode ioMode, IOType ioType)
        {
            if (ioMode == IOMode.Read && ioType == IOType.Bool)
            {
                return "$OUT[" + (baseAddress + offset).ToString() + "]";
            }
            else if (ioMode == IOMode.Write && ioType == IOType.Bool)
            {
                return "$IN[" + (baseAddress + offset).ToString() + "]";
            }
            else if (ioType == IOType.Byte)
            {
                return "Byte_" + (baseAddress + offset).ToString();
            }
            return String.Empty;
        }

    }













}
