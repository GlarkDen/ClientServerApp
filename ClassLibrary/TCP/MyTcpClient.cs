using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary.BaseClasses;
using ClassLibrary.Interfaces;

namespace ClassLibrary.TCP
{
    /// <summary>
    /// TCP клиент для взаимодействия с сервером
    /// </summary>
    public class MyTcpClient : Client, IClient
    {
		/// <summary>
		/// TCP клиент для взаимодействия с сервером
		/// </summary>
		/// <param name="ipAddress">IP адрес и порт</param>
		/// <param name="repeatDelayTime">Время до повторного запроса после получения ошибки</param>
		public MyTcpClient(IPEndPoint ipAddress, int repeatDelayTime = 2000) : base(ipAddress, repeatDelayTime)
        {

        }

		/// <summary>
		/// TCP клиент для взаимодействия с сервером
		/// </summary>
		/// <param name="repeatDelayTime">Время до повторного запроса после получения ошибки</param>
		public MyTcpClient(int repeatDelayTime = 2000) : base(repeatDelayTime)
        {

        }

        /// <summary>
        /// Запуск клиента
        /// </summary>
        /// <param name="messages">Список запросов для отправки на сервер</param>
		public async void Start(string[] messages)
        {
            // Количество необработанных запросов
            int unacceptedRequest = messages.Length;

            // Создаём очередь запросов
            Queue<string> messagesQueue = new();
            foreach (var item in messages)
                messagesQueue.Enqueue(item);

            // Запускаем клиента в отдельном потоке
            await Task.Run(async () =>
            {
                using (TcpClient tcpClient = new TcpClient())
                {
					#region Проверяем связь с сервером
					try
					{
						// OnGetRequestMessage служит для получения технической информации от сервера
                        // Здесь он также используется для передачи технической ннформации от клиента пользователю
						OnGetRequestMessage("Соединяюсь с сервером...");
						await tcpClient.ConnectAsync(_ipAddress.Address, _ipAddress.Port);
                    }
                    catch
                    {
                        waitHandler.WaitOne();
                        OnGetRequestMessage("Нет соединения");
                        waitHandler.Set();

						OnStoppedClient();

						return;
                    }
					#endregion

					var stream = tcpClient.GetStream();

                    answerHandler.Set();

					// Далее мы запускаем 2 задачи: одна отправляет данные на сервер, другая считывает все приходящие сообщения
					// Поток, который считыdает данные, по сути является управляющим

					#region Поток для отправки данных на сервер
					_ = Task.Run(async () =>
                    {
                        // Пока очередь сообщений не станет пуста
                        // (хотя можно было что-то более надёжное придумать, по типу токена отмены, но это тоже работает)
                        while (messagesQueue.Count > 0)
                        {
                            // За счёт этого события осуществляется основное управление потоком
                            answerHandler.WaitOne();

                            waitHandler.WaitOne();
                            string temp;

                            // Если очередь не стала пустой пока мы ждали waitHandler
                            // Её может опустошить управляющий поток при потере соединения с сервером
                            if (messagesQueue.Count > 0)
								// Удаляем запрос из очереди
								temp = messagesQueue.Dequeue();
                            else
                                break;

                            // Отправляем запрос на сервер (добавляем маркер окончания сообщения "\n")
                            OnGetRequestMessage("Отправка: " + temp);
                            byte[] data = Encoding.UTF8.GetBytes(temp + '\n');
                            waitHandler.Set();

							await stream.WriteAsync(data);

							// Ждём. В это время управлящий поток ждёт от сервера ответ о принятии запроса или перегрузке
							Task.Delay(100).Wait();
                        }
                    });
					#endregion

					// Второй, управляющий поток, который принимает все сообщения от сервера
					// Он работает немного быстрее предыдущего за счёт установки меньшего времени задержки

					#region Поток для чтения сообщений от сервера
					await Task.Run(async () =>
                    {
                        // Пока есть необработанные запросы
                        while (unacceptedRequest > 0)
                        {
                            // Ждём сообщений от сервера, если он долго не пишет - разрываем соединение
                            int waitingTime = 500;

                            while (tcpClient.Available == 0 && waitingTime > 0)
                            {
                                waitingTime--;

                                if (waitingTime == 250)
									OnGetRequestMessage("Ожидание ответа...");

								Task.Delay(10).Wait();
                            }

							// Создаём буфер для чтения данных
							var response = new List<byte>();
							int bytesRead;

							try
                            {
                                // Считываем данные до конечного символа
                                while ((bytesRead = stream.ReadByte()) != '\n')
                                {
                                    response.Add((byte)bytesRead);
                                }

                                var messageText = Encoding.UTF8.GetString(response.ToArray());

                                waitHandler.WaitOne();
                                OnGetRequestMessage("Получение: " + messageText);
                                waitHandler.Set();

                                // В случае ошибки добавляем запрос обратно в очередь
                                if (messageText.IndexOf("-Ошибка") != -1)
                                {
                                    waitHandler.WaitOne();
                                    messagesQueue.Enqueue(messageText.Substring(0, messageText.LastIndexOf('-')));
                                    waitHandler.Set();

                                    // Ждём некоторое время перед повторной отправкой
                                    await Task.Delay(RepeatDelayTime);
                                    answerHandler.Set();
                                }
                                // Если запрос обработан успешно, то посылаем информацию об этом в событии
                                else if (messageText.IndexOf(':') != -1)
                                {
                                    OnGetRequestAnswer(MyAnswer.DecodeServerAnswer(messageText));
                                    unacceptedRequest--;
                                }
                                // "Окей" означает, что запрос был учпешно добавлен в очередь на обработку
                                else if (messageText.IndexOf("-Окей") != -1)
                                {
                                    answerHandler.Set();
                                }
                                Task.Delay(10).Wait();
                            }
                            // Если сервер внезапно прервал подключение, то завершаем работу клиента
                            catch
                            {
                                waitHandler.WaitOne();
                                OnGetRequestMessage("Обрыв");
                                waitHandler.Set();

                                // Очищаем очередь, чтобы остановить второй поток
                                messagesQueue.Clear();
								answerHandler.Set();
								break;
                            }
                            response.Clear();
                        }
                    });
					#endregion

					try
					{
                        // Отправляем маркер завершения подключения - END
                        await stream.WriteAsync(Encoding.UTF8.GetBytes("END\n"));
                        Task.Delay(100).Wait();
                        stream.Close();
                        tcpClient.Close();
                    }
                    catch 
                    {
                        // Выполняется в случае, если было потеряно подключение к серверу
						OnGetRequestMessage("Задача отменена");
					}

					OnStoppedClient();
				}
            }, cancellationToken.Token);
		}
	}
}
