using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using UsbHid;

namespace CustomHID_App.ViewModels
{
	public class ReportByte : BindableBase
	{
		#region Data
		byte _Data;
		public byte Data
		{
			get { return _Data; }
			set { SetProperty<byte>(ref _Data, value); }
		}
		#endregion

		#region Color
		Brush _Color;
		public Brush Color
		{
			get { return _Color; }
			set { SetProperty<Brush>(ref _Color, value); }
		}
		#endregion
	}

	public class MainPageViewModel : BindableBase
	{
		DispatcherTimer refreshtimer = new DispatcherTimer();
		bool canrefresh = true;

		#region Публичные свойства
		public UsbHidPort USB { get; private set; } = new UsbHidPort();

		#region ReportInput
		ObservableCollection<ReportByte> _ReportInput;
		public ObservableCollection<ReportByte> ReportInput
		{
			get { return _ReportInput; }
			set { SetProperty<ObservableCollection<ReportByte>>(ref _ReportInput, value); }
		}
		#endregion

		#region ReportOutput
		ObservableCollection<ReportByte> _ReportOutput;
		public ObservableCollection<ReportByte> ReportOutput
		{
			get { return _ReportOutput; }
			set { SetProperty<ObservableCollection<ReportByte>>(ref _ReportOutput, value); }
		}
		#endregion

		#region SendByOutputReport
		bool _SendByOutputReport;
		public bool SendByOutputReport
		{
			get { return _SendByOutputReport; }
			set 
			{ 
				if(SetProperty<bool>(ref _SendByOutputReport, value))
                {
					UpdateOutputReport();
				}
			}
		}
		#endregion

		#region IsConnectByPIDVID
		bool _IsConnectByPIDVID;
		public bool IsConnectByPIDVID
		{
			get { return _IsConnectByPIDVID; }
			set { SetProperty<bool>(ref _IsConnectByPIDVID, value); }
		}
		#endregion

		public UInt16 DeviceVID { get; set; } = 0xADAD;
		public UInt16 DevicePID { get; set; } = 0x1000;

		#region AvalibleDevices
		List<HIDDeviceAvalible> _AvalibleDevices;
		public List<HIDDeviceAvalible> AvalibleDevices
		{
			get { return _AvalibleDevices; }
			set { SetProperty<List<HIDDeviceAvalible>>(ref _AvalibleDevices, value); }
		}
		#endregion

		#region SelectedDevice
		HIDDeviceAvalible _SelectedDevice;
		public HIDDeviceAvalible SelectedDevice
		{
			get { return _SelectedDevice; }
			set { SetProperty<HIDDeviceAvalible>(ref _SelectedDevice, value); }
		}
		#endregion

		#region IsConnected
		bool _IsConnected;
		public bool IsConnected
		{
			get { return _IsConnected; }
			set 
			{ 
				SetProperty<bool>(ref _IsConnected, value);
				IsSettingsUnlocked = !IsConnected;
			}
		}
		#endregion

		#region IsSettingsUnlocked
		bool _IsSettingsUnlocked;
		public bool IsSettingsUnlocked
		{
			get { return _IsSettingsUnlocked; }
			set { SetProperty<bool>(ref _IsSettingsUnlocked, value); }
		}
        #endregion

        #endregion

        #region Команды
        public ICommand Сommand_Connect { get; set; }
		public ICommand Сommand_Disconnect { get; set; }
		public ICommand Сommand_WriteReport { get; set; }
		#endregion

		#region Конструктор
		public MainPageViewModel()
        {
            Application.Current.MainWindow.Loaded += MainWindow_Loaded;

			refreshtimer.Interval = TimeSpan.FromMilliseconds(100);
            refreshtimer.Tick += Refreshtimer_Tick;

			ReportInput = new ObservableCollection<ReportByte>();
			ReportOutput = new ObservableCollection<ReportByte>();

			IsConnected = false;
			IsConnectByPIDVID = false;
			SendByOutputReport = true;

			UpdateAvalibleDevices();
			
			Сommand_Connect = new RelayCommand((p) =>
			{
				USB.OnSpecifiedDeviceRemoved += Usb_OnSpecifiedDeviceRemoved;
				USB.OnDataRecieved += Usb_OnDataRecieved;
				if(IsConnectByPIDVID) 
					IsConnected = USB.Open(DeviceVID, DevicePID);
				else
					IsConnected = USB.Open(SelectedDevice);

				if(IsConnected)
                {
					RaisePropertyChanged("USB");
					for (UInt16 i = 0; i < USB.SpecifiedDevice.InputReportLength; i++)
						ReportInput.Add(new ReportByte() { Color = Brushes.Black, Data = 0x00 });
					UpdateOutputReport();
				}
				
			}, (p) => (SelectedDevice != null && !IsConnected));

			Сommand_Disconnect = new RelayCommand((p) =>
			{
				IsConnected = false;
				ReportInput.Clear();
				ReportOutput.Clear();
				USB.OnSpecifiedDeviceRemoved -= Usb_OnSpecifiedDeviceRemoved;
				USB.OnDataRecieved -= Usb_OnDataRecieved;
				USB.Close();
			}, (p) => IsConnected);

			Сommand_WriteReport = new RelayCommand((p) =>
			{
				SendReportToDevice();
			}, (p) => IsConnected);
		}

        void Refreshtimer_Tick(object sender, EventArgs e)
        {
			canrefresh = true;
			refreshtimer.Stop();
		}

        void UpdateAvalibleDevices()
        {
			if (!IsConnected)
			{
				AvalibleDevices = UsbHidPort.GetDevicesList();

				if(!AvalibleDevices.Contains(SelectedDevice)) // бесполезно, но оставлю... догадались почему?
                {
					SelectedDevice = AvalibleDevices.First();
				}
			}
		}

		void UpdateOutputReport()
		{
			if(IsConnected)
            {
				if (SendByOutputReport)
				{
					if (USB.SpecifiedDevice.OutputReportLength > 0)
					{
						ReportOutput.Clear();
						for (UInt16 i = 0; i < USB.SpecifiedDevice.OutputReportLength; i++)
							ReportOutput.Add(new ReportByte());
					}
					else
					{
						ReportOutput.Clear();
					}
				}
				else
				{
					if (USB.SpecifiedDevice.FeatureReportLength > 0)
					{
						ReportOutput.Clear();
						for (UInt16 i = 0; i < USB.SpecifiedDevice.FeatureReportLength; i++)
							ReportOutput.Add(new ReportByte());
					}
					else
					{
						ReportOutput.Clear();
					}
				}
			}
			else
            {
				ReportInput.Clear();
				ReportOutput.Clear();
			}
		}
		#endregion

		#region Отправка Report
		void SendReportToDevice()
		{
			if (SendByOutputReport && USB.SpecifiedDevice.OutputReportLength > 0)
			{
				byte[] report = new byte[USB.SpecifiedDevice.OutputReportLength];
				for (UInt16 i = 0; i < USB.SpecifiedDevice.OutputReportLength; i++)
					report[i] = ReportOutput[i].Data;
				USB.WriteOutputReport(report);
			} 
			else if (USB.SpecifiedDevice.FeatureReportLength > 0)
            {
				byte[] respond = new byte[USB.SpecifiedDevice.FeatureReportLength];
				byte[] report = new byte[USB.SpecifiedDevice.FeatureReportLength];
				for (UInt16 i = 0; i < USB.SpecifiedDevice.FeatureReportLength; i++)
					report[i] = ReportOutput[i].Data;
				USB.WriteFeatureReport(report, ref respond);
				// TODO: ...
			}
		}
        #endregion

        #region События USB

        void Usb_OnDataRecieved(object sender, DataRecievedEventArgs args)
		{
			if(canrefresh)
            {
				if (!Application.Current.Dispatcher.CheckAccess())
				{
					Application.Current.Dispatcher.Invoke(() => Usb_OnDataRecieved(sender, args));
					return;
				}
				else
				{
					canrefresh = false;
					refreshtimer.Start();

					//ReportInput.Clear();
					for (UInt16 i = 0; i< args.data.Length;i++)
                    {
						if (ReportInput[i].Data != args.data[i])
							ReportInput[i].Color = Brushes.Red;
						else
							ReportInput[i].Color = Brushes.Black;

						ReportInput[i].Data = args.data[i];
					}
				}
			}
		}

		void Usb_OnSpecifiedDeviceRemoved(object sender, EventArgs e)
		{
			IsConnected = false;
			UpdateOutputReport();
		}

		#endregion

		#region Смотрим за событиями в ОС
		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(Application.Current.MainWindow).Handle);
			IntPtr handle = new WindowInteropHelper(Application.Current.MainWindow).Handle; 
			source.AddHook(new HwndSourceHook(WndProc));
			USB.RegisterHandle(handle);
		}

		IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			USB.ParseMessages(msg, wParam);

			if (msg == Win32Usb.WM_DEVICECHANGE && (wParam.ToInt32() == Win32Usb.DEVICE_ARRIVAL || wParam.ToInt32() == Win32Usb.DEVICE_REMOVECOMPLETE))
			{
				UpdateAvalibleDevices();
            }
			return IntPtr.Zero;
		}
		#endregion
	}
}
