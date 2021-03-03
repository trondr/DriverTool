using System;
using System.Runtime.InteropServices;

namespace DriverTool.CSharpLib
{

    //Source: https://stackoverflow.com/questions/6596327/how-to-check-if-a-file-is-signed-in-c/6597017#6597017
    //Source: http://geekswithblogs.net/robp/archive/2007/05/04/112250.aspx
    public static class Wintrust
    {
        [DllImport("Wintrust.dll", PreserveSig = true, SetLastError = false)]
        private static extern uint WinVerifyTrust(IntPtr hWnd, IntPtr pgActionID, IntPtr pWinTrustData);
        private static uint WinVerifyTrust(string fileName)
        {
            uint result = 1;
            if (!System.IO.File.Exists(fileName)) return result;
            var wintrustActionGenericVerifyV2 = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");            
            using (var fileInfo = new WintrustFileInfo(fileName,Guid.Empty))
            using (var guidPtr = new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid))),AllocMethod.HGlobal))
            using (var wvtDataPtr = new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WintrustData))),AllocMethod.HGlobal))
            {
                var data = new WintrustData(fileInfo);
                IntPtr pGuid = guidPtr;
                IntPtr pData = wvtDataPtr;
                Marshal.StructureToPtr(wintrustActionGenericVerifyV2, pGuid, true);
                Marshal.StructureToPtr(data, pData, true);
                result = WinVerifyTrust(IntPtr.Zero, pGuid, pData);
            }
            return result;
        }
        public static bool IsTrusted(string fileName)
        {
            return WinVerifyTrust(fileName) == 0;
        }
    }

    internal struct WintrustFileInfo : IDisposable
    {

        public WintrustFileInfo(string fileName, Guid subject)
        {
            cbStruct = (uint)Marshal.SizeOf(typeof(WintrustFileInfo));
            pcwszFilePath = fileName;
            if (subject != Guid.Empty)
            {
                pgKnownSubject = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                Marshal.StructureToPtr(subject, pgKnownSubject, true);
            }
            else
            {
                pgKnownSubject = IntPtr.Zero;
            }
            hFile = IntPtr.Zero;
        }

        public uint cbStruct;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;

        #region IDisposable Members
        public void Dispose()
        {

            Dispose(true);

        }
        private void Dispose(bool disposing)
        {
            if (pgKnownSubject != IntPtr.Zero)
            {
                Marshal.DestroyStructure(this.pgKnownSubject, typeof(Guid));
                Marshal.FreeHGlobal(this.pgKnownSubject);
            }
        }
        #endregion
    }

    enum AllocMethod
    {
        HGlobal,
        CoTaskMem
    };
    enum UnionChoice
    {
        File = 1,
        Catalog,
        Blob,
        Signer,
        Cert
    };
    enum UiChoice
    {
        All = 1,
        NoUI,
        NoBad,
        NoGood
    };
    enum RevocationCheckFlags
    {
        None = 0,
        WholeChain
    };
    enum StateAction
    {
        Ignore = 0,
        Verify,
        Close,
        AutoCache,
        AutoCacheFlush
    };
    enum TrustProviderFlags
    {
        UseIE4Trust = 1,
        NoIE4Chain = 2,
        NoPolicyUsage = 4,
        RevocationCheckNone = 16,
        RevocationCheckEndCert = 32,
        RevocationCheckChain = 64,
        RecovationCheckChainExcludeRoot = 128,
        Safer = 256,
        HashOnly = 512,
        UseDefaultOSVerCheck = 1024,
        LifetimeSigning = 2048
    };
    enum UiContext
    {
        Execute = 0,
        Install
    };

    [StructLayout(LayoutKind.Sequential)]
    internal struct WintrustData : IDisposable
    {
        public WintrustData(WintrustFileInfo fileInfo)
        {
            this.cbStruct = (uint)Marshal.SizeOf(typeof(WintrustData));
            pInfoStruct = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WintrustFileInfo)));
            Marshal.StructureToPtr(fileInfo, pInfoStruct, false);
            this.dwUnionChoice = UnionChoice.File;
            pPolicyCallbackData = IntPtr.Zero;
            pSIPCallbackData = IntPtr.Zero;
            dwUIChoice = UiChoice.NoUI;
            fdwRevocationChecks = RevocationCheckFlags.None;
            dwStateAction = StateAction.Ignore;
            hWVTStateData = IntPtr.Zero;
            pwszURLReference = IntPtr.Zero;
            dwProvFlags = TrustProviderFlags.Safer;
            dwUIContext = UiContext.Execute;
        }

        public uint cbStruct;

        public IntPtr pPolicyCallbackData;

        public IntPtr pSIPCallbackData;

        public UiChoice dwUIChoice;

        public RevocationCheckFlags fdwRevocationChecks;

        public UnionChoice dwUnionChoice;

        public IntPtr pInfoStruct;

        public StateAction dwStateAction;

        public IntPtr hWVTStateData;

        private IntPtr pwszURLReference;

        public TrustProviderFlags dwProvFlags;

        public UiContext dwUIContext;
        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }
        private void Dispose(bool disposing)
        {
            if (dwUnionChoice == UnionChoice.File)
            {
                WintrustFileInfo info = new WintrustFileInfo();
                Marshal.PtrToStructure(pInfoStruct, info);
                info.Dispose();
                Marshal.DestroyStructure(pInfoStruct, typeof(WintrustFileInfo));
            }
            Marshal.FreeHGlobal(pInfoStruct);
        }
        #endregion
    }

    internal sealed class UnmanagedPointer : IDisposable
    {
        private IntPtr _ptr;
        private AllocMethod _method;
        internal UnmanagedPointer(IntPtr ptr, AllocMethod method)
        {
            _method = method;
            _ptr = ptr;
        }

        ~UnmanagedPointer()
        {
            Dispose(false);
        }

        #region IDisposable Members
        private void Dispose(bool disposing)
        {
            if (_ptr != IntPtr.Zero)
            {
                if (_method == AllocMethod.HGlobal)
                {
                    Marshal.FreeHGlobal(_ptr);
                }
                else if (_method == AllocMethod.CoTaskMem)
                {
                    Marshal.FreeCoTaskMem(_ptr);
                }
                _ptr = IntPtr.Zero;
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public static implicit operator IntPtr(UnmanagedPointer ptr)
        {
            return ptr._ptr;
        }
    }
}
