extern alias KassArrayDB;

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает главную форму программы
	/// </summary>
	public partial class KassArrayECForm: Form
		{
		// Переменные и константы
		private KassArrayDB::RD_AAOW.KnowledgeBase kb;
		private bool getValuesFromRegistration;

		// Ресивер сообщений на повторное открытие окна
		private EventWaitHandle ewh;

		// Список сохранённых реквизитов
		private KAECList kl;

		// Иконка в трее
		private bool closeWindowOnError = false;
		private bool closeWindowOnRequest = false;
		private bool hideWindow = false;
		private NotifyIcon ni = new NotifyIcon ();

		// Текущая выбранная строка и страница, размер страницы
		private uint selectedIndex = 0;
		private uint currentPage = 0;
		private const uint itemsPerPage = 9;

		/*// Число оповещений, видимых в поле списка без прокрутки
		private const uint visibleNotificationsInList = 10;*/

		// Стандартные компоненты стиля полей списка ККТ
		private Font kktFont;
		private Padding kktMargin = new Padding (3, 3, 3, 3);

		/// <summary>
		/// Конструктор. Запускает главную форму
		/// </summary>
		/// <param name="GetValuesFromRegistration">Флаг командной строки, указывающий на необходимость
		/// получения параметров ККТ из статуса ФН</param>
		public KassArrayECForm (bool GetValuesFromRegistration, bool HideWindow)
			{
			// Инициализация
			InitializeComponent ();
			RDGenerics.LoadWindowDimensions (this);

			getValuesFromRegistration = GetValuesFromRegistration;
			kb = new KassArrayDB::RD_AAOW.KnowledgeBase ();
			hideWindow = HideWindow;

			this.Text = RDGenerics.DefaultAssemblyVisibleName;

			if (KAECList.OverrideCloseButton)
				MExit.Text = "Свернуть";

			// Настройка иконки в трее
			ni.Icon = KassArrayECResources.KATray_Green;
			ni.Text = RDGenerics.DefaultAssemblyVisibleName;
			ni.Visible = true;

			ni.ContextMenuStrip = new ContextMenuStrip ();
			ni.ContextMenuStrip.ShowImageMargin = false;

			ni.ContextMenuStrip.Items.Add (RDLocale.GetDefaultText (RDLDefaultTexts.Button_Exit),
				null, CloseService);

			ni.MouseDown += ReturnWindow;
			ni.BalloonTipClicked += ReturnWindow;

			// Подключение к прослушиванию системного события вызова окна
			if (!RDGenerics.StartedFromMSStore)
				{
				bool ewhFailed = false;
				try
					{
					ewh = EventWaitHandle.OpenExisting (ProgramDescription.AssemblyMainName);
					}
				catch
					{
					ewhFailed = true;
					}
				if (ewhFailed)
					{
					try
						{
						ewh = new EventWaitHandle (false, EventResetMode.AutoReset,
							ProgramDescription.AssemblyMainName);
						}
					catch { }
					}

				ShowWindowTimer.Enabled = true;
				}

			// Загрузка списка
			kl = new KAECList ();
			kktFont = new Font (this.Font, FontStyle.Bold);
			ReloadList ();
			}

		private void KassArrayECForm_Shown (object sender, EventArgs e)
			{
			// Скрытие при автозапуске
			if (hideWindow)
				this.Hide ();

			// Прерывание при ошибке
			if (closeWindowOnError)
				{
				this.Close ();
				return;
				}

			// Запуск переданных реквизитов на редактирование
			if (getValuesFromRegistration)
				GetRequisitesFromRegistration ();
			}

		// Закрытие окна
		private void CloseService (object sender, EventArgs e)
			{
			closeWindowOnRequest = true;
			this.Close ();
			}

		private void MExit_Click (object sender, EventArgs e)
			{
			if (KAECList.OverrideCloseButton)
				{
				this.Hide ();
				return;
				}

			closeWindowOnRequest = true;
			this.Close ();
			}

		private void KassArrayECForm_FormClosing (object sender, FormClosingEventArgs e)
			{
			if (closeWindowOnError)
				return;

			if (KAECList.OverrideCloseButton && !closeWindowOnRequest)
				{
				e.Cancel = true;
				MExit_Click (null, null);
				return;
				}

			// Завершение
			RDGenerics.SaveWindowDimensions (this);
			ni.Visible = false;
			}

		// Возврат окна приложения
		private void ReturnWindow (object sender, MouseEventArgs e)
			{
			if (e.Button != MouseButtons.Left)
				return;

			if (this.Visible)
				{
				MExit_Click (null, null);
				}
			else
				{
				this.Show ();

				this.TopMost = true;
				this.TopMost = false;
				this.WindowState = FormWindowState.Normal;
				}
			}

		private void ReturnWindow (object sender, EventArgs e)
			{
			this.Show ();

			this.TopMost = true;
			this.TopMost = false;
			this.WindowState = FormWindowState.Normal;
			}

		// Отображение справки
		private void MHelp_Clicked (object sender, EventArgs e)
			{
			RDInterface.ShowAbout (false);
			}

		// Таймер обратной связи с вызывающим приложением
		private void ShowWindowTimer_Tick (object sender, EventArgs e)
			{
			// Контроль
			if (ewh == null)
				return;

			// Защита от лишних действий
			string flag = KassArrayDB::RD_AAOW.KKTSupport.PathForStartupOpening;
			if (string.IsNullOrWhiteSpace (flag) && this.Visible &&
				(this.WindowState != FormWindowState.Minimized) || !ewh.WaitOne (100))
				{
				ewh.Reset ();   // Удаление задвоенных вызовов
				return;
				}

			// Отмена повторного обращения
			ewh.Reset ();

			// Запуск
			this.Show ();

			this.TopMost = true;
			this.TopMost = false;
			this.WindowState = FormWindowState.Normal;

			// Запуск команды, если она была передана
			if (!string.IsNullOrWhiteSpace (flag))
				{
				KassArrayDB::RD_AAOW.KKTSupport.PathForStartupOpening = "";

				GetRequisitesFromRegistration ();
				}
			}

		// Получение реквизитов для отслеживания из данных регистрации KassArrayFS
		private void GetRequisitesFromRegistration ()
			{
			KassArrayECEntry kaec = new KassArrayECEntry (kl, true);
			if (!kaec.Cancelled)
				ReloadList ();
			kaec.Dispose ();
			}

		// Перезагрузка списка ККТ
		private void ReloadList ()
			{
			// Сброс списка
			KKTList.Controls.Clear ();

			if (kl.ItemsCount < 1)
				{
				currentPage = 0;
				selectedIndex = 0;
				}
			else if (currentPage * itemsPerPage >= kl.ItemsCount)
				{
				currentPage--;
				selectedIndex = currentPage * itemsPerPage;
				}
			else if (selectedIndex >= kl.ItemsCount)
				{
				selectedIndex = kl.ItemsCount - 1;
				}

			/*uint pagesCount = kl.ItemsCount / itemsPerPage;
			if (kl.ItemsCount % itemsPerPage != 0)
				pagesCount++;*/

			GoToFirstPage.Enabled = GoToPreviousPage.Enabled = (currentPage > 0);
			GoToLastPage.Enabled = GoToNextPage.Enabled = (currentPage < (int)PagesCount - 1);

			if (kl.ItemsCount < 1)
				PageLabel.Text = "—";
			else
				PageLabel.Text = (currentPage + 1).ToString () + " из " + PagesCount.ToString ();

			uint yellowTs = KAECList.YellowWarningThreshold;
			uint redTs = KAECList.RedWarningThreshold;
			RDInterfaceColors color;

			// Формирование контролов
			for (uint i = currentPage * itemsPerPage; (i < (currentPage + 1) * itemsPerPage) && (i < kl.ItemsCount); i++)
				{
				KAECFoundRequisites? v = kl.GetRequisites (i);
				KAECFoundRequisites fr = v.Value;
				string model = kb.KKTNumbers.GetKKTModel (fr.KKTSerial);

				// Сборка контрола
				Label l = new Label ();
				l.AutoSize = false;

				if (kl.GetNoControlStatus (i))
					color = RDInterfaceColors.MediumGrey;
				else if ((kl.GetDaysToFNExpiration (i) < redTs) || (kl.GetDaysToOFDExpiration (i) < redTs))
					color = RDInterfaceColors.ErrorMessage;
				else if ((kl.GetDaysToFNExpiration (i) < yellowTs) || (kl.GetDaysToOFDExpiration (i) < yellowTs))
					color = RDInterfaceColors.WarningMessage;
				else
					color = RDInterfaceColors.SuccessMessage;
				l.BackColor = RDInterface.GetInterfaceColor (color);

				l.ForeColor = RDInterface.GetInterfaceColor (RDInterfaceColors.DefaultText);
				l.Click += KKTList_LabelClicked;
				l.Font = kktFont;
				l.Text = model + "  |  " + fr.KKTOwner;
				l.Margin = kktMargin;
				l.Padding = kktMargin;

				// Блокировка и применение размера (минимально необходимого для точного
				// триггеринга VerticalScroll.Visible)
				l.MaximumSize = l.MinimumSize = new Size (KKTList.Width - 6 -
					/*((kl.ItemsCount > visibleNotificationsInList) ? 18 : 0), 0);*/
					18, 0);
				l.AutoSize = true;

				// Добавление
				KKTList.Controls.Add (l);
				}

			// Придётся прогонять ещё раз, поскольку статус VerticalScroll.Visible определяется только
			// по окончании загрузки списка
			for (int i = 0; i < KKTList.Controls.Count; i++)
				{
				Label l = (Label)KKTList.Controls[i];

				l.AutoSize = false;
				l.MaximumSize = l.MinimumSize = new Size (KKTList.Width - 6 -
					(KKTList.VerticalScroll.Visible ? 18 : 0), 0);
				l.AutoSize = true;
				}

			// Прочее
			BAddSameOwner.Enabled = BUpdate.Enabled = BRemove.Enabled = BUpdateContacts.Enabled =
				SearchButton.Enabled = SearchField.Enabled = (kl.ItemsCount > 0);

			uint yWarnings = 0;
			uint rWarnings = 0;

			for (uint i = 0; i < kl.ItemsCount; i++)
				{
				if (kl.GetNoControlStatus (i))
					continue;

				int fnExpiration = kl.GetDaysToFNExpiration (i);
				int ofdExpiration = kl.GetDaysToOFDExpiration (i);

				if ((fnExpiration < redTs) || (ofdExpiration < redTs))
					rWarnings++;
				else if ((fnExpiration < yellowTs) || (ofdExpiration < yellowTs))
					yWarnings++;
				}

			CountLabel.Text = "Касс в списке: " + kl.ItemsCount.ToString () +
				"  |  Число владельцев: " + kl.OwnersCount.ToString () + RDLocale.RN +
				"Предупреждений: " + (yWarnings + rWarnings).ToString ();

			if (rWarnings > 0)
				{
				color = RDInterfaceColors.ErrorMessage;
				ni.Icon = KassArrayECResources.KATray_Red;

				if (!this.Visible)
					ni.ShowBalloonTip (5000, RDGenerics.DefaultAssemblyVisibleName,
						"Есть ККТ, срок действия ФН или тарифа ОФД на который почти истёк",
						ToolTipIcon.Error);
				}
			else if (yWarnings > 0)
				{
				color = RDInterfaceColors.WarningMessage;
				ni.Icon = KassArrayECResources.KATray_Yellow;

				if (!this.Visible)
					ni.ShowBalloonTip (3000, RDGenerics.DefaultAssemblyVisibleName,
						"Есть ККТ, срок действия ФН или тарифа ОФД на который скоро истекает",
						ToolTipIcon.Warning);
				}
			else
				{
				color = RDInterfaceColors.SuccessMessage;
				ni.Icon = KassArrayECResources.KATray_Green;
				}

			CountLabel.BackColor = RDInterface.GetInterfaceColor (color);

			// Загрузка описания
			KKTList_LabelClicked (null, null);

			int idx = (int)(selectedIndex % itemsPerPage);
			if ((kl.ItemsCount > 0) && (idx < KKTList.Controls.Count))
				KKTList.ScrollControlIntoView (KKTList.Controls[idx]);
			}

		// Выбор ККТ в списке
		private void KKTList_LabelClicked (object sender, EventArgs e)
			{
			// Поиск индекса
			if (sender != null)
				{
				int idx = KKTList.Controls.IndexOf ((Control)sender);
				if (idx < 0)
					return;
				else
					selectedIndex = (uint)idx + currentPage * itemsPerPage;
				}

			// Обновление стиля
			for (uint i = currentPage * itemsPerPage; (i < (currentPage + 1) * itemsPerPage) && (i < kl.ItemsCount); i++)
				{
				((Label)KKTList.Controls[(int)(i % itemsPerPage)]).BorderStyle = (i == selectedIndex) ?
					BorderStyle.FixedSingle : BorderStyle.None;
				}

			// Загрузка состояния
			if (kl.ItemsCount < 1)
				{
				InfoLabel.Text = "(отсутствуют ККТ для отображения)";
				InfoLabel.BackColor = RDInterface.GetInterfaceColor (RDInterfaceColors.LightGrey);

				return;
				}

			KAECFoundRequisites? v = kl.GetRequisites (selectedIndex);
			KAECFoundRequisites fr = v.Value;

			InfoLabel.Text = "Заводской номер ККТ: " + fr.KKTSerial + RDLocale.RN;
			InfoLabel.Text += "Модель ККТ: " + kb.KKTNumbers.GetKKTModel (fr.KKTSerial) + RDLocale.RN;
			InfoLabel.Text += "Владелец: " + fr.KKTOwner + RDLocale.RNRN;

			InfoLabel.Text += "Местоположение: " + fr.KKTPlacement + RDLocale.RN;
			InfoLabel.Text += "Контактные данные: " + fr.KKTOwnerContact + RDLocale.RNRN;

			InfoLabel.Text += "Срок действия ФН: " + fr.FNExpirationDate + RDLocale.RN;
			InfoLabel.Text += "  Осталось дней: " + fr.DaysToFNExpiration.ToString () + RDLocale.RN;
			if (fr.FNActivationDate != KAECList.NoOFDAlias)
				InfoLabel.Text += "  Активирован: " + fr.FNActivationDate + RDLocale.RN;

			InfoLabel.Text += RDLocale.RN;
			if (fr.OFDExpirationDate == KAECList.UnknownOFDAlias)
				{
				InfoLabel.Text += "Состояние ОФД: неизвестно";
				}
			else if (fr.OFDExpirationDate != KAECList.NoOFDAlias)
				{
				InfoLabel.Text += "Срок тарифа ОФД: " + fr.OFDExpirationDate + RDLocale.RN;
				InfoLabel.Text += "  Осталось дней: " + fr.DaysToOFDExpiration.ToString () + RDLocale.RN;
				if (fr.OFDActivationDate != KAECList.NoOFDAlias)
					InfoLabel.Text += "  Активирован: " + fr.OFDActivationDate;
				}

			int fnDays = kl.GetDaysToFNExpiration (selectedIndex);
			int ofdDays = kl.GetDaysToOFDExpiration (selectedIndex);
			uint yellowTs = KAECList.YellowWarningThreshold;
			uint redTs = KAECList.RedWarningThreshold;

			if ((fnDays < redTs) || (ofdDays < redTs))
				InfoLabel.BackColor = RDInterface.GetInterfaceColor (RDInterfaceColors.ErrorMessage);
			else if ((fnDays < yellowTs) || (ofdDays < yellowTs))
				InfoLabel.BackColor = RDInterface.GetInterfaceColor (RDInterfaceColors.WarningMessage);
			else
				InfoLabel.BackColor = RDInterface.GetInterfaceColor (RDInterfaceColors.SuccessMessage);
			}

		// Добавление новой ККТ
		private void BAddNew_Click (object sender, EventArgs e)
			{
			KassArrayECEntry kaec = new KassArrayECEntry (kl, false);
			if (!kaec.Cancelled)
				{
				ReloadList ();

				SearchField.Text = kaec.LastSerialNumber;
				SearchButton_Click (null, null);
				}

			kaec.Dispose ();
			}

		// Добавление ККТ к существующему пользователю
		private void BAddSameOwner_Click (object sender, EventArgs e)
			{
			if (kl.ItemsCount < 1)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Отсутствуют ККТ для копирования реквизитов", 1500);
				return;
				}

			KassArrayECEntry kaec = new KassArrayECEntry (kl, selectedIndex, true);
			if (!kaec.Cancelled)
				{
				ReloadList ();

				SearchField.Text = kaec.LastSerialNumber;
				SearchButton_Click (null, null);
				}

			kaec.Dispose ();
			}

		// Обновление записи
		private void BUpdate_Click (object sender, EventArgs e)
			{
			if (kl.ItemsCount < 1)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Нет ККТ для обновления", 1000);
				return;
				}

			KassArrayECEntry kaec = new KassArrayECEntry (kl, selectedIndex, false);
			if (!kaec.Cancelled)
				{
				ReloadList ();

				SearchField.Text = kaec.LastSerialNumber;
				SearchButton_Click (null, null);
				}

			kaec.Dispose ();
			}

		// Удаление записи
		private void BRemove_Click (object sender, EventArgs e)
			{
			// Контроль
			if (kl.ItemsCount < 1)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Список ККТ пуст", 1000);
				return;
				}

			if (RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText, "Удалить выбранную ККТ?",
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_YesNoFocus),
				RDLocale.GetDefaultText (RDLDefaultTexts.Button_No)) != RDMessageButtons.ButtonOne)
				return;

			// Удаление
			kl.RemoveEntry (selectedIndex);
			ReloadList ();
			}

		// Поиск записи
		private void SearchButton_Click (object sender, EventArgs e)
			{
			// Контроль
			if (string.IsNullOrWhiteSpace (SearchField.Text))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Укажите критерий для поиска", 1000);
				return;
				}

			// Поиск
			int idx = kl.FindEntry (SearchField.Text);
			if (idx < 0)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Не найдены ККТ, соответствующие указанному критерию", 1500);
				return;
				}

			// Успешно
			selectedIndex = (uint)idx;
			uint oldPage = currentPage;
			currentPage = selectedIndex / itemsPerPage;

			if (oldPage != currentPage)
				ReloadList ();

			KKTList_LabelClicked (null, null);
			KKTList.ScrollControlIntoView (KKTList.Controls[(int)(selectedIndex % itemsPerPage)]);
			}

		private void SearchField_KeyDown (object sender, KeyEventArgs e)
			{
			if (e.KeyCode == Keys.Return)
				SearchButton_Click (null, null);
			}

		// Обновление контактов
		private void BUpdateContacts_Click (object sender, EventArgs e)
			{
			if (kl.ItemsCount < 1)
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.CenterText,
					"Нет ККТ для обновления", 1000);
				return;
				}

			KassArrayECEntry kaec = new KassArrayECEntry (selectedIndex, kl);
			if (!kaec.Cancelled)
				ReloadList ();
			kaec.Dispose ();
			}

		// Вызов настроек
		private void MSettings_Click (object sender, EventArgs e)
			{
			_ = new KassArrayECSettings ();

			ReloadList ();

			if (KAECList.OverrideCloseButton)
				MExit.Text = "Свернуть";
			else
				MExit.Text = "Выход";
			}

		// Экспорт данных
		private void MExport_Click (object sender, EventArgs e)
			{
			KAECFoundRequisites? v = kl.GetRequisites (selectedIndex);

			KassArrayECExport kaece = new KassArrayECExport (kl, v == null ? "" : v.Value.KKTOwner, kb);
			if (kaece.ReloadRequired)
				ReloadList ();

			kaece.Dispose ();
			}

		// Короткая подсказка по сортировке списка
		private void ShortTipButton_Click (object sender, EventArgs e)
			{
			RDInterface.MessageBox (RDMessageFlags.Information | RDMessageFlags.LockSmallSize,
				"ККТ в списке отсортированы по порядку истечения сроков жизни ФН, начиная с тех, " +
				"замена которых потребуется раньше всех (относительно текущей даты)");
			}

		// Перезапрос списка ККТ из файла
		private void MUpdate_Click (object sender, EventArgs e)
			{
			kl.Dispose ();
			kl = new KAECList ();
			ReloadList ();
			}

		// Движение по страницам
		private void GoToFirstPage_Click (object sender, EventArgs e)
			{
			currentPage = 0;
			ReloadList ();
			}

		private void GoToPreviousPage_Click (object sender, EventArgs e)
			{
			if (currentPage > 0)
				currentPage--;

			ReloadList ();
			}

		private void GoToNextPage_Click (object sender, EventArgs e)
			{
			if (currentPage < PagesCount - 1)
				currentPage++;

			ReloadList ();
			}

		private void GoToLastPage_Click (object sender, EventArgs e)
			{
			currentPage = PagesCount - 1;
			ReloadList ();
			}

		private uint PagesCount
			{
			get
				{
				uint pagesCount = kl.ItemsCount / itemsPerPage;
				if (kl.ItemsCount % itemsPerPage != 0)
					pagesCount++;

				return pagesCount;
				}
			}
		}
	}
