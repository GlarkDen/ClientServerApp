using System;
using System.Windows.Input;

namespace ClassLibrary
{
	/// <summary>
	/// Создание команды для биндинга в WPF
	/// </summary>
	public class MyCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;

		private Action<object> _execute;
		private Func<object, bool> _canExecute;

		public delegate bool canAction();
		private canAction _canAction;

		public delegate void action();
		private action _action;

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, new EventArgs());
		}

		public MyCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public MyCommand(action execute, Func<object, bool> canExecute)
		{
			_action = execute;
			_canExecute = canExecute;
		}

		public MyCommand(Action<object> execute, canAction canExecute)
		{
			_execute = execute;
			_canAction = canExecute;
		}

		public MyCommand(action execute, canAction canExecute)
		{
			_action = execute;
			_canAction = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			if (_canExecute == null)
				return CanExecute();
			else
				return _canExecute.Invoke(parameter);
		}

		public bool CanExecute()
		{
			return _canAction.Invoke();
		}

		public void Execute()
		{
			_action();
		}

		public void Execute(object parameter)
		{
			if (_execute == null)
				Execute();
			else
				_execute(parameter);
		}
	}
}
