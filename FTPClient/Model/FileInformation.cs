using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FTPClient.Model {

	/// <summary>
	/// Данные о файле с ftp сервера
	/// </summary>
	public class FileInformation {

		/// <summary>
		/// Флаги, полученные при дитальном запросе
		/// </summary>
		private string _flags;

		/// <summary>
		/// Владелец файла
		/// </summary>
		private string _owner;

		/// <summary>
		/// Является ли файл каталогом
		/// </summary>
		private bool _isDirectory;

		/// <summary>
		/// Дата создания файла
		/// </summary>
		private string _createTime;

		/// <summary>
		/// Полное имя файла (не адрес, а данные полученные из запроса)
		/// </summary>
		private string _fullName;

		/// <summary>
		/// Имя файла с расширением или каталога
		/// </summary>
		private string _shortName;

		/// <summary>
		/// Получение короткого имени из полного
		/// </summary>
		/// <param name="str">Полное имя</param>
		/// <returns>короткое имя</returns>
		private string ShortNameFromFullName ( string str ) {
			try {
				string result = "";
				Stack<string> namePart = new Stack<string> ( );

				for ( int i = str.Split ( ' ' ).Length - 1; i != 0; --i ) {
					if ( Regex.IsMatch ( str.Split ( ' ' ) [ i ], @"\d\d:\d\d" ) )
						break;
					namePart.Push ( str.Split ( ' ' ) [ i ] );
				}
				try {
					while ( namePart.Peek ( ) != null ) {
						result += namePart.Pop ( );
						if ( namePart.Peek ( ) != null )
							result += " ";
						else
							break;
					}
				}
				catch { }
				return result;
			}
			catch ( Exception error ) {
				Log.LogObject.AddNewValue ( (TypeLog.Error, "_error shortName " + _fullName + " " + error.Message + "_") );
				return "_error name_";
			}
		}

		/// <summary>
		/// Пустой конструктор
		/// </summary>
		public FileInformation ( ) {
			_flags = "";
			_owner = "";
			_isDirectory = false;
			_createTime = "";
			_shortName = "";
			_fullName = "";
		}

		/// <summary>
		/// Полный конструктор
		/// </summary>
		/// <param name="flags">Флаг файлов</param>
		/// <param name="owner">Владелец файла</param>
		/// <param name="isDirecory">Является ли файл деректорией</param>
		/// <param name="createTime">Время создания</param>
		/// <param name="shortName">Короткое имя</param>
		/// <param name="fullName">Полное имя</param>
		public FileInformation ( string flags, string owner, bool isDirecory, string createTime, string shortName, string fullName ) {
			_flags = flags;
			_owner = owner;
			_isDirectory = isDirecory;
			_createTime = createTime;
			_fullName = fullName;
			_shortName = shortName;
		}

		/// <summary>
		/// Полный конструктор, без ручного задания короткого имени
		/// </summary>
		/// <param name="flags">Флаг файлов</param>
		/// <param name="owner">Владелец файла</param>
		/// <param name="isDirecory">Является ли файл деректорией</param>
		/// <param name="createTime">Время создания</param>
		/// <param name="fullName">Полное имя</param>
		public FileInformation ( string flags, string owner, bool isDirecory, string createTime, string fullName ) {
			_flags = flags;
			_owner = owner;
			_isDirectory = isDirecory;
			_createTime = createTime;
			_fullName = fullName;
			_shortName = ShortNameFromFullName ( fullName );
		}

		/// <summary>
		/// Конвертировать имя файла
		/// </summary>
		public void ConvertName ( ) {
			_shortName = ShortNameFromFullName ( FullName );
		}

		/// <summary>
		/// Флаги
		/// </summary>
		public string Flags { get => _flags; set => _flags = value; }

		/// <summary>
		/// Владелец
		/// </summary>
		public string Owner { get => _owner; set => _owner = value; }

		/// <summary>
		/// Является ли файл деректорией
		/// </summary>
		public bool IsDirectory { get => _isDirectory; set => _isDirectory = value; }

		/// <summary>
		/// Время создания
		/// </summary>
		public string CreateTime { get => _createTime; set => _createTime = value; }

		/// <summary>
		/// Короткое имя
		/// </summary>
		public string ShortName { get => _shortName; set => _shortName = value; }

		/// <summary>
		/// Полное имя
		/// </summary>
		public string FullName { get => _fullName; set => _fullName = value; }
	}
}