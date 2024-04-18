using System;
using ClassLibrary.BaseClasses;

namespace ClassLibrary.Interfaces
{
	/// <summary>
	/// Сервер, обрабатывающий запросы клиентов
	/// </summary>
	public interface IServer
	{
		/// <summary>
		/// Изменение количества запросов
		/// </summary>
		public event Server.ChangeRequestCountEventHandler ChangeRequestCount;

		/// <summary>
		/// Получение нового запроса от клиента
		/// </summary>
		public event Server.RequestEventHandler GetRequest;

		/// <summary>
		/// Завершение обработки запроса от клиента
		/// </summary>
		public event Server.RequestEventHandler CloseRequest;

		/// <summary>
		/// Включение или отключение сервера
		/// </summary>
		public event Server.ChangedStatusEventHandler ChangedStatus;

		/// <summary>
		/// Максимальное число одновременно обрабатываемых запросов для данного типа сервера
		/// </summary>
		public static readonly int HighestRequestNumber;

		/// <summary>
		/// Дополнительное время обработки запроса
		/// </summary>
		public int TaskWorkTime { get; set; }

		/// <summary>
		/// Максимальное количество одновременно обрабатываемых запросов
		/// </summary>
		public int MaxRequestCount { get; set; }

		/// <summary>
		/// Метод для обработки запроса
		/// </summary>
		public Server.CheckMethod checkMethod { get; set; }

		/// <summary>
		/// Статус сервера: включён-true/выключен-false
		/// </summary>
		public bool Status { get; }

		/// <summary>
		/// Запуск сервера
		/// </summary>
		public void Start();

		/// <summary>
		/// Остановка сервера
		/// </summary>
		public void Stop();
	}
}
