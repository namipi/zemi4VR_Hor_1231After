# 実装計画 - ステージスクロールとOSC送信機能の強化

## 目的
ステージの無限スクロール機能の実装、および特定の敵接触時の外部連携（OSC）と、演出用の遅延付き変数操作機能を追加する。

## 変更内容

### [MODIFY] [DieOnPlayerContact.cs](file:///c:/CreativeProjects/projects/zemi4/01_Projects/Unity/zemi4VR_Hor_After1231/Assets/Script/After1231/DieOnPlayerContact.cs)
- 特定のオブジェクト名（デフォルト: "Chuboss"）を含む場合にOSCメッセージを送信する機能を追加。
- `SendOSC` コンポーネントへの参照フィールドを追加。

### [NEW] [DelayedFunctionVariableSetter.cs](file:///c:/CreativeProjects/projects/zemi4/01_Projects/Unity/zemi4VR_Hor_After1231/Assets/Script/After1231/DelayedFunctionVariableSetter.cs)
- `FunctionVariableSetter` の遅延実行版。
- `OnEnable` 時や任意のタイミングから指定秒数後に変数を書き換える。

### [NEW] [InfiniteStageScroller.cs](file:///c:/CreativeProjects/projects/zemi4/01_Projects/Unity/zemi4VR_Hor_After1231/Assets/Script/After1231/InfiniteStageScroller.cs)
- 指定したPrefabを一定方向にスクロール。
- 指定距離（`stageLength`）移動するごとに新しいクローンを生成し、古いものを削除。
- 生成されたクローンはこのスクリプトを持つオブジェクトの子要素になる。

## 検証プラン
### 自動テスト・手動検証
- [ ] Inspectorで `DieOnPlayerContact` に `SendOSC` をセットし、Chuboss衝突時にログとOSCが飛ぶか確認。
- [ ] `DelayedFunctionVariableSetter` で遅延時間が正しく動作するか確認。
- [ ] `InfiniteStageScroller` でステージが隙間なく生成され、Hierarchyが子要素で整理されているか確認。
