using ClassLibrary;
using ClassLibrary.BaseClasses;
using ClassLibrary.HTTP;
using ClassLibrary.Interfaces;
using ClassLibrary.TCP;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Server.ViewModels
{
    public class ViewModel : INotifyPropertyChanged
	{
		// Реализация интерфейса INotifyPropertyChanged для биндинга
		public event PropertyChangedEventHandler? PropertyChanged;

		public void OnPropetryChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		#region Работа с кнопками
		private MyCommand serverStartCommand;
		private MyCommand serverStopCommand;

		private MyCommand setHttpCommand;
		private MyCommand setTcpCommand;

		private MyCommand setMaxRequestCount;

		public ViewModel()
		{
			serverStartCommand = new MyCommand(serverStart, CanExecuteStartServer);
			serverStopCommand = new MyCommand(serverStop, CanExecuteStopServer);
			setHttpCommand = new MyCommand(() => protocolType = MyProtocolType.HTTP, CanExecuteSetHttp);
			setTcpCommand = new MyCommand(() => protocolType = MyProtocolType.TCP, CanExecuteSetTcp);
		}

		public ICommand StartServer
		{
			get => serverStartCommand;
		}

		public ICommand StopServer
		{
			get => serverStopCommand;
		}

		public ICommand SetHttp
		{
			get => setHttpCommand;
		}

		public ICommand SetTcp
		{
			get => setTcpCommand;
		}

		private bool CanExecuteStopServer(object? obj) => Status == "Запущен" && RequestCount == 0;
		private bool CanExecuteStartServer(object? obj) => Status == "Остановлен";
		private bool CanExecuteSetHttp(object? obj) => Status == "Остановлен" && protocolType == MyProtocolType.TCP;
		private bool CanExecuteSetTcp(object? obj) => Status == "Остановлен" && protocolType == MyProtocolType.HTTP;
		#endregion


		// Абстракци для сервера, сейчас здесь может быть HTTP и TCP сервер
		IServer? server;


		// Протокол
		MyProtocolType _protocolType = MyProtocolType.TCP;
		
		private MyProtocolType protocolType
		{
			get => _protocolType;
			set
			{
				_protocolType = value;

				setHttpCommand.RaiseCanExecuteChanged();
				setTcpCommand.RaiseCanExecuteChanged();

				if (MaxRequestCount > maxProtocolRequestCount())
					MaxRequestCount = maxProtocolRequestCount();
			}
		}


		// Коллекция для хранения запросов от клиентов
		public ObservableCollection<MyClient> Requests { get; private set; }
		
		private void addRequest(MyClient client)
		{
			// Здесь приходится передавать метод от вызывающего потока этому, основному,
			// поскольку производится работа в том числе с Binding компонентами
			App.Current.Dispatcher.Invoke((Action)delegate
			{
				Requests.Add(client);
				OnPropetryChanged("Requests");
			});
		}

		private void removeRequest(MyClient client)
		{
			// Здесь приходится передавать метод от вызывающего потока этому, основному,
			// поскольку производится работа в том числе с Binding компонентами
			App.Current.Dispatcher.Invoke((Action)delegate
			{
				Requests.Remove(client);
				OnPropetryChanged("Requests");
			});
		}


		// Текущее количество запросов
		private int _requestCount = 0;

		public int RequestCount
		{
			get => _requestCount;
			set
			{
				_requestCount = value;
				OnPropetryChanged("RequestCount");

				App.Current.Dispatcher.Invoke((Action)delegate
				{
					serverStopCommand.RaiseCanExecuteChanged();
				});
			}
		}

		private void requestCountChange(int requestCount)
		{
			RequestCount = requestCount;
		}


		// Максимальное количество запросов
		private int _maxRequestCount = 5;

		public int MaxRequestCount
		{
			get => _maxRequestCount;
			set
			{
				if (value > maxProtocolRequestCount())
				{
					MessageBox.Show($"Для этого протокола максимальное кол-во равно {maxProtocolRequestCount()}");
				}
				else if (value < 1)
				{
					MessageBox.Show("Количество должно быть не меньше 1");
				}
				else
				{
					_maxRequestCount = value;
					OnPropetryChanged("MaxRequestCount");
					if (Status == "Запущен")
						MessageBox.Show("Изменения вступят в силу при следующем запуске сервера");
				}
			}
		}

		private int maxProtocolRequestCount()
		{
			switch (protocolType)
			{
				case MyProtocolType.TCP:
					return MyTcpServer.HighestRequestNumber;
				case MyProtocolType.HTTP:
					return MyHttpServer.HighestRequestNumber;
				default: 
					return MyTcpServer.HighestRequestNumber;
			}
		}


		// Запуск/остановка сервера
		private void serverStart()
		{
			Requests = new ObservableCollection<MyClient>();
			switch (_protocolType)
			{
				case MyProtocolType.TCP:
					server = new MyTcpServer(RequestChecker.Palindrome);
					break;
				case MyProtocolType.HTTP:
					server = new MyHttpServer(RequestChecker.Palindrome);
					break;
				default:
					server = new MyTcpServer(RequestChecker.Palindrome);
					break;
			}

			server.ChangeRequestCount += requestCountChange;
			server.GetRequest += addRequest;
			server.CloseRequest += removeRequest;

			server.MaxRequestCount = _maxRequestCount;

			server.Start();
			Status = "Запущен";
		}

		private async void serverStop()
		{
			server.ChangeRequestCount -= requestCountChange;
			server.GetRequest -= addRequest;
			server.CloseRequest -= removeRequest;

			server.Stop();

			Status = "Завершает работу...";
			await Task.Delay(1000);
			Status = "Остановлен";
		}


		// Статус сервера, всего их 3: "Остановлен", "Завершает работу...", "Запущен"
		// В целом, можно было бы вынести в перечисления, наверное
		private string _status = "Остановлен";

		public string Status
		{
			get => _status;
			set
			{
				_status = value;
				OnPropetryChanged("Status");

				serverStartCommand.RaiseCanExecuteChanged();
				serverStopCommand.RaiseCanExecuteChanged();
				setHttpCommand.RaiseCanExecuteChanged();
				setTcpCommand.RaiseCanExecuteChanged();
			}
		}
	}
}
