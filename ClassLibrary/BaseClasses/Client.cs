using System.Net;
using System.Threading;

namespace ClassLibrary.BaseClasses
{
    /// <summary>
    /// Клиент для взаимодействия с сервером
    /// </summary>
    public abstract class Client
    {
        /// <summary>
        /// Получение технической информации от сервера
        /// </summary>
        /// <param name="message">Сообщение</param>
        public delegate void GetRequestEventHandler(string message);

		/// <summary>
		/// Получение технической информации от сервера
		/// </summary>
		public event GetRequestEventHandler GetRequest;

		/// <summary>
		/// Получение технической информации от сервера
		/// </summary>
		/// <param name="message">Сообщение</param>
		protected void OnGetRequestMessage(string message)
		{
			GetRequest?.Invoke(message);
		}

		/// <summary>
		/// Получение ответа на запрос от сервера
		/// </summary>
		/// <param name="answer">Запрос/Ответ</param>
		public delegate void GetAnswerEventHandler(MyAnswer answer);

		/// <summary>
		/// Получение ответа на запрос от сервера
		/// </summary>
		public event GetAnswerEventHandler GetAnswer;

		/// <summary>
		/// Получение ответа на запрос от сервера
		/// </summary>
		/// <param name="answer">Запрос/ответ</param>
		protected void OnGetRequestAnswer(MyAnswer answer)
		{
			GetAnswer?.Invoke(answer);
		}

		/// <summary>
		/// Завершение работы клиента
		/// </summary>
		public delegate void StopedClientEventHandler();

		/// <summary>
		/// Завершение работы клиента
		/// </summary>
		public event StopedClientEventHandler StoppedClient;

		/// <summary>
		/// Завершение работы клиента
		/// </summary>
		protected void OnStoppedClient()
		{
			StoppedClient?.Invoke();
		}

		/// <summary>
		/// Для блокировки при многопоточных чтении/записи
		/// </summary>
		protected AutoResetEvent waitHandler = new AutoResetEvent(true);

        /// <summary>
        /// Для взаимодействия потоков
        /// </summary>
        protected AutoResetEvent answerHandler = new AutoResetEvent(true);

        /// <summary>
        /// Время ожидания перед повторным запросом после ошибки (мс)
        /// </summary>
        private int _repeatDelayTime;

		/// <summary>
		/// Время ожидания перед повторным запросом после ошибки (мс)
		/// </summary>
		public int RepeatDelayTime
		{
			get => _repeatDelayTime;
			set => _repeatDelayTime = value;
		}

		/// <summary>
		/// IP адрес и порт сервера
		/// </summary>
		protected IPEndPoint _ipAddress = IPEndPoint.Parse("127.0.0.1:5000");

        /// <summary>
        /// Для завершения работы различных операций в потоках
        /// </summary>
        protected CancellationTokenSource cancellationToken = new CancellationTokenSource();

		/// <summary>
		/// Клиент для взаимодействия с сервером
		/// </summary>
		/// <param name="ipAddress">IP адрес и порт</param>
		/// <param name="repeatDelayTime">Время ожидания перед повторным запросом после ошибки (мс)</param>
		public Client(IPEndPoint ipAddress, int repeatDelayTime = 2000)
        {
            _repeatDelayTime = repeatDelayTime;
            _ipAddress = ipAddress;
        }

		/// <summary>
		/// Клиент для взаимодействия с сервером
		/// </summary>
		/// <param name="repeatDelayTime">Время ожидания перед повторным запросом после ошибки (мс)</param>
		public Client(int repeatDelayTime = 2000)
        {
            _repeatDelayTime = repeatDelayTime;
        }
    }
}
