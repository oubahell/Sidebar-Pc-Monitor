// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

// The upstream NativeMethods.txt requests SetupDiEnumDeviceInterfaces, SetupDiGetDeviceInterfaceDetail,
// SP_DEVICE_INTERFACE_DATA and SP_DEVICE_INTERFACE_DETAIL_DATA_W, but the CsWin32 source generator does
// not emit them with the CsWin32/Win32Metadata package versions this fork restores against, leaving
// BatteryGroup.cs unable to compile. Declared by hand here, matching the shapes CsWin32 would have
// generated, so BatteryGroup.cs needs no changes.

using System;
using System.Runtime.InteropServices;
using Windows.Win32.Devices.DeviceAndDriverInstallation;

namespace Windows.Win32.Devices.DeviceAndDriverInstallation
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SP_DEVICE_INTERFACE_DATA
    {
        internal uint cbSize;
        internal Guid InterfaceClassGuid;
        internal uint Flags;
        internal UIntPtr Reserved;
    }

    internal struct SP_DEVICE_INTERFACE_DETAIL_DATA_W
    {
        internal uint cbSize;
        internal DevicePathInlineArray DevicePath;
    }

    internal struct DevicePathInlineArray
    {
        internal char e0;
    }
}

namespace Windows.Win32
{
    internal static partial class PInvoke
    {
        [DllImport("setupapi.dll", SetLastError = true)]
        internal static extern unsafe bool SetupDiEnumDeviceInterfaces(
            SetupDiDestroyDeviceInfoListSafeHandle DeviceInfoSet,
            void* DeviceInfoData,
            in Guid InterfaceClassGuid,
            uint MemberIndex,
            ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern unsafe bool SetupDiGetDeviceInterfaceDetail(
            HDEVINFO DeviceInfoSet,
            SP_DEVICE_INTERFACE_DATA* DeviceInterfaceData,
            SP_DEVICE_INTERFACE_DETAIL_DATA_W* DeviceInterfaceDetailData,
            uint DeviceInterfaceDetailDataSize,
            uint* RequiredSize,
            void* DeviceInfoData);
    }
}
