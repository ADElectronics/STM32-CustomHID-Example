using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UsbHid
{
	public class UsbHidPort
	{
        IntPtr usb_event_handle;

        #region События 
        /// <summary>
        /// Событие, срабатывающее при подключении устройства с указанными PID, VID.
        /// </summary>
        [Description("Событие, срабатывающее при подключении устройства с указанными PID, VID")]
        [Category("Embedded Event")]
        [DisplayName("OnSpecifiedDeviceArrived")]
        public event EventHandler OnSpecifiedDeviceArrived;

        /// <summary>
        /// Событие, срабатывающее при отключении устройства с указанными PID, VID.
        /// </summary>
        [Description("Событие, срабатывающее при отключении устройства с указанными PID, VID")]
        [Category("Embedded Event")]
        [DisplayName("OnSpecifiedDeviceRemoved")]
        public event EventHandler OnSpecifiedDeviceRemoved;

        /// <summary>
        /// Событие, срабатывающее при подключении любого USB-устройства.
        /// </summary>
        [Description("Событие, срабатывающее при подключении любого USB-устройства")]
        [Category("Embedded Event")]
        [DisplayName("OnDeviceArrived")]
        public event EventHandler OnDeviceArrived;

        /// <summary>
        /// Событие, срабатывающее при отключении любого USB-устройства.
        /// </summary>
        [Description("Событие, срабатывающее при отключении любого USB-устройства")]
        [Category("Embedded Event")]
        [DisplayName("OnDeviceRemoved")]
        public event EventHandler OnDeviceRemoved;

        /// <summary>
        /// Событие, срабатывающее при получении данных от выбранного USB-устройства.
        /// </summary>
        [Description("Событие, срабатывающее при получении данных от выбранного USB-устройства")]
        [Category("Embedded Event")]
        [DisplayName("OnDataRecieved")]
        public event DataRecievedEventHandler OnDataRecieved;

        /// <summary>
        /// Событие, срабатывающее при отправке данных к выбранному USB-устройству. 
        /// Будет вызванно только при успешной отправке данных.
        /// </summary>
        [Description("Событие, срабатывающее при отправке данных к выбранному USB-устройству")]
        [Category("Embedded Event")]
        [DisplayName("OnDataSend")]
        public event EventHandler OnDataSend;
        #endregion

        #region Конструктор
        public UsbHidPort()
        {
            SpecifiedDevice = null;
        }

        ~UsbHidPort()
        {
            UnregisterHandle();
        }
        #endregion

        #region Публичные свойства
        [Description("Это Product ID выбранного USB устройства")]
        public UInt16 ProductId { get; private set; }

        [Description("Это Vendor ID выбранного USB устройства")]
        public UInt16 VendorId { get; private set; }

        [Description("Устройство, которое описывает установленный экземпляр")]
        public SpecifiedDevice SpecifiedDevice { get; private set; }

        [Description("Флаг (мнимой) готовности устройства к работе")]
        public bool IsConnected { get { return (SpecifiedDevice != null); } }
        #endregion

        #region Обработка сообщений ОС
        /// <summary>
        /// Регистрация для получения событий USB-шины.  
        /// </summary>
        /// <param name="Handle">Путь до приложения.</param>
        public void RegisterHandle(IntPtr Handle)
        {
            usb_event_handle = Win32Usb.RegisterForUsbEvents(Handle, Win32Usb.HIDGuid);
            // CheckDevicePresent();
        }

        /// <summary>
        /// Отмена регистрации для получения событий USB-шины, это приложение не будет на них реагировать. 
        /// </summary>
        /// <returns>Возвращает true - если успешно.</returns>
        public bool UnregisterHandle()
        {
            if (usb_event_handle != null)
            {
                return Win32Usb.UnregisterForUsbEvents(usb_event_handle);
            }

            return false;
        }
        /// <summary>
        /// This method will filter the messages that are passed for usb device change messages only. 
        /// And parse them and take the appropriate action 
        /// </summary>
        /// <param name="m">a ref to Messages, The messages that are thrown by windows to the application.</param>
        public void ParseMessages(ref System.Windows.Forms.Message m)
        {
            ParseMessages(m.Msg, m.WParam);
        }

        /// <summary>
        /// This method will filter the messages that are passed for usb device change messages only. 
        /// And parse them and take the appropriate action 
        /// </summary>
        /// <param name="m">a ref to Messages, The messages that are thrown by windows to the application.</param>
        public void ParseMessages(int Msg, IntPtr WParam)
        {
            if (Msg == Win32Usb.WM_DEVICECHANGE)
            {
                switch (WParam.ToInt32())
                {
                    case Win32Usb.DEVICE_ARRIVAL:
                        if (OnDeviceArrived != null)
                        {
                            OnDeviceArrived(this, new EventArgs());
                            CheckDevicePresent();
                        }
                        else if (OnSpecifiedDeviceArrived != null)
                        {
                            CheckDevicePresent();
                        }
                        break;
                    case Win32Usb.DEVICE_REMOVECOMPLETE:
                        if (OnDeviceRemoved != null)
                        {
                            OnDeviceRemoved(this, new EventArgs());
                            CheckDevicePresent();
                        }
                        else if (OnSpecifiedDeviceRemoved != null)
                        {
                            CheckDevicePresent();
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        void CheckDevicePresent()
        {
			try
			{
				bool history = false;

				if (SpecifiedDevice != null)
				{
					history = true;
				}

                SpecifiedDevice = SpecifiedDevice.FindSpecifiedDevice(VendorId, ProductId);

				if (SpecifiedDevice != null)
				{
					OnSpecifiedDeviceArrived?.Invoke(this, new EventArgs());
					if (OnDataRecieved != null) SpecifiedDevice.DataRecieved += new DataRecievedEventHandler(OnDataRecieved);
					if (OnDataSend != null) SpecifiedDevice.DataSend += new DataSendEventHandler(OnDataSend);
				}
				else
				{
					if (OnSpecifiedDeviceRemoved != null && history)
					{
						this.OnSpecifiedDeviceRemoved(this, new EventArgs());
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
        }

        private void DataRecieved(object sender, DataRecievedEventArgs args)
        {
            OnDataRecieved?.Invoke(sender, args);
        }

        private void DataSend(object sender, DataSendEventArgs args)
        {
            OnDataSend?.Invoke(sender, args);
        }
        #endregion 

        #region Подключение\отключение
        /// <summary>
        /// Закрываем текущее подключение к устройству, если есть таковое.
        /// </summary>
        public void Close()
        {
            if (SpecifiedDevice != null)
            {
                SpecifiedDevice.Dispose();
                SpecifiedDevice = null;
                if (OnSpecifiedDeviceRemoved != null)
                    this.OnSpecifiedDeviceRemoved(this, new EventArgs());
            }
        }

        /// <summary>
        /// Подключение к первому подходящему устройству по PID\VID при условии (DeviceProduct = null)
        /// </summary>
        /// <returns>Возвращает True - если устройство успешно найденно, иначе False</returns>
        public bool Open(UInt16 VendorID, UInt16 ProductID)
        {
            bool success = true;
            VendorId = VendorID;
            ProductId = ProductID;

            CheckDevicePresent();

            if (SpecifiedDevice == null)
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Подключение напрямую к устройству по указанному пути
        /// </summary>
        /// <param name="DevicePath">Путь к устройству в ОС</param>
        /// <returns>Возвращает True - если устройство успешно найденно, иначе False</returns>
        public bool Open(string DevicePath)
        {
            bool success = true;
			SpecifiedDevice = SpecifiedDevice.OpenSpecifiedDevice(DevicePath);

			if (SpecifiedDevice == null)
			{
				success = false;
			}
			else
			{
				OnSpecifiedDeviceArrived?.Invoke(this, new EventArgs());
				if (OnDataRecieved != null) SpecifiedDevice.DataRecieved += new DataRecievedEventHandler(OnDataRecieved);
				if (OnDataSend != null) SpecifiedDevice.DataSend += new DataSendEventHandler(OnDataSend);
			}
			return success;
        }

        public bool Open(HIDDeviceAvalible Device)
        {
            bool success = true;
            SpecifiedDevice = SpecifiedDevice.OpenSpecifiedDevice(Device.Path);

            if (SpecifiedDevice == null)
            {
                success = false;
            }
            else
            {
                OnSpecifiedDeviceArrived?.Invoke(this, new EventArgs());
                if (OnDataRecieved != null) SpecifiedDevice.DataRecieved += new DataRecievedEventHandler(OnDataRecieved);
                if (OnDataSend != null) SpecifiedDevice.DataSend += new DataSendEventHandler(OnDataSend);
            }
            return success;
        }
        #endregion

        #region Static
        public static List<HIDDeviceAvalible> GetDevicesList(UInt16 VendorId, UInt16 ProductId)
        {
            return SpecifiedDevice.GetDevicesListByVIDPID(VendorId, ProductId);
        }

        public static List<HIDDeviceAvalible> GetDevicesList(UInt16 VendorId)
        {
            return SpecifiedDevice.GetDevicesListByVID(VendorId);
        }

        public static List<HIDDeviceAvalible> GetDevicesList()
        {
            return SpecifiedDevice.GetDevicesList();
        }
        #endregion

        #region Write Reports

        /// <summary>
        /// Записываем Output Report в девайс.
        /// </summary>
        /// <param name="ReportID">Report ID</param>
        /// <param name="Data">Report Data</param>
        /// <returns>Возвращает True, если успешно отправленны данные,иначе False</returns>
        public bool WriteOutputReport(byte ReportID, byte[] Data)
        {
            bool success = false;
            byte[] Report = new byte[SpecifiedDevice.OutputReportLength];

            if (SpecifiedDevice.OutputReportLength - 1 > Data.Length)
            {
                Report[0] = ReportID;
                Array.Copy(Data, 0, Report, 1, Data.Length);
            }
            else
                return success;

            try
            {
                SpecifiedDevice.SendData(Report);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }

        public bool WriteOutputReport(byte[] Data)
        {
            bool success = false;
            try
            {
                SpecifiedDevice.SendData(Data);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Записываем Feature Report в девайс.
        /// </summary>
        /// <param name="ReportID">Report ID</param>
        /// <param name="Data">Report Data to Write</param>
        /// <param name="RespondData">Respond Report Data</param>
        /// <returns>Возвращает True, если успешно отправленны и полученны данные,иначе False</returns>
        public bool WriteFeatureReport(byte ReportID, byte[] Data, ref byte[] RespondData)
        {
            bool success = false;
            byte[] Report = new byte[SpecifiedDevice.FeatureReportLength];

            if(SpecifiedDevice.FeatureReportLength - 1 > Data.Length)
            {
                Report[0] = ReportID;
                Array.Copy(Data, 0, Report, 1, Data.Length);
            }
            else
                return success;

            try
            {
                RespondData = new byte[SpecifiedDevice.FeatureReportLength];
                RespondData = SpecifiedDevice.SendFeature(Report, ref success);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }

        public bool WriteFeatureReport(byte[] Data, ref byte[] RespondData)
        {
            bool success = false;

            try
            {
                RespondData = new byte[SpecifiedDevice.FeatureReportLength];
                RespondData = SpecifiedDevice.SendFeature(Data, ref success);
                success = true;
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }
        #endregion
    }
}
