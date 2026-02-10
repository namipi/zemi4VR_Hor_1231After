# プロジェクト構造レポート（Unity）

## 概要
- プロジェクトルート: `C:\CreativeProjects\projects\zemi4\01_Projects\Unity\zemi4VR_Hor_After1231`
- 分析日: 2026-02-10
- Unityエディタ版: `6000.0.54f1`（rev `4c506e5b5cc5`）
- ルート主要項目: `Assets/`, `Packages/`, `ProjectSettings/`, `UserSettings/`, `Library/`, `Logs/`, `Temp/`, `docs/`, `.git/`, `.utmp/`, `i.apk`

## ルート構成
- `Assets/`: コンテンツ本体
- `Packages/`: パッケージ依存
- `ProjectSettings/`: プロジェクト設定
- `UserSettings/`: ユーザー設定
- `Library/`, `Logs/`, `Temp/`: Unity生成物（ソースではない）
- `docs/`: ドキュメント
- `.git/`: Gitメタデータ
- `i.apk`: ビルド済みAPK

## Assets 直下の主要フォルダ
- `Assets/Animation`: アニメーション
- `Assets/Effect`: エフェクト
- `Assets/lab`: 実験・検証用と推定
- `Assets/Material`: マテリアル
- `Assets/Model`: 3Dモデルと関連素材
- `Assets/Oculus`: Meta/Oculus系アセット
- `Assets/Particlecollection_Free samples`: パーティクル素材パック
- `Assets/Plugins`: プラグイン（`Assets/Plugins/Android`含む）
- `Assets/Prefabs`: プレハブ
- `Assets/Resources`: Resourcesロード対象
- `Assets/Scenes`: シーン
- `Assets/Script`: スクリプト
- `Assets/StreamingAssets`: StreamingAssets
- `Assets/SU3DJPFont`: 日本語フォント/TPM関連
- `Assets/TextMesh Pro`: TextMeshPro同梱アセット
- `Assets/texture`: テクスチャ/HDRI
- `Assets/XR`: XRローダー/設定

## スクリプト
- 総数: 56
- フォルダ別:
- `Assets/Script`: 15
- `Assets/Script/After1231`: 19
- `Assets/Script/After1231/Editor`: 1
- `Assets/`直下のスクリプト:
- `BukiSelect.cs`
- `Daruma.cs`
- `Doors.cs`
- `Handle.cs`
- `konnitiha.cs`
- `Koshi.cs`
- `ResetLoadScene.cs`
- `SendEmergency.cs`
- `VansTracking.cs`

## シーン（13）
- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/SampleSceneback3.unity`
- `Assets/Scenes/SampleScene_back.unity`
- `Assets/Scenes/SampleScene_back2.unity`
- `Assets/Particlecollection_Free samples/Scene/bullet_ALL.unity`
- `Assets/Particlecollection_Free samples/Scene/Cleave_ALL.unity`
- `Assets/Particlecollection_Free samples/Scene/Hit_ALL.unity`
- `Assets/Particlecollection_Free samples/Scene/Line_ALL.unity`
- `Assets/Particlecollection_Free samples/Scene/Magic_ring_ALL.unity`
- `Assets/Particlecollection_Free samples/Scene/Sprays_ALL.unity`
- `Assets/Particlecollection_Free samples/Scene/Toon.unity`
- `Assets/SU3DJPFont/Demo/SampleScene.unity`
- `Assets/Model/Dallas_City/Scenes/Dallas_City.unity`

## 主要アセット統計（拡張子上位）
- `.meta`: 606
- `.asset`: 110
- `.mat`: 69
- `.prefab`: 65
- `.png`: 60
- `.cs`: 56
- `.ttf`: 44
- `.fbx`: 24
- `.shader`: 21
- `.anim`: 16
- `.unity`: 13
- `.txt`: 10
- `.controller`: 10
- `.lighting`: 9
- `.jpg`: 7
- `.shadergraph`: 4
- `.tga`: 4
- `.cginc`: 4
- `.exr`: 3
- `.pdf`: 2

## 依存パッケージ（抜粋）
- Meta XR SDK: `com.meta.xr.sdk.all@83.0.0`
- XR Management: `com.unity.xr.management@4.5.3`
- Oculus XR Plugin: `com.unity.xr.oculus@4.5.2`
- Input System: `com.unity.inputsystem@1.14.1`
- Timeline: `com.unity.timeline@1.8.7`
- Visual Scripting: `com.unity.visualscripting@1.9.7`
- Splines: `com.unity.splines@2.8.2`
- OSC: `jp.keijiro.osc-jack@2.0.0`
- MCP: `io.github.hatayama.uloopmcp`（Git参照）

## 主要な外部/同梱コンテンツ
- `Assets/Particlecollection_Free samples`: サンプルエフェクト一式
- `Assets/TextMesh Pro`: TextMeshPro
- `Assets/SU3DJPFont`: 日本語フォントのTextMeshPro拡張
- `Assets/Oculus`: Meta/Oculus XR関連

## 関係性の構造（概観）
以下は**構造を素早く把握するためのマップ**です。実際のGameObject階層やスクリプトのアタッチ関係は、Unityが起動していないため未確定です。

### 1. コンテンツ層（Assets）
- `Scenes` → シーンの入口
- `Prefabs` → 再利用オブジェクト群
- `Model`/`Material`/`texture` → 見た目/形状
- `Animation`/`controller` → 動き
- `Effect`/`Particlecollection_Free samples` → 演出
- `Script` → ロジック

### 2. 実行系の想定接続（推定）
- `XR` + `Oculus` + Meta XR SDK → VR入力/表示
- `Script`（OSC系: `OscListener.cs`, `SendOSC.cs`, `SimpleOscReceiver.cs`, `TripleTapOSCSender.cs` など） → `jp.keijiro.osc-jack` に依存
- `Resources`/`StreamingAssets` → ランタイムロード対象
- `TextMesh Pro` + `SU3DJPFont` → UI/日本語テキスト表示

### 3. シーン構成の想定
- `Assets/Scenes/*` が本体シーン候補
- `Particlecollection_Free samples/Scene/*` はデモシーン
- `SU3DJPFont/Demo/SampleScene.unity` はフォントデモ
- `Model/Dallas_City/Scenes/Dallas_City.unity` は外部シーン素材

## 関係性の具体化に必要な情報
- `SampleScene` は実測済み（下記参照）
- それ以外のシーンは未解析
- プレハブ参照の依存ツリーは未解析
- Script同士の参照/呼び出し関係は未解析

## 次のアクション（Unity起動中に自動化可能）
- 主要シーン（`SampleScene`以外）を順に開き、Hierarchyを取得
- 各シーン内GameObjectのComponent一覧を取得
- 各プレハブの依存関係を可視化
- Scriptの参照関係（SerializeFieldやFind/Load系）を抽出

## 注記
- ルートに`i.apk`が存在（ビルド成果物の可能性）
- `Library/`, `Logs/`, `Temp/` は生成物でソースではない

## SampleScene（実測・Hierarchy）
- 取得時刻: 2026-02-10 21:04:58
- ルート数: 11
- ノード数: 707
- 最大深度: 6

### ルートオブジェクト概要
- Main Camera | Active=True | Components=[Transform, Camera, AudioListener] | Children=0
- [BuildingBlock] Camera Rig | Active=True | Components=[Transform, BuildingBlock, OVRCameraRig, OVRManager, OVRHeadsetEmulator] | Children=2
- [BuildingBlock] Passthrough | Active=True | Components=[Transform, BuildingBlock, OVRPassthroughLayer] | Children=0
- [BuildingBlock] Cube | Active=True | Components=[Transform, BuildingBlock, MeshFilter, MeshRenderer, BoxCollider, Rigidbody, Grabbable] | Children=1
- Poke Interaction | Active=True | Components=[Transform] | Children=1
- [BuildingBlock] MR Utility Kit | Active=True | Components=[Transform, BuildingBlock, MRUK] | Children=0
- [BuildingBlock] RoomModel | Active=True | Components=[Transform, BuildingBlock, AnchorPrefabSpawner] | Children=0
- Master | Active=True | Components=[Transform, SendOSC, SimpleOscReceiver, ToggleObjectWithAButton, ResetLoadScene, TripleTapOSCSender] | Children=0
- NetworkManager | Active=True | Components=[Transform, NetworkManager] | Children=0
- PlayerTracker | Active=True | Components=[Transform, PlayerTracker, HapticsPlayer] | Children=0
- GameObject | Active=True | Components=[Transform, Koshi] | Children=2

### プロジェクトスクリプトの配置（Scene内の実測）
- DieOnPlayerContact
- GameObject/Knife
- HapticsPlayer
- PlayerTracker
- konnitiha
- [BuildingBlock] Camera Rig/TrackingSpace/CenterEyeAnchor/AisatuPanel
- Koshi
- GameObject
- NetworkManager
- NetworkManager
- PlayerTracker
- PlayerTracker
- ResetLoadScene
- Master
- SendOSC
- Master
- SimpleOscReceiver
- Master
- ToggleObjectWithAButton
- Master
- TriggerRaycastEffect
- GameObject/GLOCK19/Muzzle
- TripleTapOSCSender
- Master

### 注記
- 一部オブジェクト名がエクスポート時に文字化けしている可能性があります（日本語名を含むUIパネル配下など）。


