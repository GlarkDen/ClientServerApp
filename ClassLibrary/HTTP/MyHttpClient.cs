using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary.BaseClasses;
using ClassLibrary.Interfaces;

namespace ClassLibrary.HTTP
{
    /// <summary>
    /// HTTP клиент для взаимодействия с сервером
    /// </summary>
    public class MyHttpClient : Client, IClient
    {
        /// <summary>
        /// Базовый объект, хранящий информацию о подключении
        /// </summary>
        HttpClient client = new HttpClient();

		/// <summary>
		/// "IP:port" - URL, прослушиваемый сервером
		/// </summary>
		private string _connection = "http://127.0.0.1:5500/connection/";

		/// <summary>
		/// HTTP клиент для взаимодействия с сервером
		/// </summary>
		/// <param name="connection">Строка "IP:port" для прослушивания</param>
		/// <param name="repeatDelayTime">Время ожидания до отправки запроса после получения ошибки</param>
		public MyHttpClient(string connection, int repeatDelayTime = 2000) : base(repeatDelayTime)
        {
            RepeatDelayTime = repeatDelayTime;
            _connection = connection;
        }

		/// <summary>
		/// HTTP клиент для взаимодействия с сервером
		/// </summary>
		/// <param name="repeatDelayTime">Время ожидания до отправки запроса после получения ошибки</param>
		public MyHttpClient(int repeatDelayTime = 2000) : base(repeatDelayTime)
        {
            RepeatDelayTime = repeatDelayTime;
        }

        /// <summary>
        /// Запуск клиента
        /// </summary>
        /// <param name="messages">Список запросов для отправки на сервер</param>
        public async void Start(string[] messages)
        {
            // Время ожидания ответа на запрос (не уверен, что оно тут прямо нужно, оно странно работает)
			client.Timeout = TimeSpan.FromSeconds(10);

			#region Проверяем, если ли связь с сервером
			OnGetRequestMessage("Соединяюсь с сервером...");
			try
            {
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _connection);
				request.Content = new StringContent("Handshake", Encoding.UTF8);

				// Отправляем запрос и ждём
				Task<HttpResponseMessage> connect = client.SendAsync(request);
                await Task.Delay(3000);

                // Если сервер так и не ответил, завершаем работу
				if (connect.IsCompletedSuccessfully)
					OnGetRequestMessage("Сервер обнаружен");
				else
				{
                    OnGetRequestMessage("Нет соединения");
					OnStoppedClient();
					return;
				}
			}
            catch
            {
                OnGetRequestMessage("Нет соединения");
				OnStoppedClient();
				return;
            }
			#endregion

			// Запускаем клиента в отдельном потоке
			_ = Task.Run(() =>
            {
                // Под каждый запрос создаём новое подключение
                List<Task> tasks = new List<Task>();

                foreach (var item in messages)
                {
                    tasks.Add(Task.Run(async () => await Request(item)));
                }

                // Ждём закрытия всех подключений
                Task.WaitAll(tasks.ToArray());
				OnStoppedClient();
			});
        }

        /// <summary>
        /// Отправка запроса на сервер
        /// </summary>
        /// <param name="message">Текст запроса</param>
        private async Task Request(string message)
        {
            // Пока не будет получен ответ или связь не прервётся
            while (true)
            {
                // определяем данные запроса
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _connection);
                request.Content = new StringContent(message, Encoding.UTF8);

                // Ждём ответа то сервера (встроенный Timeout тут у меня почему-то не работает)
				Task<HttpResponseMessage> connect = client.SendAsync(request);

                // Тут главное не ставить время меньше 3000, сервер обрабатывает сообщения 3 сек по умолчанию
				await Task.Delay(5000);

				// Если сервер так и не ответил, завершаем работу
				if (!connect.IsCompletedSuccessfully)
				{
                    OnGetRequestMessage($"Нет соединения - {message}"); ;
					break;
				}

				var result = await connect.Result.Content.ReadAsStringAsync();

				// Если получена ошибка (перегрузка сервера), повторяем процедуру через RepeatDelayTime
				if (result == "Ошибка")
                {
					waitHandler.WaitOne();
					OnGetRequestMessage($"{message}:Ошибка");
					waitHandler.Set();

					Task.Delay(RepeatDelayTime).Wait();
                }
                else
                {
					waitHandler.WaitOne();

                    // Отпраляем информацию о выполнении запроса в событие
					OnGetRequestMessage(result);
					OnGetRequestAnswer(MyAnswer.DecodeServerAnswer(result));

					waitHandler.Set();
					break;
                }
            }
        }
    }
}
