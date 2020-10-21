using System;
using System.Collections.Generic;
using System.Text;

namespace UsbHid.Reports
{
    public class SpecifiedFeatureReport : FeatureReport
    {
        byte[] arrData;

        public SpecifiedFeatureReport(HIDDevice oDev) : base(oDev) 
        {

        }
        public void SendData(byte[] data)
        {
            for (int i = 0; i < Buffer.Length; i++)
                Buffer[i] = data[i];
        }

        public override void ProcessData()
        {
            this.arrData = Buffer;
        }

        public byte[] Data
        {
            get
            {
                return arrData;
            }
        }
    }
}