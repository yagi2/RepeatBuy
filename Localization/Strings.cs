namespace RepeatBuy.Localization;

public enum Language
{
    English = 0,
    Japanese = 1,
    German = 2,
    French = 3,
    Chinese = 4,
    Korean = 5
}

public static class Strings
{
    private static Language lang = Language.English;
    public static void SetLanguage(Language l) => lang = l;
    public static Language Current => lang;

    private static string T(string en, string ja, string de, string fr, string zh, string ko) => lang switch
    {
        Language.Japanese => ja,
        Language.German => de,
        Language.French => fr,
        Language.Chinese => zh,
        Language.Korean => ko,
        _ => en
    };

    public static string LanguageNameEnglish => "English";
    public static string LanguageNameJapanese => "日本語";
    public static string LanguageNameGerman => "Deutsch";
    public static string LanguageNameFrench => "Français";
    public static string LanguageNameChinese => "简体中文";
    public static string LanguageNameKorean => "한국어";

    public static string WindowTitle => T("RepeatBuy", "リピート購入", "RepeatBuy", "RepeatBuy", "重复购买", "반복 구매");
    public static string ConfigWindowTitle => T(
        "RepeatBuy Settings",
        "リピート購入 設定",
        "RepeatBuy-Einstellungen",
        "Paramètres RepeatBuy",
        "RepeatBuy 设置",
        "RepeatBuy 설정");

    public static string SelectedItem => T("Selected:", "選択中:", "Ausgewählt:", "Sélectionné :", "已选择:", "선택됨:");
    public static string NoItemSelected => T(
        "(pick an item from the list below)",
        "（下のリストから商品を選んでください）",
        "(Wähle einen Artikel aus der Liste unten)",
        "(choisis un article dans la liste ci-dessous)",
        "（请从下方列表中选择一个物品）",
        "(아래 목록에서 항목을 선택하세요)");
    public static string ShopList => T("Shop items", "ショップ商品一覧", "Händler-Artikel", "Articles du marchand", "商店物品", "상점 물품");
    public static string ShopListEmpty => T(
        "(shop is empty or still loading)",
        "（ショップが空、または読み込み中）",
        "(Händler ist leer oder lädt noch)",
        "(boutique vide ou en cours de chargement)",
        "（商店为空或正在加载）",
        "(상점이 비어 있거나 불러오는 중)");
    public static string UnitPrice => T("Unit price:", "単価:", "Stückpreis:", "Prix unitaire :", "单价:", "단가:");
    public static string Owned => T("Owned:", "所持数:", "Im Besitz:", "Possédé :", "拥有:", "보유:");
    public static string Gil => T("Gil:", "所持金:", "Gil:", "Gils :", "金币:", "길:");
    public static string ColumnName => T("Item", "アイテム", "Artikel", "Article", "物品", "항목");
    public static string ColumnPrice => T("Price", "価格", "Preis", "Prix", "价格", "가격");
    public static string ColumnOwned => T("Have", "所持", "Bestand", "Stock", "库存", "보유");

    public static string ModeHeader => T("Stop condition", "停止条件", "Stoppbedingung", "Condition d'arrêt", "停止条件", "정지 조건");
    public static string ModeFixed => T("Fixed count", "個数指定", "Feste Anzahl", "Quantité fixe", "固定数量", "고정 수량");
    public static string ModeUntilOwned => T("Until owned", "所持数到達まで", "Bis Bestand erreicht", "Jusqu'à possession", "直到达到目标数量", "보유 수량 도달까지");
    public static string ModeGilFloor => T("Gil floor", "残高指定", "Gil-Untergrenze", "Plancher de gils", "金币下限", "길 하한");
    public static string ModeInventoryFull => T("Inventory full", "インベントリ満杯まで", "Inventar voll", "Inventaire plein", "直到背包已满", "가방이 가득 찰 때까지");

    public static string LabelCount => T("Count", "個数", "Anzahl", "Quantité", "数量", "수량");
    public static string LabelTarget => T("Target owned", "所持数目標", "Ziel-Bestand", "Possession cible", "目标拥有量", "목표 보유 수");
    public static string LabelGilFloor => T("Stop when gil <", "停止しきい値", "Stoppen wenn Gil <", "Arrêter si gils <", "金币 < 时停止", "길이 < 일 때 정지");

    public static string EstimateBuy => T("Buy", "購入", "Kaufen", "Acheter", "购买", "구매");
    public static string Cancel => T("Cancel", "キャンセル", "Abbrechen", "Annuler", "取消", "취소");
    public static string Stop => T("Stop", "停止", "Stopp", "Arrêter", "停止", "정지");

    public static string ConfirmTitle => T("Confirm purchase", "購入確認", "Kauf bestätigen", "Confirmer l'achat", "确认购买", "구매 확인");
    public static string ConfirmItem => T("Item:", "アイテム:", "Artikel:", "Article :", "物品:", "항목:");
    public static string ConfirmAmount => T("Quantity:", "数量:", "Menge:", "Quantité :", "数量:", "수량:");
    public static string ConfirmUnit => T("Unit price:", "単価:", "Stückpreis:", "Prix unitaire :", "单价:", "단가:");
    public static string ConfirmTotal => T("Estimated total:", "推定合計:", "Geschätzte Summe:", "Total estimé :", "预计总额:", "예상 합계:");
    public static string ConfirmHaveGil => T("Current gil:", "現在の所持金:", "Aktuelle Gil:", "Gils actuels :", "当前金币:", "현재 길:");
    public static string ConfirmCannotAfford => T(
        "Warning: not enough gil to complete this purchase.",
        "注意: 所持金が足りません。途中で停止します。",
        "Warnung: Nicht genug Gil, um diesen Kauf abzuschließen.",
        "Attention : pas assez de gils pour cet achat.",
        "警告：金币不足，将在途中停止。",
        "경고: 길이 부족하여 도중에 정지합니다.");
    public static string ConfirmEstimateOnly => T(
        "Quantity is estimated; actual amount stops when condition is met.",
        "数量は推定値です。条件を満たした時点で停止します。",
        "Menge ist geschätzt; Vorgang stoppt, sobald die Bedingung erfüllt ist.",
        "La quantité est estimée ; l'opération s'arrête dès que la condition est remplie.",
        "数量为估算值，达成条件时即停止。",
        "수량은 추정치이며, 조건이 충족되면 정지합니다.");
    public static string ConfirmStart => T("Start", "開始", "Starten", "Démarrer", "开始", "시작");

    public static string Running => T("Running…", "実行中…", "Läuft…", "En cours…", "进行中…", "진행 중…");
    public static string StatusWaiting => T(
        "Waiting for the purchase to finish…",
        "購入の完了を待っています…",
        "Warte auf den Kaufabschluss…",
        "En attente de la fin de l'achat…",
        "正在等待购买完成…",
        "구매가 끝날 때까지 대기 중…");
    public static string StatusConfirming => T(
        "Confirming the purchase…",
        "購入を確認しています…",
        "Kauf wird bestätigt…",
        "Confirmation de l'achat…",
        "正在确认购买…",
        "구매를 확인하는 중…");
    public static string Bought => T("Received {0}", "{0} 個受け取り", "{0} erhalten", "{0} reçu(s)", "已收到 {0}", "{0}개 수령");
    public static string GilSpent => T("Gil spent: {0}", "使用ギル: {0}", "Gil ausgegeben: {0}", "Gils dépensés : {0}", "已花费金币: {0}", "사용한 길: {0}");

    public static string Reasons_GilLow => T(
        "Stopped: gil floor reached.",
        "停止: 所持金が下限に達しました。",
        "Gestoppt: Gil-Untergrenze erreicht.",
        "Arrêt : plancher de gils atteint.",
        "已停止：达到金币下限。",
        "정지: 길 하한에 도달했습니다.");
    public static string Reasons_NotEnoughGil => T(
        "Stopped: not enough gil.",
        "停止: 所持金が不足しました。",
        "Gestoppt: nicht genug Gil.",
        "Arrêt : pas assez de gils.",
        "已停止：金币不足。",
        "정지: 길이 부족합니다.");
    public static string Reasons_InventoryFull => T(
        "Stopped: inventory full.",
        "停止: インベントリが満杯です。",
        "Gestoppt: Inventar voll.",
        "Arrêt : inventaire plein.",
        "已停止：背包已满。",
        "정지: 가방이 가득 찼습니다.");
    public static string Reasons_TargetReached => T(
        "Done: target reached.",
        "完了: 目標に達しました。",
        "Fertig: Ziel erreicht.",
        "Terminé : objectif atteint.",
        "完成：已达到目标。",
        "완료: 목표에 도달했습니다.");
    public static string Reasons_DoneCount => T(
        "Done: requested count purchased.",
        "完了: 指定個数を購入しました。",
        "Fertig: gewünschte Anzahl gekauft.",
        "Terminé : quantité demandée achetée.",
        "完成：已购买指定数量。",
        "완료: 지정 수량을 구매했습니다.");
    public static string Reasons_ShopClosed => T(
        "Stopped: shop window closed.",
        "停止: ショップが閉じられました。",
        "Gestoppt: Händlerfenster wurde geschlossen.",
        "Arrêt : la boutique a été fermée.",
        "已停止：商店窗口已关闭。",
        "정지: 상점 창이 닫혔습니다.");
    public static string Reasons_Cancelled => T(
        "Stopped: cancelled by user.",
        "停止: ユーザによりキャンセルされました。",
        "Gestoppt: vom Benutzer abgebrochen.",
        "Arrêt : annulé par l'utilisateur.",
        "已停止：用户已取消。",
        "정지: 사용자가 취소했습니다.");
    public static string Reasons_NoItem => T(
        "Stopped: no shop item selected.",
        "停止: 商品が選択されていません。",
        "Gestoppt: kein Artikel ausgewählt.",
        "Arrêt : aucun article sélectionné.",
        "已停止：未选择商品。",
        "정지: 상품이 선택되지 않았습니다.");
    public static string Reasons_NotReceived => T(
        "Stopped: the purchase did not arrive.",
        "停止: 購入が届きませんでした。",
        "Gestoppt: Der Kauf ist nicht angekommen.",
        "Arrêt : l'achat n'est pas arrivé.",
        "已停止：购买没有到账。",
        "정지: 구매한 물건이 도착하지 않았습니다.");
    public static string Reasons_Buyback => T(
        "Stopped: the buyback tab is open.",
        "停止: バイバックタブが開いています。",
        "Gestoppt: Der Rückkauf-Tab ist offen.",
        "Arrêt : l'onglet de rachat est ouvert.",
        "已停止：回购标签页处于打开状态。",
        "정지: 재매입 탭이 열려 있습니다.");
    public static string Reasons_Retainer => T(
        "Stopped: this is a retainer trade.",
        "停止: リテイナーとの取引です。",
        "Gestoppt: Dies ist ein Diener-Handel.",
        "Arrêt : il s'agit d'un échange avec un servant.",
        "已停止：这是与雇员的交易。",
        "정지: 집사와의 거래입니다.");
    public static string Reasons_NotBuyMode => T(
        "Stopped: switch to the buy tab.",
        "停止: 購入タブに切り替えてください。",
        "Gestoppt: Wechsle zum Kauf-Tab.",
        "Arrêt : passe à l'onglet d'achat.",
        "已停止：请切换到购买标签页。",
        "정지: 구매 탭으로 전환하세요.");
    public static string Reasons_Unique => T(
        "Stopped: you already own this unique item.",
        "停止: このユニークアイテムは所持済みです。",
        "Gestoppt: Dieser einzigartige Gegenstand ist bereits im Besitz.",
        "Arrêt : cet objet unique est déjà possédé.",
        "已停止：你已经拥有这个独特物品。",
        "정지: 이 고유 아이템을 이미 보유하고 있습니다.");
    public static string Reasons_RowGone => T(
        "Stopped: that item is no longer in the shop list.",
        "停止: その商品はショップ一覧にありません。",
        "Gestoppt: Der Artikel ist nicht mehr in der Händlerliste.",
        "Arrêt : cet article n'est plus dans la liste.",
        "已停止：该物品已不在商店列表中。",
        "정지: 그 항목이 상점 목록에 더 이상 없습니다.");
    public static string Reasons_Unexpected => T(
        "Stopped: unexpected error.",
        "停止: 予期しないエラーが発生しました。",
        "Gestoppt: unerwarteter Fehler.",
        "Arrêt : erreur inattendue.",
        "已停止：发生意外错误。",
        "정지: 예기치 않은 오류가 발생했습니다.");

    public static string SettingsAnchor => T(
        "Anchor window next to the shop",
        "ショップウィンドウ横にアンカー",
        "Fenster neben Händler ankern",
        "Ancrer la fenêtre près du marchand",
        "将窗口固定在商店旁",
        "상점 옆에 창 고정");
    public static string SettingsAutoConfirm => T(
        "Accept the shop confirmation dialog",
        "ショップの確認ダイアログを自動で承諾",
        "Händler-Bestätigung automatisch annehmen",
        "Accepter la confirmation du marchand",
        "自动接受商店确认对话框",
        "상점 확인 창 자동 수락");
    public static string SettingsAutoOpen => T(
        "Auto-open with shop",
        "ショップを開いたら自動表示",
        "Mit Händler automatisch öffnen",
        "Ouverture automatique avec le marchand",
        "打开商店时自动显示",
        "상점 열 때 자동 표시");
    public static string SettingsLanguage => T("Language", "言語", "Sprache", "Langue", "语言", "언어");
    public static string SettingsDelay => T(
        "Delay between purchases (ms)",
        "購入間隔 (ms)",
        "Verzögerung zwischen Käufen (ms)",
        "Délai entre achats (ms)",
        "购买间隔 (毫秒)",
        "구매 간격 (밀리초)");
    public static string SettingsSave => T("Save", "保存", "Speichern", "Enregistrer", "保存", "저장");

    public static string BuyBlockedNoSelection => T(
        "Pick an item from the list first.",
        "先にリストから商品を選んでください。",
        "Wähle zuerst einen Artikel aus der Liste.",
        "Choisis d'abord un article dans la liste.",
        "请先从列表中选择一个物品。",
        "먼저 목록에서 항목을 선택하세요.");
    public static string BuyBlockedNoPrice => T(
        "This item has no gil price.",
        "この商品にはギル価格がありません。",
        "Dieser Artikel hat keinen Gil-Preis.",
        "Cet article n'a pas de prix en gils.",
        "这个物品没有金币价格。",
        "이 항목에는 길 가격이 없습니다.");
    public static string BuyBlockedNothing => T(
        "Nothing left to buy for this stop condition.",
        "この停止条件では購入できるものがありません。",
        "Für diese Stoppbedingung bleibt nichts zu kaufen.",
        "Rien à acheter pour cette condition d'arrêt.",
        "按这个停止条件，已经没有可买的数量。",
        "이 정지 조건으로는 더 살 수량이 없습니다.");
    public static string EstimateCapped => T(
        "Quantity was reduced to what you can afford and carry.",
        "所持金と所持スペースに合わせて数量を減らしました。",
        "Menge wurde auf das begrenzt, was du bezahlen und tragen kannst.",
        "La quantité a été réduite à ce que tu peux payer et porter.",
        "数量已减少到你付得起且带得走的范围。",
        "수량을 지불하고 소지할 수 있는 범위로 줄였습니다.");
    public static string TipUseShop => T(
        "Pick an item from the list, then choose a stop condition.",
        "リストから商品を選び、停止条件を指定してください。",
        "Wähle einen Artikel aus der Liste und dann eine Stoppbedingung.",
        "Choisis un article dans la liste, puis une condition d'arrêt.",
        "请从列表中选择一个物品，然后选择停止条件。",
        "목록에서 항목을 선택한 다음 정지 조건을 선택하세요.");
}
