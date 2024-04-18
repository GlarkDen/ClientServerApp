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
			// ��������� ������ ������������ ������ � ��� ��������
			MyAnswer answer = new MyAnswer("���������", "���");
			
			Assert.Equal("���������", answer.Request);
			Assert.Equal("���", answer.Answer);
		}

		[Fact]
		public void DecodeAnswerTest()
		{
			// ��������� ����� �� ������������� ��������� �� �������
			// ��� ����� ��� "������:�����"
			string serverAnswer1 = "someText:���";
			string serverAnswer2 = "���:���:��";

			MyAnswer answer1 = MyAnswer.DecodeServerAnswer(serverAnswer1);
			MyAnswer answer2 = MyAnswer.DecodeServerAnswer(serverAnswer2);

			Assert.Equal("someText", answer1.Request);
			Assert.Equal("���", answer1.Answer);

			Assert.Equal("���:���", answer2.Request);
			Assert.Equal("��", answer2.Answer);
		}
	}
}
