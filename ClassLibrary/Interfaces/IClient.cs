using ClassLibrary.BaseClasses;

namespace ClassLibrary.Interfaces
{
	/// <summary>
	/// Клиент для взаимодействия с сервером
	/// </summary>
	public interface IClient
	{
		/// <summary>
		/// Получение ответа на запрос от сервера
		/// </summary>
		public event Client.GetAnswerEventHandler GetAnswer;

		/// <summary>
		/// Получение технической информации от сервера
		/// </summary>
		public event Client.GetRequestEventHandler GetRequest;

		/// <summary>
		/// Завершение работы клиента
		/// </summary>
		public event Client.StopedClientEventHandler StoppedClient;

		/// <summary>
		/// Время ожидания перед повторным запросом после ошибки (мс)
		/// </summary>
		public int RepeatDelayTime { get; set; }

		/// <summary>
		/// Запуск клиента
		/// </summary>
		/// <param name="messages">Список запросов для отправки на сервер</param>
		public void Start(string[] messages);
	}
}
