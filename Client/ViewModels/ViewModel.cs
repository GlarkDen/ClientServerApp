using ClassLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Forms;
using System.IO;
using System.Collections.ObjectModel;
using ClassLibrary.BaseClasses;
using ClassLibrary.TCP;
using ClassLibrary.Interfaces;
using System.Net.Sockets;
using ClassLibrary.HTTP;

namespace Client.ViewModels
{
    internal class ViewModel : INotifyPropertyChanged
	{
		// Реализация интерфейса INotifyPropertyChanged для биндинга
		public event PropertyChangedEventHandler? PropertyChanged;
		public void OnPropetryChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		#region Работа с кнопками
		// Команды для работы с кнопками
		private MyCommand clientStartCommand;
		private MyCommand selectFolderCommand;
		private MyCommand setHttpCommand;
		private MyCommand setTcpCommand;

		public ViewModel()
		{
			// Определяем каждую команду
			setHttpCommand = new MyCommand(() => protocolType = MyProtocolType.HTTP, CanExecuteSetHttp);
			setTcpCommand = new MyCommand(() => protocolType = MyProtocolType.TCP, CanExecuteSetTcp);
			clientStartCommand = new MyCommand(() => startClient(), () => Status == "Остановлен");
			selectFolderCommand = new MyCommand(() => selectFolder(), () => Status == "Остановлен");
		}

		// Команды для работы с кнопками
		public ICommand SetHttp
		{
			get => setHttpCommand;
		}
		public ICommand SetTcp
		{
			get => setTcpCommand;
		}
		public ICommand StartClient
		{
			get => clientStartCommand;
		}
		public ICommand SelectFolder
		{
			get => selectFolderCommand;
		}

		// Команды для смены состояния кнопок: активны/неактивны
		private bool CanExecuteSetHttp(object? obj) => Status == "Остановлен" && protocolType == MyProtocolType.TCP;
		private bool CanExecuteSetTcp(object? obj) => Status == "Остановлен" && protocolType == MyProtocolType.HTTP;
		
		#endregion
		

		// Была ли выбрана папка, откуда будут браться файлы
		private bool _isFolderSelected = false;

		// Выбор папки с файлами
		private void selectFolder()
		{
			// Здесь пришлось подключить WindowsForms, потому что выбор папок добавили только в .NET 8, а это .NET 5
			FolderBrowserDialog folderDialog = new FolderBrowserDialog();
			folderDialog.Description = "Папка с файлами для обработки...";
			folderDialog.UseDescriptionForTitle = true;

			if (folderDialog.ShowDialog() == DialogResult.OK)
			{
				DirectoryPath = folderDialog.SelectedPath;
				_isFolderSelected = true;
			}
		}


		// Используемый протокол
		MyProtocolType _protocolType = MyProtocolType.TCP;

		private MyProtocolType protocolType
		{
			get => _protocolType;
			set
			{
				_protocolType = value;

				// Обновляем кнопки
				setHttpCommand.RaiseCanExecuteChanged();
				setTcpCommand.RaiseCanExecuteChanged();
			}
		}


		// Статус клиента, всего их 2: "Остановлен" "В работе"
		private string _status = "Остановлен";

		public string Status
		{
			get => _status;
			set
			{
				_status = value;
				OnPropetryChanged("Status");

				clientStartCommand.RaiseCanExecuteChanged();
				setHttpCommand.RaiseCanExecuteChanged();
				setTcpCommand.RaiseCanExecuteChanged();
				selectFolderCommand.RaiseCanExecuteChanged();
			}
		}


		// Коллекция для хранения обработанных запросов
		public ObservableCollection<MyAnswer> Answers { get; private set; }

		// Получение ответа от сервера
		private void getAnswer(MyAnswer answer)
		{
			// Здесь приходится передавать метод от вызывающего потока этому, основному,
			// поскольку производится работа в том числе с Binding компонентами
			App.Current.Dispatcher.Invoke((Action)delegate
			{
				Answers.Add(answer);
				OnPropetryChanged("Answers");
			});
		}


		// Абстракция для клиента, сейчас здесь могут быть HTTP и TCP клиенты
		public IClient? _client;


		// Директория с файлами
		private string _directoryPath = "Укажите путь до папки с файлами...";

		public string DirectoryPath
		{
			get { return _directoryPath; }
			set
			{
				_directoryPath = value;
				OnPropetryChanged("DirectoryPath");
			}
		}


		//Текст с информацией для пользователя об обработке запросов
		private string _text = "Empty";

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				OnPropetryChanged("Text");
			}
		}

		private void setMessageText(string message)
		{
			Text = message;
		}


		// Запуск и остановка клиента
		private async void startClient()
		{
			// Пока папка не выбрана, работа не начнётся
			if (_isFolderSelected)
			{
				switch (protocolType)
				{
					case MyProtocolType.TCP:
						_client = new MyTcpClient();
						break;
					case MyProtocolType.HTTP:
						_client = new MyHttpClient();
						break;
					default:
						_client = new MyTcpClient();
						break;
				}

				// Считываем все текстовые файлы из директории
				var files = Directory.GetFiles(_directoryPath, "*.txt");

				if (files.Length == 0) 
				{
					System.Windows.MessageBox.Show("В указанной папке нет ни одного текстового (.txt) файла.");
					return;
				}

				List<string> messages = new List<string>();

				foreach (var file in files)
				{
					messages.Add(await File.ReadAllTextAsync(file));
				}

				// Подписываем нужные методы на события
				_client.GetRequest += setMessageText;
				_client.GetAnswer += getAnswer;
				_client.StoppedClient += clientStopped;

				Answers = new ObservableCollection<MyAnswer>();
				OnPropetryChanged("Answers");

				// Запускаем клиента
				_client.Start(messages.ToArray());
				Status = "В работе";
			}
			else
                System.Windows.MessageBox.Show("Пожалуйста, укажите путь к папке с файлами.");
		}

		private void clientStopped()
		{
			// Здесь приходится передавать метод от вызывающего потока этому, основному,
			// поскольку производится работа в том числе с Binding компонентами
			App.Current.Dispatcher.Invoke((Action)delegate
			{
				Status = "Остановлен";
			});
		}
	}
}
