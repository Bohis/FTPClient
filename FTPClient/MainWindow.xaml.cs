using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;

namespace FTPClient {

	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		/// <summary>
		/// Класс представляющий логику приложения
		/// </summary>
		private Model.Client _ftpModel;
		/// <summary>
		/// Коллекция загруженных файлов
		/// </summary>
		private Model.FileInformation [] _fileArray;
		/// <summary>
		/// Коллекция элементов дерева
		/// </summary>
		private ObservableCollection<TreeViewItem> _dataCollection;
		/// <summary>
		/// Массив размеров шрифтов
		/// </summary>
		private int [] _fontSize;
		/// <summary>
		/// Строка с адресом на сервере 
		/// </summary>
		private string _pathDir;
		/// <summary>
		/// Коллекция элементов для контекстного меню
		/// </summary>
		private ObservableCollection<MenuItem> _contextMenuItems;

#if DEBUG
		/// <summary>
		/// Отладочная кнопка автозаполения данных
		/// </summary>
		private Button _buttonTest;
#endif

		/// <summary>
		/// Конструктор
		/// </summary>
		public MainWindow ( ) {
			InitializeComponent ( );

			_ftpModel = new Model.Client ( );
			new Model.Log ( );

			_fontSize = new int [ 50 ];
			for ( int i = 0; i < _fontSize.Length; ++i)
				_fontSize [ i ] = i;

			_dataCollection = new ObservableCollection<TreeViewItem> ( );

			ComboBoxFontSize.ItemsSource = _fontSize;
			ComboBoxFontSize.SelectedIndex = 20;

			ButtonConnect.Click += ButtonConnect_Click;
			ButtonClear.Click += ButtonClear_Click;
			ButtonDisconnect.Click += ButtonDisconnect_Click;

			TextBlockStatusStr.MouseEnter += TextBlockStatusStr_MouseEnter;
			CheckBoxUseSSL.Checked += CheckBoxUseSSL_Checked;
			MenuItemLoadFile.Click += MenuItemLoadFile_Click;

			MenuItemCreateDirectory.Click += MenuItemCreateDirectory_Click;

			Model.Log.LogObject.NewError += LogObject_NewError;
			Model.Log.LogObject.NewOperation += LogObject_NewOperation;

			DefaultValueForMoreInformation ( );

			CheckBoxIsVisibleSymbol.Checked += CheckBoxIsVisibleSymbol_Checked;
			CheckBoxIsVisibleSymbol.Unchecked += CheckBoxIsVisibleSymbol_Unchecked;
			TextBoxPassword.Visibility = Visibility.Collapsed;

			_pathDir = "";

			MenuCommandServer.IsEnabled = false;

			#region Контекстное меню
			_contextMenuItems = new ObservableCollection<MenuItem> ( );

			MenuItem delete = new MenuItem { Header = "Удалить"};
			delete.Click += Delete_Click;

			MenuItem download = new MenuItem { Header = "Скачать" };
			download.Click += Download_Click;

			_contextMenuItems.Add ( delete);
			_contextMenuItems.Add ( download);
			#endregion

#if DEBUG
			_buttonTest = new Button {
				Content = "Test",
				Height = 40,
				Width = 60,
				FontSize = 5
			};

			MenuMenuList.Items.Add ( _buttonTest );

			_buttonTest.Click += ButtonTest_Click;
#endif

		}

		/// <summary>
		/// Адрес внутри сервера
		/// </summary>
		private string PathDir {
			get => _pathDir;
			set {
				_pathDir = value.Replace( @"//", @"/" );
				TextBlockAddress.Text = "ftp://" + _ftpModel.Host + _pathDir;
			}
		}

		/// <summary>
		/// Подписчик на событие нового действия, запись данных в поля
		/// </summary>
		private void LogObject_NewOperation ( (Model.TypeLog, String) sender ) {
			TextBlockOperator.Text = sender.Item2;
		}

		/// <summary>
		/// Подписчик на событие ошибки, запись данных в поля
		/// </summary>
		private void LogObject_NewError ( (Model.TypeLog, String) sender ) {
			string error = sender.Item2;

			if ( error.Length > 20 ) {
				TextBlockStatusStr.Text = error.Substring ( 0, 17 ) + "...";
			}
			else {
				TextBlockStatusStr.Text = error;
			}
		}

		/// <summary>
		/// Доступ к записи/чтению ошибки
		/// </summary>
		private string LastErrorMessage {
			get {
				for ( int i = Model.Log.LogObject.LogCollection.Count - 1; i >= 0; --i ) {
					if ( Model.Log.LogObject.LogCollection [ i ].Item1 == Model.TypeLog.Error )
						return Model.Log.LogObject.LogCollection [ i ].Item2;
				}
				return "FTP server";
			}
			set {
#if DEBUG
				MessageBox.Show ( value,"Ошибка" );
#endif
				Model.Log.LogObject.AddNewValue ( (Model.TypeLog.Error,value) );
			}
		}

		/// <summary>
		/// Доступ к записи/чтению действия
		/// </summary>
		private string LastOperationMessage {
			get {
				for ( int i = Model.Log.LogObject.LogCollection.Count - 1; i > 0; --i ) {
					if ( Model.Log.LogObject.LogCollection [ i ].Item1 == Model.TypeLog.Operation )
						return Model.Log.LogObject.LogCollection [ i ].Item2;
				}
				return "FTP server";
			}
			set {
				Model.Log.LogObject.AddNewValue ( (Model.TypeLog.Operation, value) );
			}
		}

		/// <summary>
		/// Ввод ошибки/действия
		/// </summary>
		/// <param name="message">описание</param>
		/// <param name="type">тип</param>
		private void InputMessage (string message , Model.TypeLog type) {
			if ( type == Model.TypeLog.Error ) 
				LastErrorMessage = message;
			else
				LastOperationMessage = message;
		}

		/// <summary>
		///  Загрузить файл
		/// </summary>
		private void MenuItemLoadFile_Click ( Object sender, RoutedEventArgs e ) {
			try {
				OpenFileDialog form = new OpenFileDialog {
					Multiselect = true
				};

				if ( form.ShowDialog ( ) == true ) {
					foreach ( string s in form.FileNames ) {
						_ftpModel.UploadFile ( PathDir + "/", s);
					}
				}

				InputMessage ( "Файл загружен", Model.TypeLog.Operation );

				Update();
			}
			catch ( Exception error ) {
				InputMessage ( error.Message,Model.TypeLog.Error);
			}
		}

		/// <summary>
		/// Создать на сервере директорию
		/// </summary>
		private void MenuItemCreateDirectory_Click ( Object sender, RoutedEventArgs e ) {
			try {
				View.Dialog form = new View.Dialog ( "Название каталога", "Сохранить" );
				form.Closed += Form_Closed;
				form.ButtonEnter.Click += ButtonEnter_Click;
				this.IsEnabled = false;

				void Form_Closed ( Object senderNull, EventArgs eNull ) {
					this.IsEnabled = true;
				}
				string nameFolder = "";
				void ButtonEnter_Click ( Object senderNull, RoutedEventArgs eNull ) {
					nameFolder = form.TextBoxFiled.Text;
					form.Close ( );
					_ftpModel.CreateDirectory ( PathDir, nameFolder );
					Update ( );
				}
				form.Show ( );

				InputMessage ( "Директория создана", Model.TypeLog.Operation );
			}
			catch ( Exception error ) {
				InputMessage ( error.Message, Model.TypeLog.Error );
			}
		}

		/// <summary>
		/// Загрузить файл/директорию на компьютер
		/// </summary>
		private void Download_Click ( Object sender, RoutedEventArgs e ) {
			try {
				Model.FileInformation fileOrNot = GetObject ( ( (TreeViewItem)TreeViewExplorer.SelectedItem ).Header.ToString ( ) );

				WinForms.FolderBrowserDialog folderBrowser = new WinForms.FolderBrowserDialog ( );

				WinForms.DialogResult result = folderBrowser.ShowDialog ( );

				string path = folderBrowser.SelectedPath;

				if ( fileOrNot.IsDirectory ) {
					_ftpModel.DownloadFolder( PathDir, fileOrNot.ShortName, path );
				}
				else {
					_ftpModel.DownloadFile ( PathDir, fileOrNot.ShortName, path );
				}

				InputMessage ( "Загрузка закончена", Model.TypeLog.Operation );
			}
			catch ( Exception error ) {
				InputMessage ( error.Message, Model.TypeLog.Error );
			}
		}

		/// <summary>
		///  Удалить файл/директорию
		/// </summary>
		private void Delete_Click ( Object sender, RoutedEventArgs e ) {
			try {
				Model.FileInformation fileOrNot = GetObject ( ((TreeViewItem)TreeViewExplorer.SelectedItem).Header.ToString());

				if ( MessageBox.Show ( "Удалить выбранное", "Предупреждение", MessageBoxButton.OKCancel ) == MessageBoxResult.OK ) {
					if ( fileOrNot.IsDirectory ) {
						_ftpModel.RemoveDirectory ( PathDir + "/" + fileOrNot.ShortName );
					}
					else {
						_ftpModel.DeleteFile ( PathDir + "/" + fileOrNot.ShortName );
					}
				}
				InputMessage ( "Удаление прошло успешно", Model.TypeLog.Operation );
				Update();
			}
			catch ( Exception error ) {
				InputMessage ( error.Message, Model.TypeLog.Error );
			}
		}

		/// <summary>
		/// Возращение полей отвечабщих за описание к дефолтному значнеию
		/// </summary>
		private void DefaultValueForMoreInformation ( ) {
			TextBlockOwner.Text = "Владелец::: ";
			TextBlockIsDirectoty.Text = "Тип::: ";
			TextBlockCreateTime.Text = "Время создания::: ";
			TextBlockShrotName.Text = "Имя::: ";
		}

		/// <summary>
		/// Возвращает объект из коллекции
		/// </summary>
		/// <param name="name">короткое имя файла/папки</param>
		/// <returns>объект типа FileInformation</returns>
		private Model.FileInformation GetObject (string name ) {
			foreach ( Model.FileInformation obj in _fileArray ) {
				if ( obj.ShortName == name || obj.ShortName + "..." == name )
					return obj;
			}
			return null;
		}

		/// <summary>
		/// Выбор отдельного узла
		/// </summary>
		private void Sheet_Selected ( Object sender, RoutedEventArgs e ) {
			try {
				DefaultValueForMoreInformation ( );

				Model.FileInformation file = GetObject ( ( sender as TreeViewItem ).Header.ToString ( ) );

				TextBlockOwner.Text += file.Owner;
				TextBlockCreateTime.Text += file.CreateTime;
				TextBlockIsDirectoty.Text += file.IsDirectory ? "каталог" : "файл типа " + file.ShortName.Split ( '.' ) [ file.ShortName.Split ( '.' ).Length - 1 ];
				TextBlockShrotName.Text += file.ShortName;

				InputMessage ( "Объект " + file.ShortName + " выбран", Model.TypeLog.Operation );
			}
			catch ( Exception error ) {
				InputMessage ( error.Message, Model.TypeLog.Error );
			}
		}

		/// <summary>
		/// Вход в папку
		/// </summary>
		private void Sheet_MouseDoubleClick ( Object sender, MouseButtonEventArgs e ) {
			try {
				TreeViewItem dir = (TreeViewItem)sender;

				if ( dir.Header.ToString() == "..." ) {
					string result = "";

					for ( int i = 0; i < PathDir.Split ( '/' ).Length - 1; ++i ) {
						result += "/" + PathDir.Split ( '/' ) [ i ];
					}

					PathDir = result;
				}
				else {
					if ( !GetObject ( dir.Header.ToString ( ) ).IsDirectory )
						return;
					PathDir += "/" + dir.Header.ToString ( ).Remove ( dir.Header.ToString ( ).Length - 3, 3 );
				}

				_dataCollection.Clear ( );
				TreeViewItem sheet = new TreeViewItem {
					Header = "...",
				};
				_dataCollection.Add ( sheet);
				sheet.MouseDoubleClick += Sheet_MouseDoubleClick;

				_fileArray = _ftpModel.ListDirectory ( PathDir );

				foreach ( Model.FileInformation file in _fileArray ) {
					sheet = new TreeViewItem {
						Header = file.ShortName,
					};

					if ( file.IsDirectory )
						sheet.Header += "...";

					sheet.MouseDoubleClick += Sheet_MouseDoubleClick;
					sheet.Selected += Sheet_Selected;
					sheet.ContextMenu = new ContextMenu {ItemsSource = _contextMenuItems};

					_dataCollection.Add ( sheet );
				}

				InputMessage ( "Папка " + PathDir.Split('/')[ PathDir.Split ( '/' ) .Length - 1] + " загружена", Model.TypeLog.Operation );
			}
			catch ( Exception error ) {
				InputMessage ( error.Message, Model.TypeLog.Error );
			}
		}

		/// <summary>
		/// Обновить данные с сервера, в текущем адресе
		/// </summary>
		private void Update() {
			try {
				_dataCollection.Clear();

				TreeViewItem sheet = new TreeViewItem {
					Header = "...",
				};

				_dataCollection.Add( sheet );
				sheet.MouseDoubleClick += Sheet_MouseDoubleClick;

				_fileArray = _ftpModel.ListDirectory( PathDir );

				foreach (Model.FileInformation file in _fileArray) {
					sheet = new TreeViewItem {
						Header = file.ShortName,
					};

					if (file.IsDirectory)
						sheet.Header += "...";

					sheet.MouseDoubleClick += Sheet_MouseDoubleClick;
					sheet.Selected += Sheet_Selected;
					sheet.ContextMenu = new ContextMenu { ItemsSource = _contextMenuItems };

					_dataCollection.Add( sheet );
				}

				InputMessage( "Папка " + PathDir.Split( '/' )[ PathDir.Split( '/' ).Length - 1 ] + " загружена", Model.TypeLog.Operation );
			}
			catch (Exception error) {
				InputMessage( error.Message, Model.TypeLog.Error );
			}
		}

		/// <summary>
		/// Событие для использования SSL 
		/// </summary>
		private void CheckBoxUseSSL_Checked ( Object sender, RoutedEventArgs e ) {
			_ftpModel.UseSSL = (bool)CheckBoxUseSSL.IsChecked;
		}

		/// <summary>
		/// Отсоеденения от сервера
		/// </summary>
		private void ButtonDisconnect_Click ( Object sender, RoutedEventArgs e ) {
			try {
				TreeViewExplorer.ItemsSource = null;
				_ftpModel = new Model.Client ( );
				_fileArray = null;
				ChangeConnect ( );
				PathDir = "";
				InputMessage ( "Сеанс завершен", Model.TypeLog.Operation );
				TextBoxHost.IsEnabled = true;
				TextBoxLogin.IsEnabled = true;
				TextBoxPassword.IsEnabled = true;
				PasswordBoxPassword.IsEnabled = true;
				CheckBoxIsVisibleSymbol.IsEnabled = true;
				ButtonClear.IsEnabled = true;
				MenuCommandServer.IsEnabled = false;
			}
			catch ( Exception error ) {
				InputMessage ( error.Message, Model.TypeLog.Error );
			}
		}

		/// <summary>
		/// Контекстное меню
		/// </summary>
		private void TextBlockStatusStr_MouseEnter ( Object sender, MouseEventArgs e ) {
			TextBlockStatusStr.ToolTip = new ToolTip { Content = LastErrorMessage };
		}

#if DEBUG
		private void ButtonTest_Click ( Object sender, RoutedEventArgs e ) {
			TextBoxHost.Text = "localhost";
			TextBoxLogin.Text = "admin";
			TextBoxPassword.Text = "24312431";
			PasswordBoxPassword.Password = "24312431";
		}
#endif

		/// <summary>
		/// Кнопка отчистки полей
		/// </summary>
		private void ButtonClear_Click ( object sender, RoutedEventArgs e ) {
			TextBoxHost.Text = "";
			TextBoxLogin.Text = "";
			TextBoxPassword.Text = "";
		}

		/// <summary>
		/// Соеденение с сервером
		/// </summary>
		private void ButtonConnect_Click ( object sender, RoutedEventArgs e ) {
			try {
				CheckBoxUseSSL_Checked ( null,null);

				if ( TextBoxHost.Text == "" )
					throw new Exception ( "Пустой адрес сервера" );

				_ftpModel.Host = TextBoxHost.Text;
				_ftpModel.UserName = TextBoxLogin.Text;
				_ftpModel.Password = TextBoxPassword.Text;
				_fileArray = _ftpModel.ListDirectory ( "" );

				_dataCollection.Clear ( );
				TreeViewItem sheet = new TreeViewItem {
					Header = "...",
				};
				_dataCollection.Add ( sheet );
				sheet.MouseDoubleClick += Sheet_MouseDoubleClick;

				foreach ( Model.FileInformation file in _fileArray ) {
					sheet = new TreeViewItem {
						Header = file.ShortName,
					};

					if ( file.IsDirectory )
						sheet.Header += "...";

					sheet.MouseDoubleClick += Sheet_MouseDoubleClick;
					sheet.Selected += Sheet_Selected;

					sheet.ContextMenu = new ContextMenu { ItemsSource = _contextMenuItems };

					_dataCollection.Add ( sheet );
				}

				TreeViewExplorer.ItemsSource = _dataCollection;

				PathDir = "";

				InputMessage ( "Соеденение установлено", Model.TypeLog.Operation );
				ChangeConnect ( );
				TextBoxHost.IsEnabled = false;
				TextBoxLogin.IsEnabled = false;
				TextBoxPassword.IsEnabled = false;
				PasswordBoxPassword.IsEnabled = false;
				ButtonClear.IsEnabled = false;
				CheckBoxIsVisibleSymbol.IsEnabled = false;
				MenuCommandServer.IsEnabled = true;
			}
			catch ( Exception error ) {
				InputMessage ( error.Message, Model.TypeLog.Error );
			}
			return;
		}

		/// <summary>
		/// Коннект/дисконнект
		/// </summary>
		private void ChangeConnect ( ) {
			if ( ButtonConnect.Visibility == Visibility.Visible ) {
				ButtonDisconnect.Visibility = Visibility.Visible;
				ButtonConnect.Visibility = Visibility.Collapsed;
			}
			else {
				ButtonDisconnect.Visibility = Visibility.Collapsed;
				ButtonConnect.Visibility = Visibility.Visible;
			}
		}

		private void CheckBoxIsVisibleSymbol_Unchecked ( object sender, RoutedEventArgs e ) {
			TextBoxPassword.Text = PasswordBoxPassword.Password;
			TextBoxPassword.Visibility = Visibility.Visible;
			PasswordBoxPassword.Visibility = Visibility.Collapsed;
		}

		private void CheckBoxIsVisibleSymbol_Checked ( object sender, RoutedEventArgs e ) {
			PasswordBoxPassword.Password = TextBoxPassword.Text;
			TextBoxPassword.Visibility = Visibility.Collapsed;
			PasswordBoxPassword.Visibility = Visibility.Visible;
		}
	}
}