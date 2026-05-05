namespace RepeatBuy.Localization;

public static class Strings
{
    private static bool jp;
    public static void SetLanguage(bool useJapanese) => jp = useJapanese;
    public static bool IsJapanese => jp;

    private static string T(string en, string ja) => jp ? ja : en;

    public static string WindowTitle => T("RepeatBuy", "リピート購入");
    public static string ConfigWindowTitle => T("RepeatBuy Settings", "リピート購入 設定");

    public static string SelectedItem => T("Selected:", "選択中:");
    public static string NoItemSelected => T("(pick an item from the list below)", "（下のリストから商品を選んでください）");
    public static string ShopList => T("Shop items", "ショップ商品一覧");
    public static string ShopListEmpty => T("(shop is empty or still loading)", "（ショップが空、または読み込み中）");
    public static string UnitPrice => T("Unit price:", "単価:");
    public static string Owned => T("Owned:", "所持数:");
    public static string Gil => T("Gil:", "所持金:");
    public static string ColumnName => T("Item", "アイテム");
    public static string ColumnPrice => T("Price", "価格");
    public static string ColumnOwned => T("Have", "所持");

    public static string ModeHeader => T("Stop condition", "停止条件");
    public static string ModeFixed => T("Fixed count", "個数指定");
    public static string ModeUntilOwned => T("Until owned", "所持数到達まで");
    public static string ModeGilFloor => T("Gil floor", "残高指定");
    public static string ModeInventoryFull => T("Inventory full", "インベントリ満杯まで");

    public static string LabelCount => T("Count", "個数");
    public static string LabelTarget => T("Target owned", "所持数目標");
    public static string LabelGilFloor => T("Stop when gil <", "停止しきい値");

    public static string EstimateBuy => T("Buy", "購入");
    public static string Cancel => T("Cancel", "キャンセル");
    public static string Stop => T("Stop", "停止");

    public static string ConfirmTitle => T("Confirm purchase", "購入確認");
    public static string ConfirmItem => T("Item:", "アイテム:");
    public static string ConfirmAmount => T("Quantity:", "数量:");
    public static string ConfirmUnit => T("Unit price:", "単価:");
    public static string ConfirmTotal => T("Estimated total:", "推定合計:");
    public static string ConfirmHaveGil => T("Current gil:", "現在の所持金:");
    public static string ConfirmCannotAfford => T("Warning: not enough gil to complete this purchase.", "注意: 所持金が足りません。途中で停止します。");
    public static string ConfirmEstimateOnly => T("Quantity is estimated; actual amount stops when condition is met.", "数量は推定値です。条件を満たした時点で停止します。");
    public static string ConfirmStart => T("Start", "開始");

    public static string Running => T("Running…", "実行中…");
    public static string Bought => T("Bought {0}", "{0} 個購入");

    public static string Reasons_GilLow => T("Stopped: gil floor reached.", "停止: 所持金が下限に達しました。");
    public static string Reasons_NotEnoughGil => T("Stopped: not enough gil.", "停止: 所持金が不足しました。");
    public static string Reasons_InventoryFull => T("Stopped: inventory full.", "停止: インベントリが満杯です。");
    public static string Reasons_TargetReached => T("Done: target reached.", "完了: 目標に達しました。");
    public static string Reasons_DoneCount => T("Done: requested count purchased.", "完了: 指定個数を購入しました。");
    public static string Reasons_ShopClosed => T("Stopped: shop window closed.", "停止: ショップが閉じられました。");
    public static string Reasons_Cancelled => T("Stopped: cancelled by user.", "停止: ユーザによりキャンセルされました。");
    public static string Reasons_NoItem => T("Stopped: no shop item selected.", "停止: 商品が選択されていません。");
    public static string Reasons_Unexpected => T("Stopped: unexpected error.", "停止: 予期しないエラーが発生しました。");

    public static string SettingsAnchor => T("Anchor window next to the shop", "ショップウィンドウ横にアンカー");
    public static string SettingsAutoOpen => T("Auto-open with shop", "ショップを開いたら自動表示");
    public static string SettingsLanguage => T("Use Japanese UI", "日本語UI");
    public static string SettingsDelay => T("Delay between purchases (ms)", "購入間隔 (ms)");
    public static string SettingsSave => T("Save", "保存");

    public static string TipUseShop => T("Pick an item from the list, then choose a stop condition.", "リストから商品を選び、停止条件を指定してください。");
}
