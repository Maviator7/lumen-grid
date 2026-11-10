# LUMEN / GRID

夜の都市に電力とデータを届ける、Unity製の3Dネットワーク設計パズル。

Unityプロジェクトは [`outputs/MichiNoNagare`](outputs/MichiNoNagare) にあります。Unity Hubではこのディレクトリを開いてください。使用Editorは **Unity 6000.3.27f2** です。

## 開発

このリポジトリのルートから実行します。

```sh
# コアの検証
outputs/MichiNoNagare/Tools/check-core.sh

# Webビルド
outputs/MichiNoNagare/Tools/build-web.sh

# Web版を起動（http://127.0.0.1:8766/）
python3 outputs/MichiNoNagare/Tools/serve-web.py

# Macビルド（Unity Editorを閉じて実行）
outputs/MichiNoNagare/Tools/build-mac.sh
```

詳細はプロジェクト内の [`AGENTS.md`](outputs/MichiNoNagare/AGENTS.md)、[`README.md`](outputs/MichiNoNagare/README.md)、[`Documentation`](outputs/MichiNoNagare/Documentation) を参照してください。

## Git管理

`main` を開発の基準とします。Unityの `Assets` と `.meta`、`Packages`、`ProjectSettings`、検証ツールと仕様書を管理します。Unityのキャッシュ、ビルド成果物、スクリーンショット、過去の試作、作業ファイルは除外しています。初回クローン後、Unityがキャッシュを再生成します。

```sh
git status
git diff
# 変更内容を確認してから対象ファイルを追加する
git add <変更したファイル>
git commit -m "変更内容"
```

Gitの履歴はゲームのセーブとは別です。ブラウザのセーブや復旧データをリポジトリで共有しないでください。Web版の既存セーブを使う場合は、これまでと同じChromeの `http://127.0.0.1:8766/` を利用します。

リモートリポジトリは未設定です。GitHubなどの保存先を決めたら追加できます。
