using System;

namespace ClassLibrary.BaseClasses
{
    /// <summary>
    /// Ответ сервера на запрос клиента
    /// </summary>
    public class MyAnswer
    {
		/// <summary>
		/// Запрос
		/// </summary>
		private string _request;

		/// <summary>
		/// Ответ
		/// </summary>
		private string _answer;

        /// <summary>
        /// Запрос
        /// </summary>
        public string Request { get => _request; set => _request = value; }

        /// <summary>
        /// Ответ
        /// </summary>
        public string Answer { get => _answer; set => _answer = value; }

		/// <summary>
		/// Ответ сервера на запрос клиента
		/// </summary>
		public MyAnswer() { }

		/// <summary>
		/// Ответ сервера на запрос клиента
		/// </summary>
		/// <param name="request">Запрос</param>
		/// <param name="answer">Ответ</param>
		public MyAnswer(string request, string answer)
        {
            _request = request;
            _answer = answer;
        }

        /// <summary>
        /// Декодирование ответа сервера
        /// </summary>
        /// <param name="answer">Строка типа "запрос:ответ"</param>
        /// <returns></returns>
        public static MyAnswer DecodeServerAnswer(string answer)
        {
            // Ищем последнее вхождение ":" и разделяем по нему
            int position = answer.LastIndexOf(':');
            MyAnswer myAnswer = new MyAnswer();
            myAnswer.Request = answer.Substring(0, position);
            myAnswer.Answer = answer.Substring(position + 1);
            return myAnswer;
        }
    }
}
