﻿using System;
using System.Collections.Generic;
using System.Linq;
using VixCOM;
using VMLib.VMware.Exceptions;

namespace VMLib.VMware.VIXItems
{
    public enum VixPowerState
    {
        Off,
        Ready,
        Pending,
        Suspended
    }

    public class VIX : IVix 
    {

        private readonly VixLib _lib;
        private readonly IHost3 _host;
        public VIX()
        {
            _lib = new VixLibClass();
            _host = WaitJobResult<IHost3>(_lib.Connect(Constants.VIX_API_VERSION, Constants.VIX_SERVICEPROVIDER_VMWARE_WORKSTATION, null, 0, null,
                null, 0, null, null));
        }

        public IVM2 ConnectToVM(string path)
        {
            return WaitJobResult<IVM2>(_host.OpenVMEx(path, Constants.VIX_VMOPEN_NORMAL, null, null));
        }

        public void RemoveSnapshot(IVM2 vm, ISnapshot snapshot, bool removechildren)
        {
            WaitJobNoResults(vm.RemoveSnapshot(snapshot, removechildren ? Constants.VIX_SNAPSHOT_REMOVE_CHILDREN : 0, null));
        }

        public void CreateSnapshot(IVM2 vm, string name, string description, bool capturememory)
        {
            WaitJobNoResults(vm.CreateSnapshot(name, description, capturememory ? Constants.VIX_SNAPSHOT_INCLUDE_MEMORY : 0, null, null));
        }

        public void RevertToSnapshot(IVM2 vm, ISnapshot snapshot, bool supressPoweron)
        {
            WaitJobNoResults(vm.RevertToSnapshot(snapshot, supressPoweron ? Constants.VIX_VMPOWEROP_SUPPRESS_SNAPSHOT_POWERON : 0, null, null));
        }

        public IEnumerable<ISnapshot> GetSnapshots(IVM2 vm)
        {
            var result = new List<ISnapshot>();
            var snapshotscount = 0;
            CheckError(vm.GetNumRootSnapshots(out snapshotscount));

            for (var i = 0; i < snapshotscount; i++)
            {
                var snapshot = default(ISnapshot);
                CheckError(vm.GetRootSnapshot(i, out snapshot));

                result.Add(snapshot);
                result.AddRange(GetSubSnapshot(snapshot));
            }

            return result;
        }

        private IEnumerable<ISnapshot> GetSubSnapshot(ISnapshot snapshot)
        {
            var results = new List<ISnapshot>();
            var snapshotcount = 0;
            CheckError(snapshot.GetNumChildren(out snapshotcount));

            for (var i = 0; i < snapshotcount; i++)
            {
                var subsnapshot = default(ISnapshot);
                CheckError(snapshot.GetChild(i, out subsnapshot));

                results.Add(subsnapshot);
                results.AddRange(GetSubSnapshot(subsnapshot));
            }

            return results;
        }

        public IVM2 Clone(string path, IVM2 vm, ISnapshot snapshot, bool linked)
        {
            return
                WaitJobResult<IVM2>(vm.Clone(snapshot,
                    linked ? Constants.VIX_CLONETYPE_LINKED : Constants.VIX_CLONETYPE_FULL, path, 0, null, null));
        }

        public VixPowerState GetState(IVM2 vm)
        {
            var result = default(object);
            CheckError(((IVixHandle2)vm).GetProperties(new[] { Constants.VIX_PROPERTY_VM_POWER_STATE }, ref result));

            var state = (int) ((object[]) result)[0];

            if(CheckBitFlags(state, Constants.VIX_POWERSTATE_POWERED_ON | Constants.VIX_POWERSTATE_TOOLS_RUNNING))
                return VixPowerState.Ready;
            if(CheckBitFlags(state, Constants.VIX_POWERSTATE_POWERED_OFF))
                return VixPowerState.Off;
            return CheckBitFlags(state, Constants.VIX_POWERSTATE_SUSPENDED) ? 
                VixPowerState.Suspended : VixPowerState.Pending;
        }

        private static bool CheckBitFlags(int value, int checkedvalue)
        {
            return (value | checkedvalue) == value;
        }

        private void CheckError(ulong err)
        {
            if (_lib.ErrorIndicatesFailure(err))
                throw new VixException($"Failed to run vix job. Error: {_lib.GetErrorText(err, null)} ({err})", err);
        }

        private T WaitJobResult<T>(IJob job)
        {
            return WaitJobResult<T>(job, new[] { Constants.VIX_PROPERTY_JOB_RESULT_HANDLE });
        }

        private T WaitJobResult<T>(IJob job, int[] properites)
        {
            var results = default(object);
            var err = job.Wait(properites, ref results);

            CheckError(err);

            CloseObject(job);

            return (T)((object[])results)[0];
        }

        private IEnumerable<T> WaitJobResultList<T>(IJob job)
        {
            var results = default(object);
            var err = job.Wait(new[] { Constants.VIX_PROPERTY_JOB_RESULT_HANDLE }, ref results);

            CheckError(err);

            CloseObject(job);

            return ((object[]) results).Cast<T>();
        }

        private void WaitJobNoResults(IJob job)
        {
            var err = job.WaitWithoutResults();

            CheckError(err);

            CloseObject(job);
        }

        public void CloseObject(object vixObject)
        {
            try
            {
                ((IVixHandle2)vixObject).Close();
            }
            catch (Exception)
            {
                //Close is not supported in this version of Vix COM - Ignore
            }
        }

        public IEnumerable<string> GetAllRunning()
        {
            return WaitJobResultList<string>(_host.FindItems(Constants.VIX_FIND_RUNNING_VMS, null, -1, null));
        }

        public void LoginToGuest(IVM2 vm, string username, string password, bool interactive)
        {
            WaitJobNoResults(vm.LoginInGuest(username, password, interactive ? 
                Constants.VIX_LOGIN_IN_GUEST_REQUIRE_INTERACTIVE_ENVIRONMENT : 0, null));
        }

        public void LogoutOfGuest(IVM2 vm)
        {
            WaitJobNoResults(vm.LogoutFromGuest(null));
        }

        public bool FileExists(IVM2 vm, string path)
        {
            return WaitJobResult<bool>(vm.FileExistsInGuest(path, null));
        }

        public bool DirectoryExist(IVM2 vm, string path)
        {
            return WaitJobResult<bool>(vm.DirectoryExistsInGuest(path, null));
        }

        public void CopyFileToGuest(IVM2 vm, string hostpath, string guestpath)
        {
            WaitJobNoResults(vm.CopyFileFromHostToGuest(hostpath, guestpath, 0, null, null));
        }

        public void CopyFileToHost(IVM2 vm, string guestpath, string hostpath)
        {
            WaitJobNoResults(vm.CopyFileFromGuestToHost(guestpath, hostpath, 0, null, null));
        }

        public void DeleteFileInGuest(IVM2 vm, string path)
        {
            WaitJobNoResults(vm.DeleteFileInGuest(path, null));
        }

        public void DeleteDirectoryInGuest(IVM2 vm, string path)
        {
            WaitJobNoResults(vm.DeleteDirectoryInGuest(path, 0, null));
        }

        public void PowerOn(IVM2 vm)
        {
           WaitJobNoResults(vm.PowerOn(Constants.VIX_VMPOWEROP_NORMAL, null, null));
        }

        public void PowerOff(IVM2 vm, bool force)
        {
            WaitJobNoResults(vm.PowerOff(force ? Constants.VIX_VMPOWEROP_NORMAL : Constants.VIX_VMPOWEROP_FROM_GUEST, null));
        }

        public void Restart(IVM2 vm, bool force)
        {
            WaitJobNoResults(vm.Reset(force ? Constants.VIX_VMPOWEROP_NORMAL : Constants.VIX_VMPOWEROP_FROM_GUEST, null));
        }

        public void Delete(IVM2 vm)
        {
            WaitJobNoResults(vm.Delete(Constants.VIX_VMDELETE_DISK_FILES, null));
        }

        public long ExecuteCommand(IVM2 vm, string path, string args, bool activewindow, bool wait)
        {
            var flags = 0;

            if (activewindow)
                flags = flags | Constants.VIX_RUNPROGRAM_ACTIVATE_WINDOW;
            if (!wait)
                flags = flags | Constants.VIX_RUNPROGRAM_RETURN_IMMEDIATELY;

            return WaitJobResult<long>(vm.RunProgramInGuest(path, args, flags, null, null), new [] { Constants.VIX_PROPERTY_JOB_RESULT_PROCESS_ID});
        }

        public void EnableSharedFolders(IVM2 vm)
        {
            WaitJobNoResults(vm.EnableSharedFolders(true, 0, null));
        }

        public void DisableSharedFolders(IVM2 vm)
        {
            WaitJobNoResults(vm.EnableSharedFolders(false, 0, null));
        }

        public void AddSharedFolder(IVM2 vm, string path, string sharename, bool writeaccess)
        {
            WaitJobNoResults(vm.AddSharedFolder(sharename, path, writeaccess ? Constants.VIX_SHAREDFOLDER_WRITE_ACCESS : 0, null ));
        }

        public void RemoveSharedFolder(IVM2 vm, string sharename)
        {
            WaitJobNoResults(vm.RemoveSharedFolder(sharename, 0, null));
        }

        public void WaitForTools(IVM2 vm)
        {
            WaitJobNoResults(vm.WaitForToolsInGuest(int.MaxValue, null));
        }

        public string GetSnapshotName(ISnapshot snapshot)
        {
            var result = default(object);
            CheckError(((IVixHandle2)snapshot).GetProperties(new[] {Constants.VIX_PROPERTY_SNAPSHOT_DISPLAYNAME}, ref result));

            return ((object[]) result)[0].ToString();
        }

        public IEnumerable<VixProcess> GetProcesses(IVM2 vm)
        {
            var returndata = new List<VixProcess>();
            var job = vm.ListProcessesInGuest(0, null);
            CheckError(job.WaitWithoutResults());

            var proccount = job.GetNumProperties(Constants.VIX_PROPERTY_JOB_RESULT_ITEM_NAME);

            for (var i = 0; i < proccount; i++)
            {
                var result = new object[] { };
                CheckError(job.GetNthProperties(i, new[]
                {
                    Constants.VIX_PROPERTY_JOB_RESULT_ITEM_NAME,
                    Constants.VIX_PROPERTY_JOB_RESULT_PROCESS_ID,
                    Constants.VIX_PROPERTY_JOB_RESULT_PROCESS_OWNER,
                    Constants.VIX_PROPERTY_JOB_RESULT_PROCESS_COMMAND
                }, result));

                returndata.Add(new VixProcess
                {
                    Name = result[0] as string,
                    ProcessID = ulong.Parse((string)result[1]),
                    Owner = result[2] as string,
                    Command =  result[3] as string
                });
            }


            return returndata;
        }

        public void KillProcess(IVM2 vm, ulong processID)
        {
            WaitJobNoResults(vm.KillProcessInGuest(processID, 0, null));
        }
    }
}
