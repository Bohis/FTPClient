using System;
using System.Collections.Generic;
using System.Net;
using System.IO;

namespace FTPClient.Model {

	/// <summary>
	/// Модель бизнес логики, обеспечивающая работу программы
	/// </summary>
	public class Client {

		/// <summary>
		/// Адрес сервера
		/// </summary>
		private string _host;

		/// <summary>
		/// Логин для сервера
		/// </summary>
		private string _userName;

		/// <summary>
		/// Пароль для сервера
		/// </summary>
		private string _password;

		/// <summary>
		/// Объект - запрос на сервер
		/// </summary>
		private FtpWebRequest _ftpRequest;

		/// <summary>
		/// Объект - ответ от сервера
		/// </summary>
		private FtpWebResponse _ftpResponse;

		/// <summary>
		/// Использовать SSL для щифровки данных
		/// </summary>
		private bool _useSSL = false;

		/// <summary>
		/// Стек для удаления данных с сервера (файл, адрес)
		/// </summary>
		private Stack<(FileInformation, string)> _stackForDelete;

		/// <summary>
		/// Рекурсивное получение всех файлов по указаному адресу
		/// </summary>
		/// <param name="path">адрес на сервере</param>
		private void AllFiles ( string path ) {
			FileInformation [] array = ListDirectory ( path );

			if ( array.Length != 0 ) {
				foreach ( FileInformation file in array ) {
					if ( file.IsDirectory ) {
						_stackForDelete.Push ( (file, path + "/" + file.ShortName) );
						AllFiles ( path + "/" + file.ShortName );
					}
					else {
						_stackForDelete.Push ( (file, path + "/" + file.ShortName) );
					}
				}
			}
		}

		/// <summary>
		/// Адрес сервера
		/// </summary>
		public string Host { get => _host; set => _host = value; }

		/// <summary>
		/// Логин
		/// </summary>
		public string UserName { get => _userName; set => _userName = value; }

		/// <summary>
		/// Пароль
		/// </summary>
		public string Password { get => _password; set => _password = value; }

		/// <summary>
		/// Использование SSL
		/// </summary>
		public bool UseSSL { get => _useSSL; set => _useSSL = value; }

		/// <summary>
		/// Метод получения файлов 
		/// </summary>
		/// <param name="path">адрес на сервере</param>
		/// <returns>коллекция файлов</returns>
		public FileInformation [] ListDirectory ( string path ) {
			if ( path == null || path == "" )
				path = "/";

			_ftpRequest = (FtpWebRequest)WebRequest.Create ( "ftp://" + _host + path );
			_ftpRequest.UsePassive = false;
			_ftpRequest.Credentials = new NetworkCredential ( _userName, _password );
			_ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
			_ftpRequest.EnableSsl = _useSSL;
			_ftpResponse = (FtpWebResponse)_ftpRequest.GetResponse ( );

			StreamReader sr = new StreamReader ( _ftpResponse.GetResponseStream ( ), System.Text.Encoding.ASCII );
			string content = sr.ReadToEnd ( );
			sr.Close ( );
			_ftpResponse.Close ( );

			DirectoryListParser parser = new DirectoryListParser ( content );
			return parser.FullListing;
		}

		/// <summary>
		/// Скачивание файлов с сервера
		/// </summary>
		/// <param name="path">адрес файла</param>
		/// <param name="fileName">имя файла</param>
		/// <param name="pathNewFile">локальный путь к файлу</param>
		public void DownloadFile ( string path, string fileName, string pathNewFile ) {
			_ftpRequest = (FtpWebRequest)WebRequest.Create ( "ftp://" + _host + path + "/" + fileName );

			_ftpRequest.Credentials = new NetworkCredential ( _userName, _password );
			_ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;

			_ftpRequest.EnableSsl = _useSSL;
			FileStream downloadedFile = new FileStream ( pathNewFile + "\\" + fileName, FileMode.Create, FileAccess.ReadWrite );

			_ftpResponse = (FtpWebResponse)_ftpRequest.GetResponse ( );
			Stream responseStream = _ftpResponse.GetResponseStream ( );

			byte [] buffer = new byte [ 1024 ];

			Int32 size;
			while ( ( size = responseStream.Read ( buffer, 0, 1024 ) ) > 0 ) {
				downloadedFile.Write ( buffer, 0, size );
			}

			_ftpResponse.Close ( );
			downloadedFile.Close ( );
			responseStream.Close ( );
		}

		/// <summary>
		/// Скачивание папки с сервера
		/// </summary>
		/// <param name="path">адрес папки на сервере</param>
		/// <param name="nameFolder">имя каталога</param>
		/// <param name="pathNewFolder">локальный путь</param>
		public void DownloadFolder ( string path, string nameFolder, string pathNewFolder ) {
			Directory.CreateDirectory ( pathNewFolder + "\\" + nameFolder );
			FileInformation [] array = ListDirectory ( path + "/" + nameFolder );

			foreach ( FileInformation obj in array ) {
				if ( obj.IsDirectory ) {
					DownloadFolder ( path + "/" + nameFolder, obj.ShortName, pathNewFolder + "\\" + nameFolder );
				}
				else {
					DownloadFile ( path + "/" + nameFolder, obj.ShortName, pathNewFolder + "\\" + nameFolder );
				}
			}
		}

		/// <summary>
		/// Загрузка файла
		/// </summary>
		/// <param name="path">путь загрузки</param>
		/// <param name="fileName">полное локальное имя файла </param>
		public void UploadFile ( string path, string fileName ) {
			string shortName = fileName.Remove ( 0, fileName.LastIndexOf ( "\\" ) + 1 );

			FileStream uploadedFile = new FileStream ( fileName, FileMode.Open, FileAccess.Read );

			_ftpRequest = (FtpWebRequest)WebRequest.Create ( "ftp://" + _host + path + shortName );
			_ftpRequest.Credentials = new NetworkCredential ( _userName, _password );
			_ftpRequest.EnableSsl = _useSSL;
			_ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

			byte [] file_to_bytes = new byte [ uploadedFile.Length ];
			uploadedFile.Read ( file_to_bytes, 0, file_to_bytes.Length );

			uploadedFile.Close ( );

			Stream writer = _ftpRequest.GetRequestStream ( );

			writer.Write ( file_to_bytes, 0, file_to_bytes.Length );
			writer.Close ( );
		}

		/// <summary>
		/// Удаление файла
		/// </summary>
		/// <param name="path">путь к файлу</param>
		public void DeleteFile ( string path ) {
			_ftpRequest = (FtpWebRequest)WebRequest.Create ( "ftp://" + _host + path );
			_ftpRequest.Credentials = new NetworkCredential ( _userName, _password );
			_ftpRequest.EnableSsl = _useSSL;
			_ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;

			FtpWebResponse ftpResponse = (FtpWebResponse)_ftpRequest.GetResponse ( );
			ftpResponse.Close ( );
		}

		/// <summary>
		/// Создание каталога на сервере
		/// </summary>
		/// <param name="path">путь создания</param>
		/// <param name="folderName">имя каталога</param>
		public void CreateDirectory ( string path, string folderName ) {
			FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create ( "ftp://" + _host + path + "/" + folderName );

			ftpRequest.Credentials = new NetworkCredential ( _userName, _password );
			ftpRequest.EnableSsl = _useSSL;
			ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

			FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse ( );
			ftpResponse.Close ( );
		}

		/// <summary>
		/// Удаление папки со всем содержимым
		/// </summary>
		/// <param name="path">путь к папке</param>
		public void RemoveDirectory ( string path ) {
			_stackForDelete = new Stack<(FileInformation, string)> ( );
			_stackForDelete.Push ( (new FileInformation ( "", "", true, "", "", "" ), path) );
			AllFiles ( path );

			while ( _stackForDelete.Count != 0 ) {
				if ( _stackForDelete.Peek ( ).Item1.IsDirectory ) {
					FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create ( "ftp://" + _host + _stackForDelete.Peek ( ).Item2 );
					ftpRequest.Credentials = new NetworkCredential ( _userName, _password );
					ftpRequest.EnableSsl = _useSSL;
					ftpRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;

					FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse ( );
					ftpResponse.Close ( );
				}
				else {
					DeleteFile ( _stackForDelete.Peek ( ).Item2 );
				}

				_stackForDelete.Pop ( );
			}
		}
	}
}