using ClassLibrary.BaseClasses;
using System;
using Xunit;

namespace UnitTests
{
	public class MyAnswerTest
	{
		[Fact]
		public void ConstructorTest()
		{
			// Тестируем работу конструктора класса и его свойства
			MyAnswer answer = new MyAnswer("палиндром", "нет");
			
			Assert.Equal("палиндром", answer.Request);
			Assert.Equal("нет", answer.Answer);
		}

		[Fact]
		public void DecodeAnswerTest()
		{
			// Тестируем метод по декодированию сообщений от сервера
			// Они имеют вид "запрос:ответ"
			string serverAnswer1 = "someText:Нет";
			string serverAnswer2 = "мар:рам:Да";

			MyAnswer answer1 = MyAnswer.DecodeServerAnswer(serverAnswer1);
			MyAnswer answer2 = MyAnswer.DecodeServerAnswer(serverAnswer2);

			Assert.Equal("someText", answer1.Request);
			Assert.Equal("Нет", answer1.Answer);

			Assert.Equal("мар:рам", answer2.Request);
			Assert.Equal("Да", answer2.Answer);
		}
	}
}
