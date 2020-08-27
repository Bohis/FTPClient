using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPClient.Model {

	/// <summary>
	/// Класс логирующий деятельность программы
	/// </summary>
	class Log {

		/// <summary>
		/// Доступ к объекту, позволяет хранить его в единственном экземпляре
		/// </summary>
		private static Log _thisObject;

		/// <summary>
		/// Коллекция действий
		/// </summary>
		private ObservableCollection<(TypeLog, string)> _collectionLog = new ObservableCollection<(TypeLog, string)> ( );

		/// <summary>
		/// Делегат для событий новых элементов
		/// </summary>
		/// <param name="sender"></param>
		public delegate void BaseMod ( (TypeLog, string) sender );

		/// <summary>
		/// Новая ошибка
		/// </summary>
		public event BaseMod NewError;

		/// <summary>
		/// Новое действие
		/// </summary>
		public event BaseMod NewOperation;

		/// <summary>
		/// Конструктор
		/// </summary>
		public Log ( ) {
			_thisObject = this;
			_collectionLog = new ObservableCollection<(TypeLog, string)> ( );
		}

		/// <summary>
		/// Добавление записи
		/// </summary>
		/// <param name="value">Новая запись</param>
		public void AddNewValue ( (TypeLog, string) value ) {
			_collectionLog.Add ( value );

			/// Активация событий
			if ( value.Item1 == TypeLog.Error ) {
				NewError?.Invoke ( value );
			}
			else {
				NewOperation?.Invoke ( value );
			}
		}

		/// <summary>
		/// Доступ к объекту
		/// </summary>
		public static Log LogObject { get => _thisObject; }

		/// <summary>
		/// Коллекция записей
		/// </summary>
		public ObservableCollection<(TypeLog, string)> LogCollection { get => _collectionLog; }
	}
}