using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ClassLibrary.BaseClasses
{
    /// <summary>
    /// Сервер, обрабатывающий запросы клиентов
    /// </summary>
    public abstract class Server
    {
        /// <summary>
        /// Изменение количества запросов
        /// </summary>
        /// <param name="count">Новое количество</param>
        public delegate void ChangeRequestCountEventHandler(int count);

		/// <summary>
		/// Изменение количества запросов
		/// </summary>
		public event ChangeRequestCountEventHandler ChangeRequestCount;

		/// <summary>
		/// Изменение количества запросов
		/// </summary>
		/// <param name="count">Новое количество</param>
		protected void OnChangeRequestCount(int count)
        {
            ChangeRequestCount?.Invoke(count);
        }

		/// <summary>
		/// Добавление/обработка запроса от клиента
		/// </summary>
		/// <param name="client">Клиент</param>
        public delegate void RequestEventHandler(MyClient client);

		/// <summary>
		/// Получение нового запроса от клиента
		/// </summary>
        public event RequestEventHandler GetRequest;

		/// <summary>
		/// Завершение обработки запроса от клиента
		/// </summary>
        public event RequestEventHandler CloseRequest;

		/// <summary>
		/// Получение нового запроса от клиента
		/// </summary>
		/// <param name="client">Клиент</param>
		protected void OnGetRequest(MyClient client)
        {
            GetRequest?.Invoke(client);
        }

		/// <summary>
		/// Завершение обработки запроса от клиента
		/// </summary>
		/// <param name="client">Клиент</param>
		protected void OnCloseRequest(MyClient client)
        {
            CloseRequest?.Invoke(client);
        }

		/// <summary>
		/// Включение или отключение сервера
		/// </summary>
		/// <param name="status">Вкл/выкл</param>
		public delegate void ChangedStatusEventHandler(bool status);

		/// <summary>
		/// Включение или отключение сервера
		/// </summary>
		public event ChangedStatusEventHandler ChangedStatus;

		/// <summary>
		/// Включение или отключение сервера
		/// </summary>
		/// <param name="status">Вкл/выкл</param>
		protected void OnChangedStatus(bool status)
		{
			ChangedStatus?.Invoke(status);
		}

		/// <summary>
		/// Для взаимодействия между потоками
		/// </summary>
		protected CancellationTokenSource cancelTokenSource;

		/// <summary>
		/// Метод для обработки запроса
		/// </summary>
		/// <param name="text">Запрос</param>
		/// <returns>Результат обработки: соответствие заданному критерию</returns>
        public delegate bool CheckMethod(string text);

		/// <summary>
		/// Метод для обработки запроса
		/// </summary>
		private CheckMethod _checkMethod;

		/// <summary>
		/// Метод для обработки запроса
		/// </summary>
		public CheckMethod checkMethod
		{
			get => _checkMethod;
			set => _checkMethod = value;
		}

		/// <summary>
		/// Количество запросов в обработке
		/// </summary>
		private int _requestCount = 0;

		/// <summary>
		/// Количество запросов в обработке
		/// </summary>
		protected int RequestCount
        {
            get => _requestCount;
            set
            {
                _requestCount = value;
                OnChangeRequestCount(_requestCount);
            }
        }

		/// <summary>
		/// Максимальное количество одновременно обрабатываемых запросов
		/// </summary>
		protected int _maxRequestCount = 5;

		/// <summary>
		/// Максимальное количество одновременно обрабатываемых запросов
		/// </summary>
		public int MaxRequestCount
		{
			get => _maxRequestCount;
			set => _maxRequestCount = value;
		}

		/// <summary>
		/// Дополнительное время обработки запроса
		/// </summary>
		protected int _taskWorkTime = 2000;

		/// <summary>
		/// Дополнительное время обработки запроса
		/// </summary>
		public int TaskWorkTime
		{
			get => _taskWorkTime;
			set => _taskWorkTime = value;
		}

		/// <summary>
		/// Статус сервера: включён-true/выключен-false
		/// </summary>
        protected bool _status = false;

		/// <summary>
		/// Статус сервера: включён-true/выключен-false
		/// </summary>
		public bool Status
		{
			get => _status;
			private set => _status = value;
		}

		/// <summary>
		/// IP адрес и порт, прослушиваемые сервером
		/// </summary>
		protected IPEndPoint _ipAddress = IPEndPoint.Parse("127.0.0.1:5000");

		/// <summary>
		/// Для блокировки при многопоточных чтении/записи
		/// </summary>
		protected AutoResetEvent waitHandler = new AutoResetEvent(true);

		/// <summary>
		/// Основной поток (Thread) сервера
		/// </summary>
        protected Task server;

		/// <summary>
		/// Сервер, обрабатывающий запросы клиентов
		/// </summary>
		/// <param name="checkMethod">Метод, обрабатыващий запрос</param>
		/// <param name="ipAddress">IP адрес и порт</param>
		public Server(CheckMethod checkMethod, IPEndPoint ipAddress)
        {
            _checkMethod = checkMethod;
            _ipAddress = ipAddress;
        }

		/// <summary>
		/// Сервер, обрабатывающий запросы клиентов
		/// </summary>
		/// <param name="checkMethod">Метод, обрабатыващий запрос</param>
		public Server(CheckMethod checkMethod)
        {
            _checkMethod = checkMethod;
        }
    }
}
