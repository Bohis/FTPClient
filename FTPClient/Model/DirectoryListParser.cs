using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace FTPClient.Model {

	/// <summary>
	/// Класс позволяющий осуществлять парсинг файлов на сервере
	/// </summary>
	public class DirectoryListParser {

		/// <summary>
		/// Коллекция файлов парсинга
		/// </summary>
		private ObservableCollection<FileInformation> _dataList;

		/// <summary>
		/// Метод возвращает ли только файлы либо каталоги из коллекции 
		/// </summary>
		/// <param name="isDirectory">true - катологи, false - файлы</param>
		/// <returns>коллекция определенных файлов</returns>
		private FileInformation [] FileOrDirArray ( bool isDirectory ) {
			try {
				ObservableCollection<FileInformation> list = new ObservableCollection<FileInformation> ( );
				foreach ( FileInformation obj in _dataList ) {
					if ( obj.IsDirectory == isDirectory )
						list.Add ( obj );
				}

				return list.ToArray ( );
			}
			catch ( Exception error ) {
				Log.LogObject.AddNewValue ( (TypeLog.Error, error.Message) );
				return null;
			}
		}

		/// <summary>
		/// Создание коллеции из полученных файлов
		/// </summary>
		/// <param name="data">данные с сервера</param>
		/// <returns>Коллекция данных</returns>
		private ObservableCollection<FileInformation> GetList ( string data ) {
			ObservableCollection<FileInformation> fileList = new ObservableCollection<FileInformation> ( );
			string [] dataRecords = data.Split ( '\n' );

			FileListStyle _directoryListStyle = GuessFileListStyle ( dataRecords );

			foreach ( string s in dataRecords ) {
				if ( _directoryListStyle != FileListStyle.Unknown && s != "" ) {
					FileInformation temp = new FileInformation {
						ShortName = ".",
						FullName = "."
					};

					switch ( _directoryListStyle ) {

						case FileListStyle.UnixStyle:
							temp = ParseFileStructFromUnixStyleRecord ( s );
							break;

						case FileListStyle.WindowsStyle:
							temp = ParseFileStructFromWindowsStyleRecord ( s );
							break;
					}

					if ( temp.FullName != "" && temp.FullName != "." && temp.FullName != ".." ) {
						temp.ConvertName ( );
						fileList.Add ( temp );
					}
				}
			}
			return fileList;
		}

		/// <summary>
		/// Парсиниг файлов для windows стиля записей
		/// </summary>
		/// <param name="record">запись</param>
		/// <returns>сформированый объект типа FileInformation</returns>
		private FileInformation ParseFileStructFromWindowsStyleRecord ( string record ) {
			FileInformation file = new FileInformation ( );

			string processstr = record.Trim ( );
			string dateStr = processstr.Substring ( 0, 8 );
			processstr = ( processstr.Substring ( 8, processstr.Length - 8 ) ).Trim ( );
			string timeStr = processstr.Substring ( 0, 7 );
			processstr = ( processstr.Substring ( 7, processstr.Length - 7 ) ).Trim ( );
			file.CreateTime = dateStr + " " + timeStr;

			if ( processstr.Substring ( 0, 5 ) == "<DIR>" ) {
				file.IsDirectory = true;
				processstr = ( processstr.Substring ( 5, processstr.Length - 5 ) ).Trim ( );
			}
			else {
				string [] strs = processstr.Split ( new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
				processstr = strs [ 1 ];
				file.IsDirectory = false;
			}

			file.FullName = processstr;
			file.ConvertName ( );
			return file;
		}

		/// <summary>
		/// Парсиниг файлов для unix стиля записей
		/// </summary>
		/// <param name="record">запись</param>
		/// <returns>сформированый объект типа FileInformation</returns>
		private FileInformation ParseFileStructFromUnixStyleRecord ( string record ) {
			FileInformation file = new FileInformation ( );

			if ( record [ 0 ] == '-' || record [ 0 ] == 'd' ) {
				string processstr = record.Trim ( );
				file.Flags = processstr.Substring ( 0, 9 );
				file.IsDirectory = ( file.Flags [ 0 ] == 'd' );
				processstr = ( processstr.Substring ( 11 ) ).Trim ( );
				CutSubstringFromStringWithTrim ( ref processstr, ' ', 0 );
				file.Owner = CutSubstringFromStringWithTrim ( ref processstr, ' ', 0 );
				file.CreateTime = GetCreateTimeString ( record );
				file.FullName = record.Replace ( "\n", "" ).Replace ( "\r", "" );
			}
			else {
				file.FullName = "";
			}
			return file;
		}

		/// <summary>
		/// Получение данных времени из записи
		/// </summary>
		/// <param name="record">запись</param>
		/// <returns>время создания файла</returns>
		private string GetCreateTimeString ( string record ) {
			string month = "(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)";
			string space = @"\s+";
			string day = "([0-9]|[0-3][0-9])";
			string year = "[1-2][0-9]{3}";
			string time = "[0-9]{1,2}:[0-9]{2}";
			Regex dateTimeRegex = new Regex ( month + space + day + space + "(" + year + "|" + time + ")", RegexOptions.IgnoreCase );
			Match match = dateTimeRegex.Match ( record );
			return match.Value;
		}

		/// <summary>
		/// Обрезка строки
		/// </summary>
		/// <param name="s">строка, с передачей данных по ссылки</param>
		/// <param name="c">символ разделитель</param>
		/// <param name="startIndex">позиция старта поиска</param>
		/// <returns>обрезанная строка</returns>
		private string CutSubstringFromStringWithTrim ( ref string s, char c, int startIndex ) {
			int pos1 = s.IndexOf ( c, startIndex );
			string retString = s.Substring ( 0, pos1 );
			s = ( s.Substring ( pos1 ) ).Trim ( );
			return retString;
		}

		/// <summary>
		/// Вся коллекция 
		/// </summary>
		public FileInformation [] FullListing { get => _dataList.ToArray ( ); }

		/// <summary>
		/// Все файлы
		/// </summary>
		public FileInformation [] FileListArray { get => FileOrDirArray ( false ); }

		/// <summary>
		/// Все каталоги
		/// </summary>
		public FileInformation [] DirectoryListArray { get => FileOrDirArray ( true ); }

		/// <summary>
		/// Конструктор
		/// </summary>
		/// <param name="response">данные запроса</param>
		public DirectoryListParser ( string response ) {
			_dataList = GetList ( response );
		}

		/// <summary>
		///  Данные, на какой ОС работает фтп-сервер
		/// </summary>
		/// <param name="recordList">лист записей</param>
		/// <returns>стиль сервера</returns>
		public FileListStyle GuessFileListStyle ( string [] recordList ) {
			foreach ( string s in recordList ) {
				if ( s.Length > 10 && Regex.IsMatch ( s.Substring ( 0, 10 ), "(-|d)((-|r)(-|w)(-|x)){3}" ) ) {
					return FileListStyle.UnixStyle;
				}
				else if ( s.Length > 8 && Regex.IsMatch ( s.Substring ( 0, 8 ), "[0-9]{2}-[0-9]{2}-[0-9]{2}" ) ) {
					return FileListStyle.WindowsStyle;
				}
			}
			return FileListStyle.Unknown;
		}
	}
}