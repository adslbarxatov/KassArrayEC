using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает функционал приложения
	/// </summary>
	public partial class App: Application
		{
		#region Настройки стилей отображения

		private Color aboutMasterBackColor = Color.FromArgb ("#F0FFF0");
		private Color aboutFieldBackColor = Color.FromArgb ("#D0FFD0");
		private Color kktListMasterBackColor = Color.FromArgb ("#F0FFF8");
		private Color kktListFieldBackColor = Color.FromArgb ("#E0FFF0");
		private Color kktSettingsMasterBackColor = Color.FromArgb ("#FFFFF0");
		private Color kktSettingsFieldBackColor = Color.FromArgb ("#FFFFD0");
		private Color settingsMasterBackColor = Color.FromArgb ("#F0FFFF");
		private Color settingsFieldBackColor = Color.FromArgb ("#D0FFFF");

		#endregion

		#region Переменные страниц

		private ContentPage kktListPage, kktInfoPage, kktSettingsPage, settingsPage, aboutPage;

		private Label fontSizeField, countLabel, infoLabel, yellowThresholdField, redThresholdField,
			kktSerialLabel, kktPlacementLabel,
			fnExpirationDateLabel, fnExpirationFlagLabel, ofdVariantLabel, ofdExpirationDateLabel,
			fnEvaluatedLengthLabel1, fnEvaluatedLengthLabel2, ofdEvaluatedLengthLabel1, ofdEvaluatedLengthLabel2;

		private Button menuButton, updateButton, removeButton, addToSameOwnerButton,
			updateContactsButton, findButton, findNextButton, fnExpirationDateFromCBButton,
			ofdVariantButton, ofdExpirationDateFromCBButton, applyButton, fnEvaluatedLengthField,
			ofdEvaluatedLengthField;

		private Editor kktSerialField, kktOwnerField, kktOwnerINNField, kktPlacementField,
			kktOwnerContactsField;

		private DatePicker fnExpirationDateField, ofdExpirationDateField;

		private Switch fnExpirationFlag, fnEvaluatedLengthFlag, ofdEvaluatedLengthFlag;

		private StackLayout kktListField, phonesField;
		private ScrollView kktListContainer, kktSettingsContainer;

		#endregion

		#region Основные переменные

		// Опорные классы
		private KnowledgeBase kb;

		// Список сохранённых реквизитов
		private KAECList kl;

		// Текущая выбранная строка
		private uint selectedIndex = 0;

		// Список вариантов меню
		private List<string> menuVariants = [];
		private List<string> ofdVariants = [];
		private int ofdVariant;
		private List<string> fnLiveVariants = [];

		// Последний указанный критерий поиска
		private string lastSearchCriteria = "";

		// Реле режима редактирования
		private bool createWithSameOwner = false;
		private bool createFromScratch = false;
		private bool editOwnerData = false;

		#endregion

		#region Запуск и настройка

		/// <summary>
		/// Конструктор. Точка входа приложения
		/// </summary>
		public App ()
			{
			// Инициализация
			InitializeComponent ();
			}

		// Замена определению MainPage = new MasterPage ()
		protected override Window CreateWindow (IActivationState activationState)
			{
			return new Window (AppShell ());
			}

		// Инициализация интерфейса
		private Page AppShell ()
			{
			Page mainPage = new MasterPage ();
			RDAppStartupFlags flags = RDGenerics.GetAppStartupFlags (RDAppStartupFlags.DisableXPUN);

			kb = new KnowledgeBase ();
			kl = new KAECList ();

			#region Общая конструкция страниц приложения

			kktListPage = RDInterface.ApplyPageSettings (new KKTListPage (),
				"Список наблюдения", kktListMasterBackColor);
			kktInfoPage = RDInterface.ApplyPageSettings (new KKTInfoPage (),
				"Информация о ККТ", kktListMasterBackColor);
			kktSettingsPage = RDInterface.ApplyPageSettings (new KKTSettingsPage (),
				"Параметры ККТ", kktSettingsMasterBackColor);
			settingsPage = RDInterface.ApplyPageSettings (new SettingsPage (),
				"Настройки программы", settingsMasterBackColor);
			aboutPage = RDInterface.ApplyPageSettings (new AboutPage (),
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_AppAbout),
				aboutMasterBackColor);

			RDInterface.SetMasterPage (mainPage, kktListPage, kktListMasterBackColor);
			DeviceDisplay.Current.MainDisplayInfoChanged += Current_MainDisplayInfoChanged;
			RDInterface.MasterPage.Popped += Current_LogPagePopped;

			#endregion

			#region Список ККТ

			kktListField = (StackLayout)kktListPage.FindByName ("KKTListField");
			kktListContainer = (ScrollView)kktListPage.FindByName ("KKTListContainer");

			menuButton = RDInterface.ApplyButtonSettings (kktListPage, "MenuButton", RDDefaultButtons.Menu, kktListFieldBackColor,
				MenuButton_Click, true);
			RDInterface.ApplyButtonSettings (kktListPage, "AddKKTButton", RDDefaultButtons.Increase, kktListFieldBackColor,
				AddKKTButton_Click, true);

			countLabel = RDInterface.ApplyLabelSettings (kktListPage, "CountLabel", " ", RDLabelTypes.HeaderCenter);
			countLabel.HorizontalOptions = LayoutOptions.Fill;
			countLabel.Padding = new Thickness (3);

			findButton = RDInterface.ApplyButtonSettings (kktListPage, "FindButton", RDDefaultButtons.Find,
				kktListFieldBackColor, SearchButton_Click, true);

			#endregion

			#region Информация о ККТ

			infoLabel = RDInterface.ApplyLabelSettings (kktInfoPage, "InfoLabel", " ", RDLabelTypes.HeaderLeft);

			updateButton = RDInterface.ApplyButtonSettings (kktInfoPage, "UpdateButton", "Обновить данные",
				kktListFieldBackColor, UpdateButton_Click);
			removeButton = RDInterface.ApplyButtonSettings (kktInfoPage, "RemoveButton", "Удалить ККТ",
				kktListFieldBackColor, RemoveButton_Click);

			addToSameOwnerButton = RDInterface.ApplyButtonSettings (kktInfoPage, "AddToSameOwnerButton",
				"Добавить ККТ к этому же пользователю", kktListFieldBackColor, AddToSameOwner_Click);
			updateContactsButton = RDInterface.ApplyButtonSettings (kktInfoPage, "UpdateContactsButton",
				"Обновить реквизиты владельца", kktListFieldBackColor, UpdateContactsButton_Click);

			findNextButton = RDInterface.ApplyButtonSettings (kktInfoPage, "FindNextButton", "Найти далее",
				kktListFieldBackColor, SearchButton_Click);

			phonesField = (StackLayout)kktInfoPage.FindByName ("PhonesField");

			#endregion

			#region Страница "О программе"

			RDInterface.ApplyLabelSettings (aboutPage, "AboutLabel",
				RDGenerics.AppAboutLabelText, RDLabelTypes.AppAbout);

			RDInterface.ApplyButtonSettings (aboutPage, "ManualsButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_ReferenceMaterials),
				aboutFieldBackColor, ReferenceButton_Click);
			RDInterface.ApplyButtonSettings (aboutPage, "HelpButton",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_HelpSupport),
				aboutFieldBackColor, HelpButton_Click);

			#endregion

			#region Настройки приложения

			RDInterface.ApplyLabelSettings (settingsPage, "RestartTipLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Message_RestartRequired),
				RDLabelTypes.TipCenter);

			RDInterface.ApplyLabelSettings (settingsPage, "FontSizeLabel",
				RDLocale.GetDefaultText (RDLDefaultTexts.Control_InterfaceFontSize),
				RDLabelTypes.DefaultLeft);
			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeInc",
				RDDefaultButtons.Increase, settingsFieldBackColor, FontSizeButton_Clicked, true);
			RDInterface.ApplyButtonSettings (settingsPage, "FontSizeDec",
				RDDefaultButtons.Decrease, settingsFieldBackColor, FontSizeButton_Clicked, true);
			fontSizeField = RDInterface.ApplyLabelSettings (settingsPage, "FontSizeField",
				" ", RDLabelTypes.DefaultCenter);

			RDInterface.ApplyLabelSettings (settingsPage, "ThresholdTipLabel",
				"Следующие настройки будут применены при обновлении списка отслеживаемых ККТ",
				RDLabelTypes.TipCenter);

			RDInterface.ApplyLabelSettings (settingsPage, "YellowThresholdLabel",
				"Порог жёлтого предупреждения, дней:", RDLabelTypes.DefaultLeft);
			RDInterface.ApplyButtonSettings (settingsPage, "YellowThresholdInc",
				RDDefaultButtons.Increase, settingsFieldBackColor, YellowThresholdButton_Clicked, true);
			RDInterface.ApplyButtonSettings (settingsPage, "YellowThresholdDec",
				RDDefaultButtons.Decrease, settingsFieldBackColor, YellowThresholdButton_Clicked, true);
			RDInterface.ApplyLabelSettings (settingsPage, "YellowThresholdTip",
				"ККТ, у которых до истечения срока жизни ФН или тарифа ОФД осталось меньше указанного " +
				"здесь количества дней, будут помечены жёлтым цветом",
				RDLabelTypes.TipJustify);

			yellowThresholdField = RDInterface.ApplyLabelSettings (settingsPage, "YellowThresholdField",
				" ", RDLabelTypes.DefaultCenter);
			yellowThresholdField.FontSize = menuButton.FontSize;
			YellowThresholdButton_Clicked (null, null);

			RDInterface.ApplyLabelSettings (settingsPage, "RedThresholdLabel",
				"Порог красного предупреждения, дней:", RDLabelTypes.DefaultLeft);
			RDInterface.ApplyButtonSettings (settingsPage, "RedThresholdInc",
				RDDefaultButtons.Increase, settingsFieldBackColor, RedThresholdButton_Clicked, true);
			RDInterface.ApplyButtonSettings (settingsPage, "RedThresholdDec",
				RDDefaultButtons.Decrease, settingsFieldBackColor, RedThresholdButton_Clicked, true);
			RDInterface.ApplyLabelSettings (settingsPage, "RedThresholdTip",
				"ККТ, у которых до истечения срока жизни ФН или тарифа ОФД осталось меньше указанного " +
				"здесь количества дней, будут помечены красным цветом",
				RDLabelTypes.TipJustify);

			redThresholdField = RDInterface.ApplyLabelSettings (settingsPage, "RedThresholdField",
				" ", RDLabelTypes.DefaultCenter);
			redThresholdField.FontSize = menuButton.FontSize;
			RedThresholdButton_Clicked (null, null);

			RDInterface.ApplyLabelSettings (settingsPage, "FontSizeTipLabel",
				"Размер шрифта интерфейса влияет на все элементы в приложении. Измените его, если " +
				"автоматическое масштабирование не дало желаемого результата", RDLabelTypes.TipJustify);
			FontSizeButton_Clicked (null, null);

			#endregion

			#region Настройки ККТ

			kktSettingsContainer = (ScrollView)kktSettingsPage.FindByName ("KKTSettingsContainer");

			kktSerialLabel = RDInterface.ApplyLabelSettings (kktSettingsPage, "KKTSerialLabel",
				"Заводской номер ККТ:", RDLabelTypes.DefaultLeft);
			kktSerialField = RDInterface.ApplyEditorSettings (kktSettingsPage, "KKTSerialField",
				kktSettingsFieldBackColor, Keyboard.Numeric, 20, "", null, true);
			kktSerialField.FontFamily = RDGenerics.MonospaceFont;

			RDInterface.ApplyLabelSettings (kktSettingsPage, "KKTOwnerLabel",
				"Владелец ККТ:", RDLabelTypes.DefaultLeft);
			kktOwnerField = RDInterface.ApplyEditorSettings (kktSettingsPage, "KKTOwnerField",
				kktSettingsFieldBackColor, Keyboard.Text, 50, "", null, true);

			RDInterface.ApplyLabelSettings (kktSettingsPage, "KKTOwnerINNLabel",
				"ИНН владельца:", RDLabelTypes.DefaultLeft);
			kktOwnerINNField = RDInterface.ApplyEditorSettings (kktSettingsPage, "KKTOwnerINNField",
				kktSettingsFieldBackColor, Keyboard.Numeric, 12, "", null, true);
			kktOwnerINNField.FontFamily = RDGenerics.MonospaceFont;

			kktPlacementLabel = RDInterface.ApplyLabelSettings (kktSettingsPage, "KKTPlacementLabel",
				"Местоположение ККТ:", RDLabelTypes.DefaultLeft);
			kktPlacementField = RDInterface.ApplyEditorSettings (kktSettingsPage, "KKTPlacementField",
				kktSettingsFieldBackColor, Keyboard.Text, 150, "", null, true);

			RDInterface.ApplyLabelSettings (kktSettingsPage, "KKTOwnerContactsLabel",
				"Контактные данные:", RDLabelTypes.DefaultLeft);
			kktOwnerContactsField = RDInterface.ApplyEditorSettings (kktSettingsPage, "KKTOwnerContactsField",
				kktSettingsFieldBackColor, Keyboard.Text, 100, "", null, true);

			fnExpirationDateLabel = RDInterface.ApplyLabelSettings (kktSettingsPage, "FNExpirationDateLabel",
				"Срок жизни ФН:", RDLabelTypes.DefaultLeft);
			fnExpirationDateField = RDInterface.ApplyDatePickerSettings (kktSettingsPage, "FNExpirationDateField",
				kktSettingsFieldBackColor, null);
			fnExpirationDateFromCBButton = RDInterface.ApplyButtonSettings (kktSettingsPage, "FNExpirationDateFromCBButton",
				RDDefaultButtons.Left, kktSettingsFieldBackColor, FNDateFromCB_Click, true);

			fnEvaluatedLengthLabel1 = RDInterface.ApplyLabelSettings (kktSettingsPage, "FNEvaluatedLengthLabel", "активирован на",
				RDLabelTypes.DefaultLeft);
			fnEvaluatedLengthFlag = RDInterface.ApplySwitchSettings (kktSettingsPage, "FNEvaluatedLengthFlag",
				false, kktSettingsFieldBackColor, FNEvaluatedLengthFlag_CheckedChanged, true);
			fnEvaluatedLengthLabel2 = RDInterface.ApplyLabelSettings (kktSettingsPage, "FNEvaluatedLengthLabelEnd", "дней",
				RDLabelTypes.DefaultLeft);
			fnEvaluatedLengthField = RDInterface.ApplyButtonSettings (kktSettingsPage, "FNEvaluatedLengthField",
				KAECList.FNLiveLengths[0].ToString (), kktSettingsFieldBackColor, FNEvaluatedField_Clicked);

			fnExpirationFlagLabel = RDInterface.ApplyLabelSettings (kktSettingsPage, "FNExpirationFlagLabel",
				"Не отслеживать данную ККТ", RDLabelTypes.DefaultLeft);
			fnExpirationFlag = RDInterface.ApplySwitchSettings (kktSettingsPage, "FNExpirationFlag",
				false, kktSettingsFieldBackColor, null, false);

			ofdVariantLabel = RDInterface.ApplyLabelSettings (kktSettingsPage, "OFDVariantLabel",
				"Режим работы с ОФД:", RDLabelTypes.DefaultLeft);
			ofdVariantButton = RDInterface.ApplyButtonSettings (kktSettingsPage, "OFDVariantButton",
				" ", kktSettingsFieldBackColor, OFDVariantButton_Click);

			ofdExpirationDateLabel = RDInterface.ApplyLabelSettings (kktSettingsPage, "OFDExpirationDateLabel",
				"Срок тарифа ОФД:", RDLabelTypes.DefaultLeft);
			ofdExpirationDateField = RDInterface.ApplyDatePickerSettings (kktSettingsPage, "OFDExpirationDateField",
				kktSettingsFieldBackColor, null);
			ofdExpirationDateFromCBButton = RDInterface.ApplyButtonSettings (kktSettingsPage, "OFDExpirationDateFromCBButton",
				RDDefaultButtons.Left, kktSettingsFieldBackColor, OFDDateFromCB_Click, true);

			ofdEvaluatedLengthLabel1 = RDInterface.ApplyLabelSettings (kktSettingsPage, "OFDEvaluatedLengthLabel", "активирован на",
				RDLabelTypes.DefaultLeft);
			ofdEvaluatedLengthFlag = RDInterface.ApplySwitchSettings (kktSettingsPage, "OFDEvaluatedLengthFlag",
				false, kktSettingsFieldBackColor, OFDEvaluatedLengthFlag_CheckedChanged, true);
			ofdEvaluatedLengthLabel2 = RDInterface.ApplyLabelSettings (kktSettingsPage, "OFDEvaluatedLengthLabelEnd", "дней",
				RDLabelTypes.DefaultLeft);
			ofdEvaluatedLengthField = RDInterface.ApplyButtonSettings (kktSettingsPage, "OFDEvaluatedLengthField",
				"1", kktSettingsFieldBackColor, OFDEvaluatedField_Clicked);

			applyButton = RDInterface.ApplyButtonSettings (kktSettingsPage, "ApplyButton", " ",
				RDInterface.GetInterfaceColor (RDInterfaceColors.SuccessMessage), ApplyButton_Click);

			#endregion

			// Загрузка списка
			ReloadList ();

			// Обязательное принятие Политики и EULA
			AcceptPolicy (flags.HasFlag (RDAppStartupFlags.DisableXPUN));
			return mainPage;
			}

		// Контроль принятия Политики и EULA
		private static async void AcceptPolicy (bool DisableXPUN)
			{
			// Контроль XPUN
			if (!DisableXPUN)
				await RDInterface.XPUNLoop ();

			// Политика
			await RDInterface.PolicyLoop ();

			if (RDGenerics.TipsState != 0)
				return;
			
			// Только после принятия
			await RDInterface.ShowMessage ("Вас приветствует " + ProgramDescription.AssemblyMainName +
				" – " + ProgramDescription.AssemblyDescription + RDLocale.RNRN +
				"Данный инструмент позволяет отслеживать и своевременно реагировать на истекающие сроки жизни " +
				"ФН и тарифы ОФД. Список ККТ на главной странице приложения автоматически сортируется таким образом, " +
				"чтобы ККТ, требующие внимания, всегда находились в самом его начале. А цветовая индикация не позволит " +
				"пропустить важные события",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));

			RDGenerics.TipsState = 0x0001;
			}

		/// <summary>
		/// Запуск интерфейса
		/// </summary>
		protected override void OnStart ()
			{
			Current_MainDisplayInfoChanged (null, null);

			base.OnStart ();
			}

		/// <summary>
		/// Возврат в интерфейс при сворачивании
		/// </summary>
		protected override void OnResume ()
			{
			RDInterface.MasterPage.PopToRootAsync (true);

			Current_MainDisplayInfoChanged (null, null);

			base.OnResume ();
			}

		/// <summary>
		/// Возврат в интерфейс из статичного оповещения (использует перенаправление в MasterPage)
		/// </summary>
		public void ResumeApp ()
			{
			OnResume ();
			}

		// Изменение ориентации экрана
		private async void Current_MainDisplayInfoChanged (object sender, DisplayInfoChangedEventArgs e)
			{
			await Task.Delay (500);

			double height = RDInterface.MasterPage.CurrentPage.Height - RDGenerics.NavigationBarsSize;
			kktListContainer.HeightRequest = kktListContainer.MaximumHeightRequest =
				height - menuButton.Height - countLabel.Height - 20;
			kktSettingsContainer.HeightRequest = kktSettingsContainer.MaximumHeightRequest =
				height - applyButton.Height;

			/*RDInterface.ShowBalloon (height.ToString (), true);*/
			}

		private async void Current_LogPagePopped (object sender, NavigationEventArgs e)
			{
			Current_MainDisplayInfoChanged (null, null);
			}

		#endregion

		#region Страница списка ККТ

		// Вызов меню программы
		private async void MenuButton_Click (object sender, EventArgs e)
			{
			// Запрос варианта
			if (menuVariants.Count < 1)
				{
				menuVariants.Add ("⬆️\t Загрузить из файла");
				menuVariants.Add ("⬇️\t Сохранить в файл");
				menuVariants.Add ("⚙️\t Настройки приложения");
				menuVariants.Add ("ℹ️\t Справка и поддержка");
				}

			int res = await RDInterface.ShowList ("Меню", RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				menuVariants);
			if (res < 0)
				return;

			// Разбор
			switch (res)
				{
				// Загрузка файла
				case 0:
					string inFile = await RDGenerics.LoadFromFile (RDEncodings.UTF8);
					if (string.IsNullOrWhiteSpace (inFile))
						{
						RDInterface.ShowBalloon ("Указанный файл не содержит данных для загрузки", true);
						return;
						}

					if (kl.ImportExchangeData (inFile) != CfgExchangeResults.ImportOk)
						{
						await RDInterface.ShowMessage ("Не удалось загрузить файл обмена настройками." + RDLocale.RNRN +
							"Возможно, выбранный файл имел версию, не поддерживаемую Android-приложением. " +
							"Попробуйте заново сформировать его с помощью Windows-клиента и повторите попытку",
							RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
						return;
						}

					ReloadList ();
					RDInterface.ShowBalloon ("Файл успешно загружен", false);
					break;

				// Сохранение файла
				case 1:
					string outFile = kl.ExportDataForExchange ();

					await RDGenerics.SaveToFile ("Данные для обмена " + ProgramDescription.AssemblyMainName +
						ProgramDescription.KassArrayECAlias + ".cfg", outFile, RDEncodings.UTF8);
					break;

				// Настройки
				case 2:
					RDInterface.SetCurrentPage (settingsPage, settingsMasterBackColor);
					break;

				// О приложении
				case 3:
					RDInterface.SetCurrentPage (aboutPage, aboutMasterBackColor);
					break;
				}
			}

		// Добавление новой ККТ
		private void AddKKTButton_Click (object sender, EventArgs e)
			{
			createFromScratch = true;
			createWithSameOwner = false;
			editOwnerData = false;

			RunRecordEdition ();
			}

		// Перезагрузка списка ККТ
		private void ReloadList ()
			{
			// Сброс списка
			kktListField.Children.Clear ();

			if (kl.ItemsCount < 1)
				selectedIndex = 0;
			else if (selectedIndex >= kl.ItemsCount)
				selectedIndex = kl.ItemsCount - 1;

			uint yWarnings = 0;
			uint rWarnings = 0;
			uint yellowTs = KAECList.YellowWarningThreshold;
			uint redTs = KAECList.RedWarningThreshold;

			// Формирование контролов
			for (uint i = 0; i < kl.ItemsCount; i++)
				{
				KAECFoundRequisites? v = kl.GetRequisites (i);
				KAECFoundRequisites fr = v.Value;
				string model = kb.KKTNumbers.GetKKTModel (fr.KKTSerial);

				// Сборка контрола
				Button b = new Button ();
				RDInterface.ApplyButtonDefaults (b, true);

				if (kl.GetNoControlStatus (i))
					{
					b.BackgroundColor = RDInterface.GetInterfaceColor (RDInterfaceColors.MediumGrey);
					}
				else if ((kl.GetDaysToFNExpiration (i) < redTs) || (kl.GetDaysToOFDExpiration (i) < redTs))
					{
					b.BackgroundColor = RDInterface.GetInterfaceColor (RDInterfaceColors.ErrorMessage);
					rWarnings++;
					}
				else if ((kl.GetDaysToFNExpiration (i) < yellowTs) || (kl.GetDaysToOFDExpiration (i) < yellowTs))
					{
					b.BackgroundColor = RDInterface.GetInterfaceColor (RDInterfaceColors.WarningMessage);
					yWarnings++;
					}
				else
					{
					b.BackgroundColor = RDInterface.GetInterfaceColor (RDInterfaceColors.SuccessMessage);
					}

				/*b.TextColor = RDInterface.GetInterfaceColor (RDInterfaceColors.DefaultText);*/
				b.Clicked += KKTList_ButtonClicked;
				b.Margin = new Thickness (6);
				b.FontSize = menuButton.FontSize;

				b.Text = model + RDLocale.RN;
				if (fr.IsINNSet)
					b.Text += fr.KKTOwner;
				else
					b.Text += "[ИНН не задан] " + fr.KKTOwner;

				// Добавление
				kktListField.Children.Add (b);
				}

			// Прочее
			addToSameOwnerButton.IsVisible = updateButton.IsVisible = removeButton.IsVisible =
				updateContactsButton.IsVisible = findButton.IsEnabled = findNextButton.IsEnabled = (kl.ItemsCount > 0);

			countLabel.Text = "Отслеживается касс: " + kl.ItemsCount.ToString () + RDLocale.RN +
				"Число владельцев: " + kl.OwnersCount.ToString () + RDLocale.RN +
				"Предупреждений: " + (yWarnings + rWarnings).ToString ();

			RDInterfaceColors color;
			if (rWarnings > 0)
				color = RDInterfaceColors.ErrorMessage;
			else if (yWarnings > 0)
				color = RDInterfaceColors.WarningMessage;
			else
				color = RDInterfaceColors.SuccessMessage;

			countLabel.BackgroundColor = RDInterface.GetInterfaceColor (color);

			// Загрузка описания
			KKTList_ButtonClicked (null, null);
			}

		// Выбор ККТ в списке
		private void KKTList_ButtonClicked (object sender, EventArgs e)
			{
			// Поиск индекса
			if (sender != null)
				{
				Button b = (Button)sender;

				int idx = kktListField.Children.IndexOf (b);
				if (idx < 0)
					return;
				else
					selectedIndex = (uint)idx;

				RDInterface.SetCurrentPage (kktInfoPage, b.BackgroundColor);
				}

			// Защита
			if (kl.ItemsCount < 1)
				return;

			KAECFoundRequisites? v = kl.GetRequisites (selectedIndex);
			KAECFoundRequisites fr = v.Value;

			infoLabel.Text = "Заводской номер ККТ: " + fr.KKTSerial + RDLocale.RN;
			infoLabel.Text += "Модель ККТ: " + kb.KKTNumbers.GetKKTModel (fr.KKTSerial) + RDLocale.RN;
			infoLabel.Text += "Владелец: " + fr.KKTOwner + RDLocale.RNRN;
			infoLabel.Text += "Местоположение: " + fr.KKTPlacement + RDLocale.RN;
			infoLabel.Text += "Контактные данные: " + fr.KKTOwnerContact + RDLocale.RNRN;
			infoLabel.Text += "Срок действия ФН: " + fr.FNExpirationDate + RDLocale.RN;
			infoLabel.Text += "  Осталось дней: " + fr.DaysToFNExpiration.ToString () + RDLocale.RN;
			if (fr.FNActivationDate != KAECList.NoOFDAlias)
				infoLabel.Text += "  Активирован: " + fr.FNActivationDate + RDLocale.RN;

			infoLabel.Text += RDLocale.RN;
			if (fr.OFDExpirationDate == KAECList.UnknownOFDAlias)
				{
				infoLabel.Text += "Состояние ОФД: неизвестно" + RDLocale.RN;
				}
			else if (fr.OFDExpirationDate != KAECList.NoOFDAlias)
				{
				infoLabel.Text += "Срок тарифа ОФД: " + fr.OFDExpirationDate + RDLocale.RN;
				infoLabel.Text += "  Осталось дней: " + fr.DaysToOFDExpiration.ToString () + RDLocale.RN;
				if (fr.OFDActivationDate != KAECList.NoOFDAlias)
					infoLabel.Text += "  Активирован: " + fr.OFDActivationDate + RDLocale.RN;
				}

			// Загрузка телефонов
			phonesField.Children.Clear ();
			for (int i = 0; i < fr.ExtractedPhoneNumbers.Length; i++)
				{
				Button b = new Button ();
				RDInterface.ApplyButtonDefaults (b, true);

				b.Text = fr.ExtractedPhoneNumbers[i];
				b.BackgroundColor = kktListFieldBackColor;
				b.Margin = new Thickness (3);
				b.Clicked += PhoneFieldButton_Clicked;

				phonesField.Children.Add (b);
				}

			// Цветовая схема
			int fnDays = kl.GetDaysToFNExpiration (selectedIndex);
			int ofdDays = kl.GetDaysToOFDExpiration (selectedIndex);
			uint yellowTs = KAECList.YellowWarningThreshold;
			uint redTs = KAECList.RedWarningThreshold;

			if ((fnDays < redTs) || (ofdDays < redTs))
				kktInfoPage.BackgroundColor = RDInterface.GetInterfaceColor (RDInterfaceColors.ErrorMessage);
			else if ((fnDays < yellowTs) || (ofdDays < yellowTs))
				kktInfoPage.BackgroundColor = RDInterface.GetInterfaceColor (RDInterfaceColors.WarningMessage);
			else
				kktInfoPage.BackgroundColor = RDInterface.GetInterfaceColor (RDInterfaceColors.SuccessMessage);

			float cr = kktInfoPage.BackgroundColor.Red * 0.95f;
			float cg = kktInfoPage.BackgroundColor.Green * 0.95f;
			float cb = kktInfoPage.BackgroundColor.Blue * 0.95f;
			updateButton.BackgroundColor = removeButton.BackgroundColor =
				addToSameOwnerButton.BackgroundColor = updateContactsButton.BackgroundColor =
				findNextButton.BackgroundColor = Color.FromRgb (cr, cg, cb);
			}

		// Поиск записи
		private async void SearchButton_Click (object sender, EventArgs e)
			{
			// Запрос критерия
			bool next = !RDInterface.IsNameDefault (((Button)sender).Text, RDDefaultButtons.Find);
			string criteria;
			if (next)
				{
				criteria = lastSearchCriteria;
				}
			else
				{
				criteria = await RDInterface.ShowInput ("Критерий поиска ККТ",
					"Укажите заводской номер, владельца или местоположение ККТ",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Find),
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
					20, Keyboard.Default);

				if (criteria == null)
					return;
				}

			if (string.IsNullOrWhiteSpace (criteria))
				{
				RDInterface.ShowBalloon ("Укажите критерий для поиска", true);
				return;
				}
			if (!next)
				lastSearchCriteria = criteria;

			// Поиск
			int idx = kl.FindEntry (criteria);
			if (idx < 0)
				{
				RDInterface.ShowBalloon ("Не найдены ККТ, соответствующие указанному критерию", true);
				return;
				}

			// Успешно
			KKTList_ButtonClicked (kktListField.Children[idx], null);
			}

		// Передача номера телефона в буфер обмена и в приложение для звонков
		private void PhoneFieldButton_Clicked (object sender, EventArgs e)
			{
			string number = ((Button)sender).Text;

			RDGenerics.SendToClipboard (number, true);
			try
				{
				PhoneDialer.Open (number);
				}
			catch { }
			}

		#endregion

		#region Страница информации о ККТ

		// Обновление записи
		private void UpdateButton_Click (object sender, EventArgs e)
			{
			createFromScratch = false;
			createWithSameOwner = false;
			editOwnerData = false;

			RunRecordEdition ();
			}

		// Удаление записи
		private async void RemoveButton_Click (object sender, EventArgs e)
			{
			if (!await RDInterface.ShowMessage ("Удалить выбранную ККТ?",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Yes),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)))
				return;

			// Удаление
			kl.RemoveEntry (selectedIndex);
			ReloadList ();
			}

		// Добавление ККТ к существующему пользователю
		private void AddToSameOwner_Click (object sender, EventArgs e)
			{
			createFromScratch = false;
			createWithSameOwner = true;
			editOwnerData = false;

			RunRecordEdition ();
			}

		// Обновление контактов
		private void UpdateContactsButton_Click (object sender, EventArgs e)
			{
			createFromScratch = false;
			createWithSameOwner = false;
			editOwnerData = true;

			RunRecordEdition ();
			}

		#endregion

		#region Настройки и О приложении

		// Вызов справочных материалов
		private async void ReferenceButton_Click (object sender, EventArgs e)
			{
			await RDInterface.CallHelpMaterials (RDHelpMaterials.ReferenceMaterials);
			}

		private async void HelpButton_Click (object sender, EventArgs e)
			{
			await RDInterface.CallHelpMaterials (RDHelpMaterials.HelpAndSupport);
			}

		// Изменение размера шрифта интерфейса
		private void FontSizeButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					RDInterface.MasterFontSize += 0.5;
				else if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					RDInterface.MasterFontSize -= 0.5;
				}

			fontSizeField.Text = RDInterface.MasterFontSize.ToString ("F1");
			fontSizeField.FontSize = RDInterface.MasterFontSize;
			}

		// Изменение порогов
		private void YellowThresholdButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					{
					if (KAECList.YellowWarningThreshold + 1 <= 30)
						KAECList.YellowWarningThreshold++;
					}
				else if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					{
					if (KAECList.YellowWarningThreshold - 1 > KAECList.RedWarningThreshold)
						KAECList.YellowWarningThreshold--;
					}
				}

			yellowThresholdField.Text = "  " + KAECList.YellowWarningThreshold.ToString () + "  ";
			}

		private void RedThresholdButton_Clicked (object sender, EventArgs e)
			{
			if (sender != null)
				{
				Button b = (Button)sender;
				if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Increase))
					{
					if (KAECList.RedWarningThreshold + 1 < KAECList.YellowWarningThreshold)
						KAECList.RedWarningThreshold++;
					}
				else if (RDInterface.IsNameDefault (b.Text, RDDefaultButtons.Decrease))
					{
					if (KAECList.RedWarningThreshold - 1 >= 1)
						KAECList.RedWarningThreshold--;
					}
				}

			redThresholdField.Text = "  " + KAECList.RedWarningThreshold.ToString () + "  ";
			}

		#endregion

		#region Настройки ККТ

		// Запуск записи на редактирование
		private void RunRecordEdition ()
			{
			// Инициализация
			KAECFoundRequisites? v;
			if (!createFromScratch)
				v = kl.GetRequisites (selectedIndex);
			else
				v = null;

			// Изменение данных о владельце ККТ
			kktSerialLabel.IsVisible = kktSerialField.IsVisible =
				kktPlacementLabel.IsVisible = kktPlacementField.IsVisible =
				fnExpirationDateLabel.IsVisible = fnExpirationDateField.IsVisible = fnExpirationFlag.IsVisible =
				fnExpirationFlagLabel.IsVisible = ofdExpirationDateLabel.IsVisible = ofdExpirationDateField.IsVisible =
				ofdVariantLabel.IsVisible = ofdVariantButton.IsVisible =
				fnExpirationDateFromCBButton.IsVisible = ofdExpirationDateFromCBButton.IsVisible =
				fnEvaluatedLengthLabel1.IsVisible = fnEvaluatedLengthLabel2.IsVisible = fnEvaluatedLengthField.IsVisible =
				fnEvaluatedLengthFlag.IsVisible = ofdEvaluatedLengthLabel1.IsVisible = ofdEvaluatedLengthLabel2.IsVisible =
				ofdEvaluatedLengthField.IsVisible = ofdEvaluatedLengthFlag.IsVisible = !editOwnerData;
			kktSerialField.IsEnabled = createFromScratch || createWithSameOwner;

			if (editOwnerData)
				{
				kktOwnerField.IsEnabled = kktOwnerContactsField.IsEnabled = true;

				KAECFoundRequisites fr = v.Value;

				kktOwnerField.Text = fr.KKTOwner;
				kktOwnerContactsField.Text = fr.KKTOwnerContact;
				kktOwnerINNField.Text = fr.KKTOwnerINN;
				kktOwnerINNField.IsEnabled = !KAECOwner.CheckINN (fr.KKTOwnerINN, true);

				kktSettingsPage.Title = "Обновление сведений о владельце";
				applyButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Update);
				}

			// Создание записи с указанным владельцем или редактирование записи
			else if (!createFromScratch)
				{
				KAECFoundRequisites fr = v.Value;

				kktOwnerField.Text = fr.KKTOwner;
				kktOwnerContactsField.Text = fr.KKTOwnerContact;
				kktOwnerINNField.Text = fr.KKTOwnerINN;
				kktOwnerField.IsEnabled = kktOwnerContactsField.IsEnabled = kktOwnerINNField.IsEnabled =
					!kl.HasOwner (kktOwnerINNField.Text);

				if (!createWithSameOwner)
					{
					kktSerialField.Text = fr.KKTSerial;
					kktPlacementField.Text = fr.KKTPlacement;
					fnExpirationDateField.Date = DateTime.Parse (fr.FNExpirationDate);

					fnEvaluatedLengthFlag.IsToggled = (fr.FNEvaluatedLength != 0);
					if (fnEvaluatedLengthFlag.IsToggled)
						fnEvaluatedLengthField.Text = fr.FNEvaluatedLength.ToString ();

					if (fr.OFDExpirationDate == KAECList.NoOFDAlias)
						ofdVariant = 2;
					else if (fr.OFDExpirationDate == KAECList.UnknownOFDAlias)
						ofdVariant = 1;
					else
						ofdVariant = 0;

					if (ofdVariant == 0)
						{
						ofdExpirationDateField.Date = DateTime.Parse (fr.OFDExpirationDate);

						ofdEvaluatedLengthFlag.IsToggled = (fr.OFDEvaluatedLength != 0);
						if (ofdEvaluatedLengthFlag.IsToggled)
							ofdEvaluatedLengthField.Text = fr.OFDEvaluatedLength.ToString ();
						}
					else
						{
						ofdExpirationDateField.Date = ofdExpirationDateField.MinimumDate.Value;
						}

					fnExpirationFlag.IsToggled = fr.NoControl;

					kktSettingsPage.Title = "Обновление сведений о ККТ";
					applyButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Update);
					}
				else
					{
					ofdVariant = 0;
					fnExpirationDateField.Date = fnExpirationDateField.MinimumDate.Value;
					ofdExpirationDateField.Date = ofdExpirationDateField.MinimumDate.Value;
					kktSerialField.Text = "";
					kktPlacementField.Text = "";

					fnEvaluatedLengthFlag.IsToggled = false;
					fnEvaluatedLengthField.Text = KAECList.FNLiveLengths[0].ToString ();
					ofdEvaluatedLengthFlag.IsToggled = false;
					ofdEvaluatedLengthField.Text = "1";

					kktSettingsPage.Title = "Добавление ККТ к тому же пользователю";
					applyButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Add);
					}

				OFDVariantButton_Click (null, null);
				}

			// Добавление новой записи
			else
				{
				fnExpirationDateField.Date = fnExpirationDateField.MinimumDate.Value;
				ofdExpirationDateField.Date = ofdExpirationDateField.MinimumDate.Value;
				kktOwnerField.IsEnabled = kktOwnerContactsField.IsEnabled = kktOwnerINNField.IsEnabled = true;
				kktOwnerField.Text = "";
				kktOwnerContactsField.Text = "";
				kktOwnerINNField.Text = "";
				kktSerialField.Text = "";
				kktPlacementField.Text = "";

				fnEvaluatedLengthFlag.IsToggled = false;
				fnEvaluatedLengthField.Text = KAECList.FNLiveLengths[0].ToString ();
				ofdEvaluatedLengthFlag.IsToggled = false;
				ofdEvaluatedLengthField.Text = "1";

				ofdVariant = 0;

				OFDVariantButton_Click (null, null);

				kktSettingsPage.Title = "Добавление новой ККТ";
				applyButton.Text = RDLocale.GetDefaultText (RDLDefaultTexts.Button_Add);
				}

			// Запуск
			RDInterface.SetCurrentPage (kktSettingsPage, kktSettingsMasterBackColor);

			// Пересчёт расположения элементов
			Current_MainDisplayInfoChanged (null, null);
			}

		// Копирование даты из буфера обмена
		private async void FNDateFromCB_Click (object sender, EventArgs e)
			{
			string s = await RDGenerics.GetFromClipboard ();
			try
				{
				fnExpirationDateField.Date = DateTime.Parse (s, RDLocale.GetCulture (RDLanguages.ru_ru));
				}
			catch
				{
				fnExpirationDateField.Date = DateTime.Now;
				}
			}

		private async void OFDDateFromCB_Click (object sender, EventArgs e)
			{
			string s = await RDGenerics.GetFromClipboard ();
			try
				{
				ofdExpirationDateField.Date = DateTime.Parse (s, RDLocale.GetCulture (RDLanguages.ru_ru));
				}
			catch
				{
				ofdExpirationDateField.Date = DateTime.Now;
				}
			}

		// Выбор варианта работы с ОФД
		private async void OFDVariantButton_Click (object sender, EventArgs e)
			{
			if (ofdVariants.Count < 1)
				{
				ofdVariants.Add ("ККТ работает с ОФД");
				ofdVariants.Add ("ККТ с ОФД, но статус неизвестен");
				ofdVariants.Add ("ККТ работает без ОФД");
				}

			// Выбор варианта
			int res;
			if (sender == null)
				{
				res = ofdVariant;
				}
			else
				{
				res = await RDInterface.ShowList ("Выберите режим работы с ОФД",
					RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
					ofdVariants);
				if (res < 0)
					return;

				ofdVariant = res;
				}

			ofdVariantButton.Text = ofdVariants[res];

			// Переключение полей
			ofdExpirationDateLabel.IsVisible = ofdExpirationDateField.IsVisible = ofdExpirationDateFromCBButton.IsVisible =
				ofdEvaluatedLengthLabel1.IsVisible = ofdEvaluatedLengthLabel2.IsVisible = ofdEvaluatedLengthField.IsVisible =
				ofdEvaluatedLengthFlag.IsVisible = (ofdVariant == 0);
			}

		// Применение настроек
		private async void ApplyButton_Click (object sender, EventArgs e)
			{
			// Контроль
			if (string.IsNullOrWhiteSpace (kktOwnerField.Text))
				{
				RDInterface.ShowBalloon ("Указаны не все необходимые данные", true);
				return;
				}

			if (kktOwnerINNField.IsEnabled && !KAECOwner.CheckINN (kktOwnerINNField.Text, false))
				{
				RDInterface.ShowBalloon ("Введено некорректное значение ИНН", true);
				return;
				}

			// Обновление данных владельца
			if (editOwnerData)
				{
				kl.UpdateOwnerData (selectedIndex, kktOwnerField.Text, kktOwnerINNField.Text,
					kktOwnerContactsField.Text);

				ReloadList ();
				await RDInterface.MasterPage.PopAsync (true);
				return;
				}

			// Контроль остальных значений
			if (string.IsNullOrWhiteSpace (kktSerialField.Text) ||
				(fnExpirationDateField.Date.Value.Year == fnExpirationDateField.MinimumDate.Value.Year) ||
				(ofdVariant == 0) &&
				(ofdExpirationDateField.Date.Value.Year == ofdExpirationDateField.MinimumDate.Value.Year))
				{
				RDInterface.ShowBalloon ("Указаны не все необходимые данные", true);
				return;
				}

			// Создание или обновление записи о ККТ
			KAECItemParameters parameters;
			parameters.Serial = kktSerialField.Text;
			parameters.Placement = kktPlacementField.Text;
			parameters.FNExpirationDate = fnExpirationDateField.Date.Value;
			parameters.OFDExpirationDate = ofdExpirationDateField.Date.Value;
			parameters.OFDControlType = (OFDControlTypes)ofdVariant;
			parameters.NoControl = fnExpirationFlag.IsToggled;
			parameters.FNEvaluatedLength = fnEvaluatedLengthFlag.IsToggled ? uint.Parse (fnEvaluatedLengthField.Text) : 0;
			parameters.OFDEvaluatedLength = ofdEvaluatedLengthFlag.IsToggled ? uint.Parse (ofdEvaluatedLengthField.Text) : 0;
			parameters.OwnerIndex = 0;  // Заглушка

			if (!kl.AddRequisites (kktOwnerField.Text, kktOwnerINNField.Text, kktOwnerContactsField.Text, parameters))
				{
				if (kktOwnerINNField.IsEnabled)
					{
					await RDInterface.ShowMessage ("Введённый ИНН уже присутствует в списке владельцев. ККТ была добавлена к " +
						"существующему владельцу, введённые контактные данные и наименование были проигнорированы",
						RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK));
					}
				}

			// Обновление списка
			ReloadList ();

			// Перенацеливание раздела просмотра
			lastSearchCriteria = kktSerialField.Text;
			SearchButton_Click (findNextButton, null);

			await RDInterface.MasterPage.PopAsync (true);
			}

		// Переключение полей сроков активации
		private void FNEvaluatedLengthFlag_CheckedChanged (object sender, EventArgs e)
			{
			fnEvaluatedLengthField.IsEnabled = fnEvaluatedLengthFlag.IsToggled;
			}

		private void OFDEvaluatedLengthFlag_CheckedChanged (object sender, EventArgs e)
			{
			ofdEvaluatedLengthField.IsEnabled = ofdEvaluatedLengthFlag.IsToggled;
			}

		// Выбор значений сроков активации
		private async void FNEvaluatedField_Clicked (object sender, EventArgs e)
			{
			// Запрос
			if (fnLiveVariants.Count < 1)
				{
				for (int i = 0; i < KAECList.FNLiveLengths.Length; i++)
					fnLiveVariants.Add (KAECList.FNLiveLengths[i].ToString ());
				}

			int res = await RDInterface.ShowList ("Срок жизни ФН",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel), fnLiveVariants);
			if (res < 0)
				return;

			fnEvaluatedLengthField.Text = fnLiveVariants[res].ToString ();
			}

		private async void OFDEvaluatedField_Clicked (object sender, EventArgs e)
			{
			// Запрос
			string res = await RDInterface.ShowInput ("Срок тарифа ОФД", "Введите срок активированного тарифа ОФД (в днях)",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_OK), RDLocale.GetDefaultText (RDLDefaultTexts.Button_Cancel),
				4, Keyboard.Numeric, ofdEvaluatedLengthField.Text);
			if (res == null)
				return;

			// Проверка
			uint v;
			try
				{
				v = uint.Parse (res);
				}
			catch
				{
				RDInterface.ShowBalloon ("Указан некорректный срок тарифа ОФД", true);
				return;
				}

			if ((v < 1) || (v > 1000))
				{
				RDInterface.ShowBalloon ("Указанный срок тарифа ОФД выходит за допустимый диапазон " +
					"(от 1 до 1000 дней)", true);
				return;
				}

			// Выполнено
			ofdEvaluatedLengthField.Text = res;
			}

		#endregion
		}
	}
