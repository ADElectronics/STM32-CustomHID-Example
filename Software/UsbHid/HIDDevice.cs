using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using UsbHid.Reports;

namespace UsbHid
{
	public class HIDDeviceAvalible
	{
		public string Path { get; set; }
		public UInt16 VendorId { get; set; }
		public UInt16 ProductId { get; set; }
		public string Description { get; set; }
	}

	public abstract class HIDDevice : Win32Usb, IDisposable
	{
		FileStream m_iFile;
		IntPtr m_hHandle;

		// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/_hid/

		#region Public
		public event EventHandler OnDeviceRemoved;

		public int OutputReportLength { get; private set; }
		public int InputReportLength { get; private set; }
		public int FeatureReportLength { get; private set; }

		public virtual InputReport CreateInputReport() { return null; }
		public virtual FeatureReport CreateFeatureInReport() { return null; }
		#endregion

		#region IDisposable Members
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposer called by both dispose and finalise
		/// </summary>
		/// <param name="bDisposing">True if disposing</param>
		protected virtual void Dispose(bool bDisposing)
		{
			try
			{
				if (bDisposing)
				{
					if (m_iFile != null)
					{
						m_iFile.Close();
						m_iFile = null;
					}
				}
				if (m_hHandle != IntPtr.Zero)
				{

					CloseHandle(m_hHandle);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message.ToString());
			}
		}
		#endregion

		#region Privates/protected
		/// <summary>
		/// Initialises the device
		/// </summary>
		/// <param name="strPath">Path to the device</param>
		private void Initialise(string strPath)
		{
			m_hHandle = CreateFile(strPath, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);

			if ( m_hHandle != InvalidHandleValue || m_hHandle == null)
			{
				IntPtr lpData;
				if (HidD_GetPreparsedData(m_hHandle, out lpData))
				{
					try
					{
						HidCaps oCaps;
						HidP_GetCaps(lpData, out oCaps);
						InputReportLength = oCaps.InputReportByteLength;
						OutputReportLength = oCaps.OutputReportByteLength;
						FeatureReportLength = oCaps.FeatureReportByteLength;

						//m_oFile = new FileStream(m_hHandle, FileAccess.Read | FileAccess.Write, true, m_nInputReportLength, true);
						if (InputReportLength > 0)
                        {
							m_iFile = new FileStream(new SafeFileHandle(m_hHandle, false), FileAccess.Read | FileAccess.Write, InputReportLength, true);
							//HidD_FlushQueue(m_hHandle);
							BeginAsyncRead();
						}
					}
					catch(Exception ex)
					{
						throw HIDDeviceException.GenerateWithWinError("Failed to create FileStream to device.");
					}
					finally
					{
						HidD_FreePreparsedData(ref lpData);
					}
				}
				else
				{
					throw HIDDeviceException.GenerateWithWinError("GetPreparsedData failed");
				}
			}
			else
			{
				m_hHandle = IntPtr.Zero;
				throw HIDDeviceException.GenerateWithWinError("Failed to create device file");
			}
		}

		/// <summary>
		/// Kicks off an asynchronous read which completes when data is read or when the device
		/// is disconnected. Uses a callback.
		/// </summary>
		private void BeginAsyncRead()
		{	
			if(m_iFile != null)
            {
				byte[] arrInputReport = new byte[InputReportLength];
				m_iFile.BeginRead(arrInputReport, 0, InputReportLength, new AsyncCallback(ReadCompleted), arrInputReport);
			}	
		}

		/// <summary>
		/// Callback for above. Care with this as it will be called on the background thread from the async read
		/// </summary>
		/// <param name="iResult">Async result parameter</param>
		protected void ReadCompleted(IAsyncResult iResult)
		{
			if (m_iFile == null)
				return; 

			byte[] arrBuff = (byte[])iResult.AsyncState;
			try
			{
				m_iFile.EndRead(iResult);
				try
				{
					InputReport oInRep = CreateInputReport();
					oInRep.SetData(arrBuff);
					HandleDataReceived(oInRep);
				}
				finally
				{
					BeginAsyncRead();
				}
			}
			catch (IOException ex)
			{
				HandleDeviceRemoved();
                OnDeviceRemoved?.Invoke(this, new EventArgs());
                Dispose();
			}
		}

		protected bool Write(OutputReport oOutRep)
		{
			bool success = false;
			try
			{
				success = HidD_SetOutputReport(m_hHandle, oOutRep.Buffer, oOutRep.BufferLength);
			}
			catch (IOException)
			{
				HandleDeviceRemoved();
				OnDeviceRemoved?.Invoke(this, new EventArgs());
				Dispose();
			}
			return success;
		}

		protected bool WriteFeature(FeatureReport oFeaRep)
		{
			bool success = false;
			try
			{
				success = HidD_SetFeature(m_hHandle, oFeaRep.Buffer, oFeaRep.BufferLength);
			}
			catch (IOException)
			{
				HandleDeviceRemoved();
                OnDeviceRemoved?.Invoke(this, new EventArgs());
                Dispose();
			}
			return success;
		}

		protected bool ReadFeature(FeatureReport oFeaRep)
		{
			byte[] arrBuff = new byte[FeatureReportLength];
			bool success = false;

			try
			{
				success = HidD_GetFeature(m_hHandle, arrBuff, arrBuff.Length);
				oFeaRep.SetData(arrBuff);
			}
			catch (IOException)
			{
				HandleDeviceRemoved();
				OnDeviceRemoved?.Invoke(this, new EventArgs());
				Dispose();
			}

			return success;
		}

		protected string GetManufacturerString()
		{
			byte[] arrBuff = new byte[255];
			bool success = HidD_GetManufacturerString(m_hHandle, arrBuff, arrBuff.Length);
			string manufacturer = "";

			if (success)
			{
				foreach (char b in arrBuff)
				{
					if (b != 0)
						manufacturer += b.ToString();
				}
			}
			return manufacturer;
		}

		protected string GetProductString()
		{
			byte[] arrBuff = new byte[255];
			bool success = HidD_GetProductString(m_hHandle, arrBuff, arrBuff.Length);
			string product = "";

			if (success)
			{
				foreach (char b in arrBuff)
				{
					if (b != 0)
						product += b.ToString();
				}
			}
			return product;
		}

		public static string GetProductString(string device_path)
		{
			IntPtr dev_handle = CreateFile(device_path, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
			bool success = false;
			byte[] arrBuff = new byte[255];
			string product = "";

			if (dev_handle != InvalidHandleValue || dev_handle == null)
			{
				success = HidD_GetProductString(dev_handle, arrBuff, arrBuff.Length);
				if (success)
				{
					foreach (char b in arrBuff)
					{
						if (b != 0)
							product += b.ToString();
					}
				}
				CloseHandle(dev_handle);
			}

			return product;
		}

		/// <summary>
		/// virtual handler for any action to be taken when data is received. Override to use.
		/// </summary>
		/// <param name="oInRep">The input report that was received</param>
		protected virtual void HandleDataReceived(InputReport oInRep)
		{
		}

		/// <summary>
		/// Virtual handler for any action to be taken when a device is removed. Override to use.
		/// </summary>
		protected virtual void HandleDeviceRemoved()
		{
		}

		/// <summary>
		/// Helper method to return the device path given a DeviceInterfaceData structure and an InfoSet handle.
		/// Used in 'FindDevice' so check that method out to see how to get an InfoSet handle and a DeviceInterfaceData.
		/// </summary>
		/// <param name="hInfoSet">Handle to the InfoSet</param>
		/// <param name="oInterface">DeviceInterfaceData structure</param>
		/// <returns>The device path or null if there was some problem</returns>
		private static string GetDevicePath(IntPtr hInfoSet, ref DeviceInterfaceData oInterface)
		{
			uint nRequiredSize = 0;

			if (!SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, IntPtr.Zero, 0, ref nRequiredSize, IntPtr.Zero))
			{
				DeviceInterfaceDetailData oDetail = new DeviceInterfaceDetailData();

				if (IntPtr.Size == 8)
					oDetail.Size = 8;
				else
					oDetail.Size = 5;

				if (SetupDiGetDeviceInterfaceDetail(hInfoSet, ref oInterface, ref oDetail, nRequiredSize, ref nRequiredSize, IntPtr.Zero))
				{
					return oDetail.DevicePath;
				}
			}
			return null;
		}
		#endregion

		#region Public static
		/// <summary>
		/// Finds a device given its PID and VID
		/// </summary>
		/// <param name="nVid">Vendor id for device (VID)</param>
		/// <param name="nPid">Product id for device (PID)</param>
		/// <param name="oType">Type of device class to create</param>
		/// <returns>A new device class of the given type or null</returns>
		public static HIDDevice FindDevice(int nVid, int nPid, Type oType)
		{
			string strPath = string.Empty;
			string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}", nVid, nPid);
			Guid gHid = HIDGuid;

			HidD_GetHidGuid(out gHid);

			IntPtr hInfoSet = SetupDiGetClassDevs(ref gHid, null, IntPtr.Zero, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

			try
			{
				int nIndex = 0;
				DeviceInterfaceData oInterface = new DeviceInterfaceData();
				oInterface.Size = Marshal.SizeOf(oInterface);

				while (SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref gHid, (uint)nIndex, ref oInterface))
				{
					string strDevicePath = GetDevicePath(hInfoSet, ref oInterface);
					if (strDevicePath.IndexOf(strSearch) >= 0)
					{
						HIDDevice oNewDevice = (HIDDevice)Activator.CreateInstance(oType);
						oNewDevice.Initialise(strDevicePath);
						return oNewDevice;
					}
					nIndex++;
				}
			}
			catch (Exception ex)
			{
				throw HIDDeviceException.GenerateError(ex.ToString());
			}
			finally
			{
				SetupDiDestroyDeviceInfoList(hInfoSet);
			}
			return null;
		}

		public static HIDDevice OpenDevice(string device_path, Type oType)
		{
			try
			{
				HIDDevice oNewDevice = (HIDDevice)Activator.CreateInstance(oType);
				oNewDevice.Initialise(device_path);
				return oNewDevice;
			}
			catch (Exception ex)
			{
				throw HIDDeviceException.GenerateError(ex.ToString());
			}
		}

		public static List<HIDDeviceAvalible> GetDevicesListByVIDPID(int nVid, int nPid)
		{
			List<HIDDeviceAvalible> devices = new List<HIDDeviceAvalible>();
			string strSearch = string.Format("vid_{0:x4}&pid_{1:x4}", nVid, nPid);
			Guid gHid;

			HidD_GetHidGuid(out gHid);

			IntPtr hInfoSet = SetupDiGetClassDevs(ref gHid, null, IntPtr.Zero, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

			try
			{
				uint nIndex = 0;
				DeviceInterfaceData oInterface = new DeviceInterfaceData();
				oInterface.Size = Marshal.SizeOf(oInterface);

				while (SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref gHid, nIndex, ref oInterface))
				{
					string strDevicePath = GetDevicePath(hInfoSet, ref oInterface);
					if (strDevicePath.IndexOf(strSearch) >= 0)
					{
						HIDDeviceAvalible device = new HIDDeviceAvalible();
						device.Path = strDevicePath;
						device.Description = HIDDevice.GetProductString(device.Path);

						int cut = device.Path.IndexOf("vid_");
						string xid_start = device.Path.Substring(cut + 4);
						device.VendorId = Convert.ToUInt16(xid_start.Substring(0, 4), 16);

						cut = device.Path.IndexOf("pid_");
						xid_start = device.Path.Substring(cut + 4);
						device.ProductId = Convert.ToUInt16(xid_start.Substring(0, 4), 16);

						if (!String.IsNullOrEmpty(device.Description))
							devices.Add(device);
					}
					nIndex++;
				}
			}
			catch (Exception ex)
			{
				throw HIDDeviceException.GenerateError(ex.ToString());
			}
			finally
			{
				SetupDiDestroyDeviceInfoList(hInfoSet);
			}

			return devices;
		}

		public static List<HIDDeviceAvalible> GetDevicesListByVID(int nVid)
		{
			List<HIDDeviceAvalible> devices = new List<HIDDeviceAvalible>();
			string strSearch = string.Format("vid_{0:x4}&pid_", nVid);
			Guid gHid;

			HidD_GetHidGuid(out gHid);

			IntPtr hInfoSet = SetupDiGetClassDevs(ref gHid, null, IntPtr.Zero, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

			try
			{
				uint nIndex = 0;
				DeviceInterfaceData oInterface = new DeviceInterfaceData();
				oInterface.Size = Marshal.SizeOf(oInterface);

				while (SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref gHid, nIndex, ref oInterface))
				{
					string strDevicePath = GetDevicePath(hInfoSet, ref oInterface);
					if (strDevicePath.IndexOf(strSearch) >= 0)
					{
						HIDDeviceAvalible device = new HIDDeviceAvalible();
						device.Path = strDevicePath;
						device.Description = HIDDevice.GetProductString(device.Path);

						int cut = device.Path.IndexOf("vid_");
						string xid_start = device.Path.Substring(cut + 4);
						device.VendorId = Convert.ToUInt16(xid_start.Substring(0, 4), 16);

						cut = device.Path.IndexOf("pid_");
						xid_start = device.Path.Substring(cut + 4);
						device.ProductId = Convert.ToUInt16(xid_start.Substring(0, 4), 16);

						if (!String.IsNullOrEmpty(device.Description))
							devices.Add(device);
					}
					nIndex++;
				}
			}
			catch (Exception ex)
			{
				throw HIDDeviceException.GenerateError(ex.ToString());
			}
			finally
			{
				SetupDiDestroyDeviceInfoList(hInfoSet);
			}

			return devices;
		}

		public static List<HIDDeviceAvalible> GetDevicesList()
		{
			List<HIDDeviceAvalible> devices = new List<HIDDeviceAvalible>();
			Guid gHid;

			HidD_GetHidGuid(out gHid);

			IntPtr hInfoSet = SetupDiGetClassDevs(ref gHid, null, IntPtr.Zero, DIGCF_DEVICEINTERFACE | DIGCF_PRESENT);

			try
			{
				uint nIndex = 0;
				DeviceInterfaceData oInterface = new DeviceInterfaceData();
				oInterface.Size = Marshal.SizeOf(oInterface);

				while (SetupDiEnumDeviceInterfaces(hInfoSet, 0, ref gHid, nIndex, ref oInterface))
				{
					HIDDeviceAvalible device = new HIDDeviceAvalible();
					device.Path = GetDevicePath(hInfoSet, ref oInterface);
					device.Description = HIDDevice.GetProductString(device.Path);

					int cut = device.Path.IndexOf("vid_");
					string xid_start = device.Path.Substring(cut + 4);
					device.VendorId = Convert.ToUInt16(xid_start.Substring(0, 4), 16);

					cut = device.Path.IndexOf("pid_");
					xid_start = device.Path.Substring(cut + 4);
					device.ProductId = Convert.ToUInt16(xid_start.Substring(0, 4), 16);

					if (!String.IsNullOrEmpty(device.Description))
						devices.Add(device);
					nIndex++;
				}
			}
			catch (Exception ex)
			{
				throw HIDDeviceException.GenerateError(ex.ToString());
			}
			finally
			{
				SetupDiDestroyDeviceInfoList(hInfoSet);
			}

			return devices;
		}
		#endregion
	}
}
