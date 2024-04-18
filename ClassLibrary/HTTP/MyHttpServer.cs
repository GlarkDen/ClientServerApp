using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary.BaseClasses;
using ClassLibrary.Interfaces;

namespace ClassLibrary.HTTP
{
    /// <summary>
    /// HTTP сервер для обработки запросов от клиентов
    /// </summary>
    public class MyHttpServer : Server, IServer
    {
		/// <summary>
		/// Основной объект сервера, содержащий информацию о подключении
		/// </summary>
        private HttpListener _server;

		/// <summary>
		/// Список обрабатываемых запросов (клиентов)
		/// </summary>
        private List<Task> _tasks;

		/// <summary>
		/// "IP:port" - URL, прослушиваемый сервером
		/// </summary>
		private string _connection = "http://127.0.0.1:5500/connection/";

		/// <summary>
		/// Максимальное число одновременно обрабатываемых запросов для этого типа сервера
		/// </summary>
		public static readonly int HighestRequestNumber = 100;

		/// <summary>
		/// HTTP сервер для обработки запросов от клиентов
		/// </summary>
		/// <param name="checkMethod">Метод для обратоки запроса</param>
		/// <param name="connection">Строка "IP:port" для прослушивания</param>
		public MyHttpServer(CheckMethod checkMethod, string connection) : base(checkMethod)
        {
            _connection = connection;
		}

		/// <summary>
		/// HTTP сервер для обработки запросов от клиентов
		/// </summary>
		/// <param name="checkMethod">Метод для обратоки запроса</param>
		public MyHttpServer(CheckMethod checkMethod) : base(checkMethod)
        {

		}

        /// <summary>
        /// Запуск сервера
        /// </summary>
        public void Start()
        {
            _status = true;
            OnChangedStatus(true);

            cancelTokenSource = new CancellationTokenSource();

			// Запускаем сервер в отдельном потоке без ожидания
			server = Task.Run(async () =>
            {
                _server = new HttpListener();
                _server.Prefixes.Add(_connection);
                _server.Start();

				_tasks = new List<Task>();

				// Пока не получим cancelToken
				while (true)
                {
                    // Ждём подключения клиента
                    var context = await _server.GetContextAsync();

                    if (cancelTokenSource.IsCancellationRequested)
                        break;

					// Считываем текст запроса из потока Stream
					var request = context.Request;
					StreamReader streamReader = new StreamReader(request.InputStream);
					string requestText = streamReader.ReadToEnd();

                    if (requestText == "Handshake")
                    {
						// Для проверки связи
                        SendText("Handshake", context);
					}
                    else if (RequestCount >= MaxRequestCount)
                    {
						// При превышении количества запросов
						SendText("Ошибка", context);
					}
                    else
                    {
						// Запускаем обработку каждого клиента в отдельном потоке
						_tasks.Add(ClientThread(context, requestText));
                    }

					if (cancelTokenSource.IsCancellationRequested)
						break;
				}

				// Ждём закрытия всех подключений
                Task.WaitAll(_tasks.ToArray());
            }, cancelTokenSource.Token);
        }

		/// <summary>
		/// Остановка сервера
		/// </summary>
		public void Stop()
		{
			// Останавливаем приём сообщений
			_server.Stop();

			// Передаём всем потокам сообщение о завершении работы
			cancelTokenSource.Cancel();

			// Выключаем сервер
			_status = false;
			_server.Close();
			OnChangedStatus(false);
		}

		/// <summary>
		/// Отправка сообщения клиенту
		/// </summary>
		/// <param name="text">Текст сообщения</param>
		/// <param name="context">Информация о подклчючении</param>
		private async void SendText(string text, HttpListenerContext context) 
        {
			// Записываем строку сообщения в массив байт
			var response = context.Response;
			Stream output = response.OutputStream;

			byte[] buffer = Encoding.UTF8.GetBytes(text);
			response.ContentLength64 = buffer.Length;

			// Пытаемся отправить (клиент уже мог отвалиться)
			try
			{
                await output.WriteAsync(buffer);
				output.Flush();
			}
			catch 
            {
                output.Close();
            }
		}

        /// <summary>
        /// Обработка запроса клиента
        /// </summary>
        /// <param name="context">Информация о подключении</param>
        /// <param name="request">Запрос</param>
        /// <returns></returns>
        private async Task ClientThread(HttpListenerContext context, string request)
        {
			// Передаём информацию о подключении в событие
			MyClient client = new MyClient(context.Request.UserHostAddress, request);

			waitHandler.WaitOne();
			RequestCount++;
			OnGetRequest(client);
			waitHandler.Set();

			// Обрабатываем запрос
			string answer = request;
            if (checkMethod.Invoke(request))
                answer += ":Да";
            else
                answer += ":Нет";

			// Ждём добавочное время
			await Task.Delay(TaskWorkTime);

			// Отправляем ответ клиенту
			SendText(answer, context);

			// Закрываем подключение
			waitHandler.WaitOne();
			OnCloseRequest(client);
			RequestCount--;
			waitHandler.Set();
		}
    }
}
