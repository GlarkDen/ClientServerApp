using System;

namespace Server.Models
{
	/// <summary>
	/// Обработка запросов от клиентов
	/// </summary>
	public static class RequestChecker
	{
		/// <summary>
		/// Проверяет, является ли текст палиндромом
		/// </summary>
		/// <param name="text">Текст</param>
		public static bool Palindrome(string text)
		{
			int len = text.Length;

			for (int i = 0; i < len; i++)
			{
				if (text[i] != text[len-i-1])
					return false;
			}

			return true;
		}
	}
}
