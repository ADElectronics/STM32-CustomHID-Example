using System;

namespace UsbHid.Reports
{
	public abstract class Report
	{
		byte[] m_arrBuffer;
		int m_nLength;

		public Report(HIDDevice oDev)
		{

		}

		protected void SetBuffer(byte[] arrBytes)
		{
			m_arrBuffer = arrBytes;
			m_nLength = m_arrBuffer.Length;
		}

		public byte[] Buffer
		{
			get { return m_arrBuffer; }
			set { m_arrBuffer = value; }
		}

		public int BufferLength
		{
			get { return m_nLength; }
		}
	}

	public abstract class OutputReport : Report
	{
		public OutputReport(HIDDevice oDev) : base(oDev)
		{
			SetBuffer(new byte[oDev.OutputReportLength]);
		}
	}

	public abstract class InputReport : Report
	{
		public InputReport(HIDDevice oDev) : base(oDev)
		{

		}

		public void SetData(byte[] arrData)
		{
			SetBuffer(arrData);
			ProcessData();
		}

		public abstract void ProcessData();
	}

    public abstract class FeatureReport : Report
    {
        public FeatureReport(HIDDevice oDev) : base(oDev)
		{
            SetBuffer(new byte[oDev.FeatureReportLength]);
		}

        public void SetData(byte[] arrData)
        {
            SetBuffer(arrData);
            ProcessData();
        }

        public abstract void ProcessData();
    }
}
