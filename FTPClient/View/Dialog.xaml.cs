using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FTPClient.View {
	/// <summary>
	/// Логика взаимодействия для Dialog.xaml
	/// </summary>
	public partial class Dialog : Window {
		public Dialog ( string title, string buttonContent, string textForBox = "" ) {
			InitializeComponent ( );

			TextBlockTitle.Text = title;
			ButtonEnter.Content = buttonContent;
			TextBoxFiled.Text = textForBox;
		}
	}
}