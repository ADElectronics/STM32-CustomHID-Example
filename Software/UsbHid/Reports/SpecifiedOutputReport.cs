using System;
using System.Collections.Generic;
using System.Text;

namespace UsbHid.Reports
{
    public class SpecifiedOutputReport : OutputReport
    {
        public SpecifiedOutputReport(HIDDevice oDev) : base(oDev) 
        {

        }

        public void SendData(byte[] data)
        {
            for (int i = 0; i < Buffer.Length; i++)
                Buffer[i] = data[i];
        }
    }
}
