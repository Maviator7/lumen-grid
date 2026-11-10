# ブラウザ版 / unityroom向け

Unity 6000.3.11f1、PCのキーボード・マウスを対象にしたWebGLリリース。

## ビルドとローカル起動

Unityエディターを閉じ、プロジェクト直下で実行:

```sh
Tools/build-web.sh
python3 Tools/serve-web.py
```

http://127.0.0.1:8766/ を開く。保存を引き継ぐには同じブラウザ・同じアドレスとポートを使用する。サーバーは127.0.0.1のみで待ち受ける。index.htmlを直接ダブルクリックするfile://起動には対応しない。

出力先は `../LumenGrid-Web`。`MICHI_WEB_BUILD_PATH` で変更可。カスタム保存先をサーブする場合は `--directory` を指定する。

## unityroom用設定

- 非DevelopmentのWebGLリリース / IL2CPP。
- Gzip、Decompression Fallback無効、スレッド無効。
- メモリ初期256MB、最大1024MB、段階的拡張。
- link.xmlで実行時のCreatePrimitiveに必要な描画・コライダー型を保持。ビルド時に削除されると、都市生成が途中で止まる。
- 同梱index.htmlはローカル・一般ホスティング用のロゴ入りローダー。unityroomの埋込ページはunityroom側のローダーを使うため、このHTMLのキー抑止や解像度設定には依存しない。ゲーム内の保存ボタンも使用可能。

投稿する際はunityroomの管理画面でゲームを作成し、解凍したBuild内の以下ファイルを対応する項目へ指定する:

- `LumenGrid-Web.data.gz`
- `LumenGrid-Web.wasm.gz`
- `LumenGrid-Web.framework.js.gz`
- `LumenGrid-Web.loader.js`

今回、アカウント上での投稿・公開は実施していない。unityroomの実際の埋込ページでは追加の動作確認が必要。

## 操作と保存

- 左クリックでノード→ノードをつなぐ。既存の回線をクリックすると選択中の種類へ変更。
- 左ドラッグで移動、ホイールで拡大、Q/E長押しで滑らかに回転、Vで都市全体。
- 1～5でツール、Spaceで停止・再開、Tabで速度、Xで拡張。
- 新規都市とセーブ読込直後は一時停止。右上「都市を再開」またはSpaceで運転開始。
- Mでタイトル。Hで操作ガイド。Gで新しいシード。
- 左下「都市を保存」で手動保存。30秒ごとと区切りの操作でも自動保存。
- セーブはブラウザ内。Macアプリ版、別ブラウザ・別端末、127.0.0.1とlocalhost、別ポートの間では共有しない。サイトデータ削除で失われる場合がある。
- PC優先。タッチ端末・スマートフォン向けUIは未調整。

## 公式資料

https://help.unityroom.com/unityroom-351dc3ed5de980eebd79eef3b153be31
https://docs.unity3d.com/ja/6000.0/ScriptReference/GameObject.CreatePrimitive.html
