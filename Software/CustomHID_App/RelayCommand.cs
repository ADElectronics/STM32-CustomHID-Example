using System;
using System.Windows.Input;

namespace CustomHID_App
{
    public class RelayCommand : ICommand
    {
        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;

        #region Конструкторы
        public RelayCommand(Action<object> execute) : this(execute, null)
        {

        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }
        #endregion

        #region Методы
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
        #endregion
    }
}
