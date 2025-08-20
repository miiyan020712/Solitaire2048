# Ads module (Runtime)
- Namespace: App.Ads
- Files:
  - AdManager.cs …… Load/Showの統括（シングルトン予定）
  - AdEvents.cs …… イベント定義（enum/型）
- Data (ScriptableObject in ScriptableObjects/Ads):
  - AdPolicy.asset …… 頻度/クールダウン/セッション上限
  - AdUnitIds.asset …… 開発ID/本番ID

# Events (Game側から呼ぶ/購読する想定)
- OnGameStart
- OnResultShown
- OnRetryPressed
- OnRewardRequested(type)   // PassPlus, BonusDrawPlus, EmptyColumnUpgrade, ContinueOnNoMove
- OnAdShown(kind)
- OnAdFailed(kind, reason)

# Policy (初期案)
- Interstitial: リザルト後のみ / CD=120s / セッション最大3回 / 直前にReward視聴なら抑止
- Rewarded: 1ラン最大3回 / 同種連続使用不可 / 1手クールダウン
- Banner: 常時（チュートリアル中は非表示）
