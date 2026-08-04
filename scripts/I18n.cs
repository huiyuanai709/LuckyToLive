using Godot;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 标准国际化：从 CSV 注册到 TranslationServer，支持中英切换并持久化。
/// </summary>
public partial class I18n : Node
{
	public static I18n Instance { get; private set; }

	public const string LocaleZh = "zh_CN";
	public const string LocaleEn = "en";
	private const string CsvPath = "res://assets/i18n/translations.csv";
	private const string SavePath = "user://save.cfg";

	[Signal] public delegate void LocaleChangedEventHandler(string locale);

	public string CurrentLocale { get; private set; } = LocaleZh;

	public override void _Ready()
	{
		Instance = this;
		LoadCsvTranslations(CsvPath);
		SetLocale(LoadSavedLocale(), persist: false);
	}

	public static string T(string key)
	{
		if (string.IsNullOrEmpty(key)) return key;
		var translated = TranslationServer.Translate(key);
		return translated.ToString();
	}

	public static string T(string key, params object[] args)
	{
		string template = T(key);
		try
		{
			return string.Format(template, args);
		}
		catch (System.FormatException)
		{
			return template;
		}
	}

	public static string CardName(string cardId)
	{
		string key = $"card.{cardId}.name";
		string t = T(key);
		if (t == key)
		{
			var card = CardCatalog.Get(cardId);
			return card?.Name ?? cardId;
		}
		return t;
	}

	public static string CardDesc(string cardId)
	{
		string key = $"card.{cardId}.desc";
		string t = T(key);
		if (t == key)
		{
			var card = CardCatalog.Get(cardId);
			return card?.Desc ?? "";
		}
		return t;
	}

	public static string HeroName(HeroId id) => id switch
	{
		HeroId.Warrior => T("hero.warrior.name"),
		HeroId.Mage => T("hero.mage.name"),
		_ => T("hero.hunter.name"),
	};

	public static string HeroDesc(HeroId id) => id switch
	{
		HeroId.Warrior => T("hero.warrior.desc"),
		HeroId.Mage => T("hero.mage.desc"),
		_ => T("hero.hunter.desc"),
	};

	public static string KindLabel(CardKind kind) => kind switch
	{
		CardKind.Weapon => T("card.kind.weapon"),
		CardKind.Building => T("card.kind.building"),
		CardKind.Upgrade => T("card.kind.upgrade"),
		CardKind.Passive => T("card.kind.passive"),
		CardKind.Pet => T("card.kind.pet"),
		_ => "",
	};

	public static string AffixLabel(string affixId) => affixId switch
	{
		"dash" => T("affix.dash"),
		"melee" => T("affix.melee"),
		"shield" => T("affix.shield"),
		"summon" => T("affix.summon"),
		"ranged" => T("affix.ranged"),
		"orbit" => T("affix.orbit"),
		"fire_ground" => T("affix.fire_ground"),
		_ => affixId,
	};

	public void ToggleLocale()
	{
		SetLocale(CurrentLocale == LocaleZh ? LocaleEn : LocaleZh);
	}

	public void SetLocale(string locale, bool persist = true)
	{
		if (locale != LocaleEn && locale != LocaleZh)
			locale = LocaleZh;
		CurrentLocale = locale;
		TranslationServer.SetLocale(locale);
		if (persist) SaveLocale(locale);
		EmitSignal(SignalName.LocaleChanged, locale);
	}

	public string LocaleDisplayName(string locale = null)
	{
		locale ??= CurrentLocale;
		return locale == LocaleEn ? T("ui.lang.en") : T("ui.lang.zh");
	}

	private static string LoadSavedLocale()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SavePath) != Error.Ok)
			return DetectDefaultLocale();
		if (cfg.HasSectionKey("settings", "locale"))
			return (string)cfg.GetValue("settings", "locale", DetectDefaultLocale());
		return DetectDefaultLocale();
	}

	private static void SaveLocale(string locale)
	{
		var cfg = new ConfigFile();
		cfg.Load(SavePath); // keep other sections if present
		cfg.SetValue("settings", "locale", locale);
		cfg.Save(SavePath);
	}

	private static string DetectDefaultLocale()
	{
		string lang = OS.GetLocaleLanguage();
		if (lang.StartsWith("zh")) return LocaleZh;
		if (lang.StartsWith("en")) return LocaleEn;
		return LocaleZh;
	}

	private static void LoadCsvTranslations(string path)
	{
		if (!FileAccess.FileExists(path))
		{
			GD.PushWarning($"I18n: missing translation CSV at {path}");
			return;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushWarning($"I18n: failed to open {path}");
			return;
		}

		string headerLine = file.GetLine();
		var header = ParseCsvLine(headerLine);
		if (header.Count < 2 || header[0] != "keys")
		{
			GD.PushWarning("I18n: CSV must start with keys,<locale>...");
			return;
		}

		var byLocale = new Dictionary<string, Translation>();
		for (int i = 1; i < header.Count; i++)
		{
			string locale = header[i].Trim();
			if (string.IsNullOrEmpty(locale)) continue;
			var tr = new Translation { Locale = locale };
			byLocale[locale] = tr;
		}

		while (!file.EofReached())
		{
			string line = file.GetLine();
			if (string.IsNullOrWhiteSpace(line)) continue;
			var cols = ParseCsvLine(line);
			if (cols.Count == 0) continue;
			string key = cols[0];
			if (string.IsNullOrEmpty(key) || key == "keys") continue;
			for (int i = 1; i < header.Count && i < cols.Count; i++)
			{
				string locale = header[i].Trim();
				if (!byLocale.TryGetValue(locale, out var tr)) continue;
				tr.AddMessage(key, cols[i]);
			}
		}

		foreach (var tr in byLocale.Values)
			TranslationServer.AddTranslation(tr);
	}

	/// <summary>Minimal CSV parser supporting quoted fields with commas.</summary>
	private static List<string> ParseCsvLine(string line)
	{
		var result = new List<string>();
		if (line == null) return result;
		var sb = new StringBuilder();
		bool inQuotes = false;
		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];
			if (inQuotes)
			{
				if (c == '"')
				{
					if (i + 1 < line.Length && line[i + 1] == '"')
					{
						sb.Append('"');
						i++;
					}
					else inQuotes = false;
				}
				else sb.Append(c);
			}
			else
			{
				if (c == '"') inQuotes = true;
				else if (c == ',')
				{
					result.Add(sb.ToString());
					sb.Clear();
				}
				else sb.Append(c);
			}
		}
		result.Add(sb.ToString());
		return result;
	}
}
