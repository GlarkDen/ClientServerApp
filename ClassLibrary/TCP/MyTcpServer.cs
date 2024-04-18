using System;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Transactions;
using ClassLibrary.Interfaces;
using ClassLibrary.BaseClasses;

namespace ClassLibrary.TCP
{
	/// <summary>
	/// TCP сервер для обработки запросов от клиентов
	/// </summary>
	public class MyTcpServer : Server, IServer
    {
		/// <summary>
		/// Основной объект сервера, содержащий информацию о подключении
		/// </summary>
		private TcpListener _tcpListener;

		/// <summary>
		/// Максимальное число одновременно обрабатываемых запросов для этого типа сервера
		/// </summary>
		public static readonly int HighestRequestNumber = 150;

		/// <summary>
		/// TCP сервер для обработки запросов от клиентов
		/// </summary>
		/// <param name="checkMethod">Метод для обратоки запроса</param>
		/// <param name="ipAddress">IP адрес и порт</param>
		public MyTcpServer(CheckMethod checkMethod, IPEndPoint ipAddress) : base(checkMethod, ipAddress)
        {

		}

		/// <summary>
		/// TCP сервер для обработки запросов от клиентов
		/// </summary>
		/// <param name="checkMethod">Метод для обратоки запроса</param>
		public MyTcpServer(CheckMethod checkMethod) : base(checkMethod)
        {

		}

        /// <summary>
        /// Запуск сервера
        /// </summary>
        public void Start()
        {
            cancelTokenSource = new CancellationTokenSource();

            _status = true;
            OnChangedStatus(true);

			// Запускаем сервер в отдельном потоке без ожидания (и передаём ему cancelToken)
			server = Task.Run(async () =>
            {
                _tcpListener = new TcpListener(_ipAddress);

                // Пытаемся запустить сервер
                try
                {
                    _tcpListener.Start();
                    _status = true;

                    // Пока не получим Token отмены
                    while (true)
                    {
                        if (cancelTokenSource.IsCancellationRequested)
                            break;

                        // Ждём клиентов
                        var tcpClient = await _tcpListener.AcceptTcpClientAsync();

                        if (cancelTokenSource.IsCancellationRequested)
                        {
                            tcpClient.Close();
                            break;
                        }

                        // Создаем новую задачу для обслуживания клиента
                        _ = Task.Run(() => ProcessClientAsync(tcpClient), cancelTokenSource.Token);
                    }
                }
                finally
                {
                    _tcpListener.Stop();
                    _status = false;
                }
            }, cancelTokenSource.Token);
        }

        /// <summary>
        /// Остановка сервера
        /// </summary>
        public void Stop()
        {
            cancelTokenSource.Cancel();
            _tcpListener.Stop();

            _status = false;
			OnChangedStatus(false);
		}

        /// <summary>
        /// Обработка клиента
        /// </summary>
        /// <param name="tcpClient">Клиент</param>
        /// <returns></returns>
        private async Task ProcessClientAsync(TcpClient tcpClient)
        {
            MyClient client = new MyClient(tcpClient.Client.RemoteEndPoint.ToString(), "");

            var stream = tcpClient.GetStream();

            // Пока не получен токен отмены или не будет прислано сообщение о завершении "END"
            while (true)
            {
                // Ждём, пока в NetworkStream что-нибудь напишут
                while (tcpClient.Available == 0)
                {
                    if (cancelTokenSource.IsCancellationRequested)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                }

                if (cancelTokenSource.IsCancellationRequested)
                {
                    tcpClient.Close();
                    break;
                }

				#region Обрабатываем полученное сообщение

				// Создаём буфер для чтения сообщения
				var response = new List<byte>();
                int bytesRead;

				// Считываем данные до конечного символа (тут им принят \n)
				while ((bytesRead = stream.ReadByte()) != '\n')
                {
                    response.Add((byte)bytesRead);
                }

                var messageText = Encoding.UTF8.GetString(response.ToArray());

                client.Message = messageText;

                // Все данные клиент передал, завершаем работу с ним
                if (messageText == "END")
                {
                    break;
                }

                // Если превышено число запросов, отправляем текст запроса с припиской "-Ошибка"
				if (RequestCount >= _maxRequestCount)
                {
                    try
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(messageText + "-Ошибка" + '\n'));
                    }
                    catch
                    {
                        stream.Close();
                    }
                    continue;
                }
                else
                {
					try
					{
                        // Если запрос успешно передан в очередь, также сообщаем об этом
						await stream.WriteAsync(Encoding.UTF8.GetBytes(messageText + "-Окей" + '\n'));
					}
					catch
					{
						stream.Close();
					}
                }
				#endregion

				waitHandler.WaitOne();
                RequestCount++;
                OnGetRequest(client);
                waitHandler.Set();

				#region Создаём задачу на обработку запроса клиента
				new Thread(async () =>
                {
                    // Обрабатываем запрос
                    string result = messageText;
                    if (checkMethod.Invoke(messageText))
                        result += ":Да";
                    else
                        result += ":Нет";
                    
                    // Добавляем символ окончания сообщения 
                    result += '\n';

                    // Ждём добавочное время и пытаемся отправить ответ
                    await Task.Delay(_taskWorkTime);
                    try
                    {
                        await stream.WriteAsync(Encoding.UTF8.GetBytes(result));
                    }
                    catch 
                    { 
                        stream.Close();
                    }

                    response.Clear();

                    waitHandler.WaitOne();
                    RequestCount--;
                    OnCloseRequest(client);
                    waitHandler.Set();

                }).Start();
				#endregion
			}

			// Ждём немного, чтобы клиент точно всё прочитал из потока и закрываем соединение с ним
			Task.Delay(100).Wait();
            tcpClient.Close();
        }
    }
}
