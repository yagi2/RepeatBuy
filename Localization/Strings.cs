namespace RepeatBuy.Localization;

public enum Language
{
    English = 0,
    Japanese = 1,
    German = 2,
    French = 3
}

public static class Strings
{
    private static Language lang = Language.English;
    public static void SetLanguage(Language l) => lang = l;
    public static Language Current => lang;

    private static string T(string en, string ja, string de, string fr) => lang switch
    {
        Language.Japanese => ja,
        Language.German => de,
        Language.French => fr,
        _ => en
    };

    public static string LanguageNameEnglish => "English";
    public static string LanguageNameJapanese => "日本語";
    public static string LanguageNameGerman => "Deutsch";
    public static string LanguageNameFrench => "Français";

    public static string WindowTitle => T("RepeatBuy", "リピート購入", "RepeatBuy", "RepeatBuy");
    public static string ConfigWindowTitle => T(
        "RepeatBuy Settings",
        "リピート購入 設定",
        "RepeatBuy-Einstellungen",
        "Paramètres RepeatBuy");

    public static string SelectedItem => T("Selected:", "選択中:", "Ausgewählt:", "Sélectionné :");
    public static string NoItemSelected => T(
        "(pick an item from the list below)",
        "（下のリストから商品を選んでください）",
        "(Wähle einen Artikel aus der Liste unten)",
        "(choisis un article dans la liste ci-dessous)");
    public static string ShopList => T("Shop items", "ショップ商品一覧", "Händler-Artikel", "Articles du marchand");
    public static string ShopListEmpty => T(
        "(shop is empty or still loading)",
        "（ショップが空、または読み込み中）",
        "(Händler ist leer oder lädt noch)",
        "(boutique vide ou en cours de chargement)");
    public static string UnitPrice => T("Unit price:", "単価:", "Stückpreis:", "Prix unitaire :");
    public static string Owned => T("Owned:", "所持数:", "Im Besitz:", "Possédé :");
    public static string Gil => T("Gil:", "所持金:", "Gil:", "Gils :");
    public static string ColumnName => T("Item", "アイテム", "Artikel", "Article");
    public static string ColumnPrice => T("Price", "価格", "Preis", "Prix");
    public static string ColumnOwned => T("Have", "所持", "Bestand", "Stock");

    public static string ModeHeader => T("Stop condition", "停止条件", "Stoppbedingung", "Condition d'arrêt");
    public static string ModeFixed => T("Fixed count", "個数指定", "Feste Anzahl", "Quantité fixe");
    public static string ModeUntilOwned => T("Until owned", "所持数到達まで", "Bis Bestand erreicht", "Jusqu'à possession");
    public static string ModeGilFloor => T("Gil floor", "残高指定", "Gil-Untergrenze", "Plancher de gils");
    public static string ModeInventoryFull => T("Inventory full", "インベントリ満杯まで", "Inventar voll", "Inventaire plein");

    public static string LabelCount => T("Count", "個数", "Anzahl", "Quantité");
    public static string LabelTarget => T("Target owned", "所持数目標", "Ziel-Bestand", "Possession cible");
    public static string LabelGilFloor => T("Stop when gil <", "停止しきい値", "Stoppen wenn Gil <", "Arrêter si gils <");

    public static string EstimateBuy => T("Buy", "購入", "Kaufen", "Acheter");
    public static string Cancel => T("Cancel", "キャンセル", "Abbrechen", "Annuler");
    public static string Stop => T("Stop", "停止", "Stopp", "Arrêter");

    public static string ConfirmTitle => T("Confirm purchase", "購入確認", "Kauf bestätigen", "Confirmer l'achat");
    public static string ConfirmItem => T("Item:", "アイテム:", "Artikel:", "Article :");
    public static string ConfirmAmount => T("Quantity:", "数量:", "Menge:", "Quantité :");
    public static string ConfirmUnit => T("Unit price:", "単価:", "Stückpreis:", "Prix unitaire :");
    public static string ConfirmTotal => T("Estimated total:", "推定合計:", "Geschätzte Summe:", "Total estimé :");
    public static string ConfirmHaveGil => T("Current gil:", "現在の所持金:", "Aktuelle Gil:", "Gils actuels :");
    public static string ConfirmCannotAfford => T(
        "Warning: not enough gil to complete this purchase.",
        "注意: 所持金が足りません。途中で停止します。",
        "Warnung: Nicht genug Gil, um diesen Kauf abzuschließen.",
        "Attention : pas assez de gils pour cet achat.");
    public static string ConfirmEstimateOnly => T(
        "Quantity is estimated; actual amount stops when condition is met.",
        "数量は推定値です。条件を満たした時点で停止します。",
        "Menge ist geschätzt; Vorgang stoppt, sobald die Bedingung erfüllt ist.",
        "La quantité est estimée ; l'opération s'arrête dès que la condition est remplie.");
    public static string ConfirmStart => T("Start", "開始", "Starten", "Démarrer");

    public static string Running => T("Running…", "実行中…", "Läuft…", "En cours…");
    public static string Bought => T("Bought {0}", "{0} 個購入", "{0} gekauft", "{0} acheté(s)");

    public static string Reasons_GilLow => T(
        "Stopped: gil floor reached.",
        "停止: 所持金が下限に達しました。",
        "Gestoppt: Gil-Untergrenze erreicht.",
        "Arrêt : plancher de gils atteint.");
    public static string Reasons_NotEnoughGil => T(
        "Stopped: not enough gil.",
        "停止: 所持金が不足しました。",
        "Gestoppt: nicht genug Gil.",
        "Arrêt : pas assez de gils.");
    public static string Reasons_InventoryFull => T(
        "Stopped: inventory full.",
        "停止: インベントリが満杯です。",
        "Gestoppt: Inventar voll.",
        "Arrêt : inventaire plein.");
    public static string Reasons_TargetReached => T(
        "Done: target reached.",
        "完了: 目標に達しました。",
        "Fertig: Ziel erreicht.",
        "Terminé : objectif atteint.");
    public static string Reasons_DoneCount => T(
        "Done: requested count purchased.",
        "完了: 指定個数を購入しました。",
        "Fertig: gewünschte Anzahl gekauft.",
        "Terminé : quantité demandée achetée.");
    public static string Reasons_ShopClosed => T(
        "Stopped: shop window closed.",
        "停止: ショップが閉じられました。",
        "Gestoppt: Händlerfenster wurde geschlossen.",
        "Arrêt : la boutique a été fermée.");
    public static string Reasons_Cancelled => T(
        "Stopped: cancelled by user.",
        "停止: ユーザによりキャンセルされました。",
        "Gestoppt: vom Benutzer abgebrochen.",
        "Arrêt : annulé par l'utilisateur.");
    public static string Reasons_NoItem => T(
        "Stopped: no shop item selected.",
        "停止: 商品が選択されていません。",
        "Gestoppt: kein Artikel ausgewählt.",
        "Arrêt : aucun article sélectionné.");
    public static string Reasons_Unexpected => T(
        "Stopped: unexpected error.",
        "停止: 予期しないエラーが発生しました。",
        "Gestoppt: unerwarteter Fehler.",
        "Arrêt : erreur inattendue.");

    public static string SettingsAnchor => T(
        "Anchor window next to the shop",
        "ショップウィンドウ横にアンカー",
        "Fenster neben Händler ankern",
        "Ancrer la fenêtre près du marchand");
    public static string SettingsAutoOpen => T(
        "Auto-open with shop",
        "ショップを開いたら自動表示",
        "Mit Händler automatisch öffnen",
        "Ouverture automatique avec le marchand");
    public static string SettingsLanguage => T("Language", "言語", "Sprache", "Langue");
    public static string SettingsDelay => T(
        "Delay between purchases (ms)",
        "購入間隔 (ms)",
        "Verzögerung zwischen Käufen (ms)",
        "Délai entre achats (ms)");
    public static string SettingsSave => T("Save", "保存", "Speichern", "Enregistrer");

    public static string TipUseShop => T(
        "Pick an item from the list, then choose a stop condition.",
        "リストから商品を選び、停止条件を指定してください。",
        "Wähle einen Artikel aus der Liste und dann eine Stoppbedingung.",
        "Choisis un article dans la liste, puis une condition d'arrêt.");
}
