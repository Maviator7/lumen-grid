# WebGL検証 / 2026-10-30

- Unity6000.3.11f1 WebGLリリース成功。IL2CPP、Gzip、Decompression Fallback無効、threads無効。ブラウザ転送用Build4ファイル合計19,652,431 bytes。配布ZIP 19,620,241 bytes、CRC検証成功。
- ビルド実行中の既存コア・シード・拡張・保存検証すべてPASS（保存1211、永続保存8を含む）。
- 初回実機はBoxCollider型のストリッピングでCreatePrimitiveが失敗しNullReference。公式APIの必要型と照合しlink.xmlでMeshFilter/MeshRenderer/Box/Sphere/Capsule/MeshColliderを保持。再ビルド後に都市・タイトル・HUDすべて表示。修正後のブラウザ実行ログにerrorなし。
- ローカルサーバーはGzip実データ、Content-Encoding:gzip、JS/wasmのMIME、404がGzipを偽装しないことをHTTPで検証済み。
- in-app browser、1280x720で実機操作。タイトルとシード生成ボタンをクリック。Spaceで停止。2で送電ツール、中継①→③を実際にクリックして建設。3で既存の①→中央ハブ回線をクリックし通信専用へ変更。59.0/67、専用2/3、橋3/3を確認。
- 4倍速で運転し電力100%、通信100%、平均遅延1.93s、安定45秒に到達。Xで拡張候補表示、北の医療地区をボタンクリックで開放。
- 4地区・拡張1回、追加区画は未接続のまま電力78%、接続出力7.0/全14.0、回線72.0/116、専用2/5、橋3/4、送信待ち1・移動中14。Spaceで停止し、左下の保存ボタンをクリックして完了通知を確認。
- ページ再読込後、タイトルに同じseed291026・稼働283秒・最終保存日時を表示。「つづける」をクリックし上記の数値、北地区中心のカメラ、4倍速、停止状態を復元。Mac保存とは独立したブラウザ保存で動作。
- Unity内部からpersistentDataPath同期の将来廃止に関するwarningあり。保存・再読込は成功。現バージョンでは非致命的。公式unityroom埋込ローダーでも再確認が必要。
- スマートフォン、他のブラウザ、低性能端末でのfps測定、実際のunityroom上での埋込と保存は未検証。公開投稿は行っていない。

画面: ../../lumen-grid-web-restored.png
URL: http://127.0.0.1:8766/
配布: ../../LumenGrid-unityroom.zip
