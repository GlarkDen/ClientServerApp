using System;

namespace ClassLibrary.BaseClasses
{
    /// <summary>
    /// Клиент, отправивший запрос серверу
    /// </summary>
    public class MyClient
    {
        /// <summary>
        /// IP адрес (и порт)
        /// </summary>
        private string _ipAddress;

        /// <summary>
        /// Запрос
        /// </summary>
        private string _message;

		/// <summary>
		/// IP адрес (и порт)
		/// </summary>
		public string IpAddress
        {
            get => _ipAddress;
            set => _ipAddress = value;
        }

		/// <summary>
		/// Запрос
		/// </summary>
		public string Message
        {
            get => _message;
            set => _message = value;
        }

		/// <summary>
		/// Клиент, отправивший запрос серверу
		/// </summary>
		/// <param name="ipAddress">IP адрес (и порт) клиента</param>
		/// <param name="message">Запрос</param>
		public MyClient(string ipAddress, string message)
        {
            _ipAddress = ipAddress;
            _message = message;
        }

		/// <summary>
		/// Клиент, отправивший запрос серверу
		/// </summary>
		public MyClient()
        {

        }
    }
}
