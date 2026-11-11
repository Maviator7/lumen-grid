# LUMEN GRID（旧みちの流れ）
Unity 6000.3.27f2 / C#。電力とデータの供給網を設計する、夜の都市の3D自動化パズル。

## コマンド
- 検証: `Tools/check-core.sh`
- Webビルド: `Tools/build-web.sh` / ローカル確認: `python3 Tools/serve-web.py`（127.0.0.1:8766）
- Macビルド: `Tools/build-mac.sh`（Unityエディターを閉じて実行）
- エディター内: `Michi` メニュー

## 構成
- WebBuildはunityroom指定のGzip/展開フォールバック無効のリリースを生成。Assets/Michi/link.xmlはCreatePrimitiveの型を保持する。消すとIL2CPPで都市生成が失敗する。
- OrbitRotationは実時間の指数追従でQ/E長押しと回転ボタンを補間。Angleの設定は保留回転を破棄。モーダル・タイトル・フォーカス喪失時に回転を止める。
- TitleGame/TitleHudが起動メニューを担当。既存セッションのタイトル中は入力・計算を停止し、Mで復帰。Continueは本編の永続セーブを復元。Core/NetworkSaveが計算状態、CitySaveGame/CitySaveStoreが1枠のJSON保存・バックアップ・10秒自動保存と配線編集後保存を担当。操作ガイドは本編の保存を上書きしない。
- CityMapはv1の整数乱数で都市を生成。制約を満たす参照配線を実シミュレーションで検証する。生成器変更時はシード再現性とバージョンを扱う。
- ロゴの編集元はAssets/Michi/BrandingSourceのSVG。Resources/BrandingのPNGをHUDとビルド用アイコンに使用。ProjectTools.ConfigureBrandingがインポート設定とアイコンを適用。
- 新しい都市へ切り替える際はView.Rebuildで旧シーンルート・生成マテリアルを回収する。
- 新規都市はNetworkSimulation.CurrentRulesVersion=1（地区別期限・住宅ピーク・専用資材×1.75）。既存セーブはRulesVersion省略時0でクラシック維持。地形生成器v1は維持。費用はCableCost/CableCostLimit、物理距離はCableLengthで区別。Tools/check-strategy.shが旧セーブ互換と新ルールを検証。
- 本編はContinuous=trueの継続運転。45秒連続安定で拡張、15秒窓で品質を判定。拡張候補はクローン上で検証してから確定する。ExpansionDirectionはNone=0を旧保存用に予約し、候補の順序は北・南・東・西。北南は従来生成を維持、東西は密集/迂回地形。開放ボタンとXは条件未達でも確認画面を開き、未達理由を表示する。ExpansionConfirmationが明示選択と確定を分離。Tools/check-expansion-dialog.shで条件喪失・キャンセル・二重確定も検証。
- DataSupplyは本編の役割別通信需要と7件/sのセンター出力を計算する。DistrictDataConnectedと地区別の警告件数は現在の経路・電力を示し、直近15秒の成功実績とは分けて表示する。待ち要求も生成時刻から期限を数える（クラシック5秒、新ルールは地区別）。成功率から需要を黙って消さない。固定ガイドの旧spawnは維持。
- 施設・供給源・需要区画の数は可変。表示と計算で固定ID範囲を仮定しない。Roleで建物種別を識別。
- ガイド完了時はNextTutorialを通じて本編のシード生成へ進む（左ボタン・右上・Enter共通）。都市初期化は拡張も消すことを確認画面で説明し、一時停止で再開する。
- PreviewConnectは経路編集の非破壊見積もり。Connect/Paintも同じ判定を使う。NetworkPlanningHudが候補への吸着・可否・追加資材・確定後予算を表示する。NetworkLabelsは優先度付きでHUDとラベル同士の重なりを避け、通常の説明は選択/ホバー時に限る。
- NetworkTutorialは計算結果に基づく操作ガイドの状態機械。CanAdvanceでボタンとEnterの進行条件を統一。中央ハブ選択後は明示的な次へを待ち、ShowsHubDetailsで配線前後の負荷を観察できる。入力ゲートはHUD・キー・ワールド操作すべてで共通化する。
- PowerInspectionは実際の配分経路を記録し、中継の待機理由と発電所別MWを表示。配分ごとに診断キャッシュを破棄。地区の供給元に他地区の配分を混ぜない。派生記録は保存しない。Tools/check-power-inspection.shで検証。
- Assets/Michi/Scripts/Core: Unityに依存しない電力配分・パケット・判定。旧交通コアも比較用に保持。
- Assets/Michi/Scripts/Runtime: 3D表示、入力、HUD。
- Assets/Michi/Editor: シーン生成、検証、Macビルド。
- Documentation: 仕様と変更記録。
- Tools: バッチ実行スクリプト。

- NetworkGameにはF2/F3などの配線を直接置換する開発キーを設けない。初期化はResetConfirmation経由に限定。Tools/check-input-safety.pyで入力経路の再混入を検出する。

## 方針
- ユーザーの本編セーブはChromeの http://127.0.0.1:8766/ にある。8765やアプリ内ブラウザは別保存先。復旧・更新後の案内は同じブラウザ・同じ8766を使い、テスト環境の起動だけでユーザーのセーブ復旧を確認したと言わない。
- 固定刻み・同じ需要で、設計を比較できるようにする。
- 供給・通信の計算をMonoBehaviourに混ぜない。
- メインシーンはNocturne。旧Riversideは参考として残す。
- NetworkBuildingMotionは既存の稼働量とsim.Timeでコイル・サーバー灯・窓・医療サイン・工業蒸気を動かす。稼働アニメは一時停止で止まり、供給やセーブ形式は変えない。登録リストはDisposeSceneで破棄。毎フレームのGameObject/Material生成は禁止。
- NetworkMetalLookが設備の金属素材・研磨テクスチャ・共有巻線メッシュ・静的反射環境を所有。反射設定の退避はcustomReflectionTexture（Texture型）で行い、Rebuild/Disposeで以前の設定を復元する。Webの見た目変更はMICHI_WEB_BUILD_PATHで別フォルダへビルドして起動検証後に公開先へ差し替える。
- 生成した描画素材は所有権を追跡し、回収・終了時に破棄する。
- Library / Temp / Logs / UserSettings / Buildsはソース管理しない。
- Unityが生成する.metaはソースと一緒に扱う。
- 個人プロジェクトとして開始。エージェントのワーカー定義はホーム側を使う。
- コマンドや構成が変わったら、このファイルも更新する。

## Git
- コミットメッセージは `gitmoji接頭辞: 日本語の説明` の形式にする（例: `✨: 東西への地区拡張と地形制約を追加`、`🐛: セーブ復元時の不具合を修正`、`📝: コミットルールを記録`）。
- リポジトリのルートはこのプロジェクトの2階層上（new-chat）。mainが初期ブランチ。Unityソース・設定・ツール・仕様を管理し、外側のworkやビルド・画像・復旧データは除外。
- Assetsのファイルと.metaは必ず一緒に変更する。生成キャッシュや検証日時だけのDocumentation/Verification.txtはコミットしない。ブラウザのセーブをGitに混ぜない。

## セーブの耐久性
- WebはCitySave.jslibでlocalStorageの固定キーに主保存＋バックアップを同期保存し、読み返し一致後に成功表示。旧PlayerPrefsは読み込み互換を維持。セーブ形式はCitySaveStore.Encode/DecodeのSHA-256付きJSON。
- ローカルserve-web.pyは同一オリジンの/api/city-saveで検証済みJSONをwork/city-saves/<port>へ保存。ビルドと独立し、起動時にブラウザ保存が消えてもPCバックアップを利用する。テストは別ポートを使い、本編8766のバックアップへ混ぜない。バックアップJSONをGitに追加しない。
- タイトルとフッターのセーブ管理からファイル書き出し・読み込み。読み込みは内容確認後に明示復元。タイトル中・読み込み確認中の自動保存を避ける。
- 検証: Tools/check-web-save.cjs、Tools/check-server-save.py、Editor/SaveDurabilityChecks.cs。保存の確認は実際のChromeのリロードまで行い、別ブラウザの結果だけで本編復旧と呼ばない。

## 4方向の拡張
- CardinalExpansionが東西の外縁選択・建物の台座を避ける進入路・地区の障害物を担当。地形はCitySectorが所有し、AllObstacles/IsBlockedを配線・表示・復元で共用する。既存回線の外側に追加し、旧地区は動かさない。
- CitySectorのDirectionはGrowthDirection=Noneなら旧Northフラグから推定。SavedSectorには方向・地形・DemandRoute・障害物を任意項目として保存。クローン・保存・復元で矩形と経路を共有しない。復元時は未配線のDemandRouteも長さ・障害物・連続性・重複・中間ノード種別を検証。
- Tools/check-cardinal-expansion.sh（789項目）とEditor/CardinalSaveChecks（実JSON11項目）を実行。四方向混在・鏡映・シード1の建物重なり・旧南方向の省略JSONを含む。

## 蓄電所
- BatteryStorageは中継の改装による設備（初期1枠・2拡張ごと+1）、120MJ/充電2MW/効率90%/放電4MWを所有。通常発電→不足への放電→余剰の充電の順で配分し、蓄電所同士では充電しない。全流量で中継と回線容量を共用。ライブ状態は公開読み取り専用で、SavedBatteryへ独立保存する。旧JSONの省略は設備なし、旧需要ルールは維持する。
- BatteryPowerCacheは入力・刻み幅・残量境界を検証して定常流量を再利用。充電と不足/他設備の放電が同居したら再配分する。エネルギー積分と不足時の安定解除は正のステップのみ。0秒のプレビュー・ロードで品質を変更しない。PowerFlowsは地区供給のみで、充電の記録は別管理する。
- BatteryHud/BatteryGameが中継詳細・枠/残量・改装・撤去の二段階確認を担当。選択/都市が変われば確認を解除。BatteryViewの緑のセル灯はシーン所有で、設備更新時に素材を回収しカメラを保つ。一時停止中は予定流量と停止を明示。
- Tools/check-battery.sh（5,520項目）とEditor/BatterySaveChecks（125項目）で検証。Editorの内部状態セットはテスト用リフレクションに限定し、ランタイムの開発キー・状態書き換えAPIを追加しない。
