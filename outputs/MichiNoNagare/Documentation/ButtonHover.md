# ホバー背景修正 / 2026-10-30
NetworkHud.Buttonの内側だけの矩形オーバーレイを削除。Frameに任意の背景色を受け取らせ、枠線と同じ切り欠き形状でホバー背景を描画する。選択状態の枠・色分け・操作ゲートは維持。

WebGL/Macビルド成功、ビルド内の既存チェックすべてPASS。ブラウザで観測ボタンをクリックし、カーソルが乗った状態で背景が切り欠き枠に沿うことを目視確認。

画面: ../../lumen-grid-button-hover.png
Mac最新: ../../LumenGrid-UI-Mac/LumenGrid.app
