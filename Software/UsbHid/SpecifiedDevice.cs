using System;
using UsbHid.Reports;

namespace UsbHid
{
    public class DataRecievedEventArgs : EventArgs
    {
        public readonly byte[] data;

        public DataRecievedEventArgs(byte[] data)
        {
            this.data = data;
        }
    }

    public class DataSendEventArgs : EventArgs
    {
        public readonly byte[] data;

        public DataSendEventArgs(byte[] data)
        {
            this.data = data;
        }
    }

    public delegate void DataRecievedEventHandler(object sender, DataRecievedEventArgs args);
    public delegate void DataSendEventHandler(object sender, DataSendEventArgs args);

    public class SpecifiedDevice : HIDDevice
    {
        public event DataRecievedEventHandler DataRecieved;
        public event DataSendEventHandler DataSend;

        public override InputReport CreateInputReport()
        {
            return new SpecifiedInputReport(this);
        }

        public override FeatureReport CreateFeatureInReport()
        {
            return new SpecifiedFeatureReport(this);
        }

        public static SpecifiedDevice FindSpecifiedDevice(int vendor_id, int product_id)
        {
            return (SpecifiedDevice)FindDevice(vendor_id, product_id, typeof(SpecifiedDevice));
        }

		public static SpecifiedDevice OpenSpecifiedDevice(string device_path)
		{
			return (SpecifiedDevice)OpenDevice(device_path, typeof(SpecifiedDevice));
		}

		protected override void HandleDataReceived(InputReport oInRep)
        {
            if (DataRecieved != null)
            {
                SpecifiedInputReport report = (SpecifiedInputReport)oInRep;
                DataRecieved(this, new DataRecievedEventArgs(report.Data));
            }
        }

        public void SendData(byte[] data)
        {
            SpecifiedOutputReport oRep = new SpecifiedOutputReport(this);
            oRep.SendData(data);
            Write(oRep);
            DataSend?.Invoke(this, new DataSendEventArgs(data));
        }

        public byte[] SendFeature(byte[] data, ref bool success)
        {
            SpecifiedFeatureReport oRep = new SpecifiedFeatureReport(this);
            oRep.SendData(data);
            try
            {
                if (!WriteFeature(oRep))
                    success = false;
                if (!ReadFeature(oRep))
                    success = false;
                return oRep.Data;
            }
            catch
            {
                success = false;
            }

            return null;
        }

		protected override void Dispose(bool bDisposing)
        {
            if (bDisposing)
            {
                // ...
            }
            base.Dispose(bDisposing);
        }

    }
}
