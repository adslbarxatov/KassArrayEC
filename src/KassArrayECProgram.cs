extern alias KassArrayDB;

using System;
using System.Threading;
using System.Windows.Forms;

namespace RD_AAOW
	{
	/// <summary>
	/// Класс описывает точку входа приложения
	/// </summary>
	public static class KAECProgram
		{
		/// <summary>
		/// Главная точка входа для приложения
		/// </summary>
		[STAThread]
		public static void Main (string[] args)
			{
			// Инициализация
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);
			RDLocale.InitEncodings ();

			// Язык интерфейса и контроль XPUN
			if (!RDLocale.IsXPUNClassAcceptable)
				return;

			// Проверка запуска единственной копии (псевдоним не должен совпадать с именем EventWaitHandle)
			if (!RDGenerics.IsAppInstanceUnique (false, "_L"))
				{
				RDInterface.MessageBox (RDMessageFlags.Warning | RDMessageFlags.LockSmallSize | RDMessageFlags.CenterText,
					"Программа " + ProgramDescription.AssemblyMainName + " уже запущена");

				try
					{
					EventWaitHandle ewh = EventWaitHandle.OpenExisting (ProgramDescription.AssemblyMainName);
					ewh.Set ();
					}
				catch { }

				return;
				}

			// Контроль прав и целостности
			if (!RDGenerics.AppHasAccessRights (true, false))
				return;

			if (!RDGenerics.StartedFromMSStore)
				{
				if (!RDGenerics.CheckLibrariesExistence (ProgramDescription.AssemblyLibraries, true))
					return;

				if (!LibraryProtocolChecker.CheckProtocolVersion (ProgramDescription.AssemblyDLLProtocol,
					KassArrayDB::RD_AAOW.ProgramDescription.KassArrayDBDLL))
					return;
				}

			// Отображение справки и запроса на принятие Политики
			if (!RDInterface.AcceptEULA ())
				return;
			if (!RDInterface.ShowAbout (true))
				RDGenerics.RegisterFileAssociations (true);

			// Запуск
			if (args.Length > 0)
				{
				if (args[0] == "-a")
					{
					Application.Run (new KassArrayECForm (true, false));
					return;
					}
				else if (args[0] == KassArrayDB::RD_AAOW.KKTSupport.HideWindowKey)
					{
					if (KAECList.AllowAutostart)
						Application.Run (new KassArrayECForm (false, true));

					return;
					}
				}

			Application.Run (new KassArrayECForm (false, false));
			}
		}
	}
