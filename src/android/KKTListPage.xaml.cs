using Microsoft.Maui.Controls;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает страницу оглавления приложения
	/// </summary>
	/*[XamlCompilation (XamlCompilationOptions. Compile)]*/
	public partial class KKTListPage: ContentPage
		{
		/// <summary>
		/// Конструктор. Запускает страницу
		/// </summary>
		public KKTListPage ()
			{
			InitializeComponent ();
			}

		// Исправление дефекта интерфейса MAUI, позволяющего обрушить приложение
		// нажатием системной кнопки Назад на главной странице. Применимо, соответственно,
		// только к главной странице
		protected override bool OnBackButtonPressed ()
			{
			return true;
			}
		}
	}
