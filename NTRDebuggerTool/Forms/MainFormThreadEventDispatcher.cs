﻿using NTRDebuggerTool.Forms.FormEnums;
using NTRDebuggerTool.Objects;
using System;
using System.Linq;
using System.Threading;

namespace NTRDebuggerTool.Forms
{
    class MainFormThreadEventDispatcher
    {
        internal bool DispatchConnect = false;
        internal bool DispatchOpenProcess = false;
        internal bool DispatchSearch = false;

        internal string CurrentSelectedProcess = "";
        internal string CurrentMemoryRange = "";
        internal DataTypeExact CurrentSelectedDataType;
        internal SearchTypeBase CurrentSelectedSearchType;
        private MainForm Form;

        internal MainFormThreadEventDispatcher(MainForm Form)
        {
            // TODO: Complete member initialization
            this.Form = Form;
        }

        internal void ThreadEventDispatcher()
        {
            while (true)
            {
                if (DispatchConnect)
                {
                    DispatchConnect = false;
                    DoConnect();
                }
                if (DispatchOpenProcess)
                {
                    DispatchOpenProcess = false;
                    DoOpenProcess();
                }
                if (DispatchSearch)
                {
                    DispatchSearch = false;
                    DoSearch();
                }

                Thread.Sleep(100);
            }
        }

        private void DoConnect()
        {
            if (Form.NTRConnection.IsConnected || Form.ButtonConnectDisconnect.Text == "Disconnect")
            {
                Form.NTRConnection.SetCurrentOperationText = "Disconnecting";
                Form.NTRConnection.Disconnect();
                Form.SetConnectedControls(false);
                Form.SetProcessSelectedControls(false);
                Form.SetConnectText = "Connect";
                Form.ControlEnabledButtonConnectDisconnect = true;
                Form.NTRConnection.SetCurrentOperationText = "";
            }
            else
            {
                Form.SetConnectionControls(false);
                Form.NTRConnection.SetCurrentOperationText = "Connecting";
                Form.NTRConnection.IP = Form.IP.Text;
                Form.NTRConnection.Port = short.Parse(Form.Port.Text);
                if (Form.NTRConnection.Connect())
                {
                    Form.SetConnectText = "Disconnect";
                    Form.NTRConnection.SetCurrentOperationText = "Fetching Processes";
                    Form.NTRConnection.SendListProcessesPacket();
                }
                else
                {
                    Form.SetConnectionControls(true);
                    Form.NTRConnection.SetCurrentOperationText = "";
                }
            }
        }

        private void DoOpenProcess()
        {
            Form.SetConnectedControls(false);
            Form.NTRConnection.SetCurrentOperationText = "Fetching Memory Ranges";
            Form.NTRConnection.SendReadMemoryAddressesPacket(CurrentSelectedProcess.Split('|')[0]);
        }

        private void DoSearch()
        {
            if (Form.NTRConnection.SearchCriteria == null)
            {
                Form.NTRConnection.SearchCriteria = new SearchCriteria();
                Form.NTRConnection.SearchCriteria.ProcessID = BitConverter.ToUInt32(Utilities.GetByteArrayFromByteString(CurrentSelectedProcess.Split('|')[0]), 0);

                if (CurrentMemoryRange.Equals("All"))
                {
                    Form.NTRConnection.SearchCriteria.StartAddress = Form.NTRConnection.SearchCriteria.Length = uint.MaxValue;
                }
                else
                {
                    Form.NTRConnection.SearchCriteria.StartAddress = BitConverter.ToUInt32(Utilities.GetByteArrayFromByteString(Form.MemoryStart.Text).Reverse().ToArray(), 0);
                    Form.NTRConnection.SearchCriteria.Length = BitConverter.ToUInt32(Utilities.GetByteArrayFromByteString(Form.MemorySize.Text).Reverse().ToArray(), 0);
                }

                if (Form.ResultsGrid.Rows.Count > 0 || Form.NTRConnection.SearchCriteria.AddressesFound.Count > 0)
                {
                    Form.NTRConnection.SearchCriteria.Length = Form.GetSearchMemorySize();
                }
                Form.NTRConnection.SearchCriteria.SearchType = this.CurrentSelectedSearchType;
                Form.NTRConnection.SearchCriteria.DataType = this.CurrentSelectedDataType;
            }
            Form.NTRConnection.SetCurrentOperationText = "Searching Memory";

            switch (CurrentSelectedDataType)
            {
                case DataTypeExact.Bytes1: //1 Byte
                    Form.NTRConnection.SearchCriteria.SearchValue = BitConverter.GetBytes((byte)uint.Parse(Form.SearchValue.Text));
                    break;
                case DataTypeExact.Bytes2: //2 Bytes
                    Form.NTRConnection.SearchCriteria.SearchValue = BitConverter.GetBytes(ushort.Parse(Form.SearchValue.Text));
                    break;
                case DataTypeExact.Bytes4: //4 Bytes
                    Form.NTRConnection.SearchCriteria.SearchValue = BitConverter.GetBytes(uint.Parse(Form.SearchValue.Text));
                    break;
                case DataTypeExact.Bytes8: //8 Bytes
                    Form.NTRConnection.SearchCriteria.SearchValue = BitConverter.GetBytes(ulong.Parse(Form.SearchValue.Text));
                    break;
                case DataTypeExact.Float: //Float
                    Form.NTRConnection.SearchCriteria.SearchValue = BitConverter.GetBytes(float.Parse(Form.SearchValue.Text));
                    break;
                case DataTypeExact.Double: //Double
                    Form.NTRConnection.SearchCriteria.SearchValue = BitConverter.GetBytes(double.Parse(Form.SearchValue.Text));
                    break;
                case DataTypeExact.Raw: //Raw Bytes
                    Form.NTRConnection.SearchCriteria.SearchValue = Utilities.GetByteArrayFromByteString(Form.SearchValue.Text);
                    break;
                default: //Text
                    Form.NTRConnection.SearchCriteria.SearchValue = System.Text.Encoding.Default.GetBytes(Form.SearchValue.Text);
                    break;
            }

            Form.NTRConnection.SendReadMemoryPacket();

            Form.ControlEnabledSearchButton = true;
        }
    }
}
