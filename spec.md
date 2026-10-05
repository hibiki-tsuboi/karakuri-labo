> **2026-10-05 現在の優先要件**：ドミノはゲームから廃止します。
> 既存の坂・シーソー・奥行きの橋・大型コースに、バネ台・カーブレール・漏斗・リフト・送風機を追加します。
> それぞれに専用ステージを設けて全9ステージとし、NEXT と STAGES から移動できます。
> 既存シーンは StageOne / StageThree / StageFour / StageFive を維持し、新シーンは StageSpring / StageCurve / StageFunnel / StageLift / StageFan とします。
> 旧 StageTwo と Main は Editor の検証用にのみ残し、iPhone アプリへ含めません。
> 以下の初期仕様にあるドミノ、旧ステージ番号、連鎖デモよりも、この要件と現在の README.md を優先します。

# からくりらぼ — Unity 3D 物理演算パズル

Unityを使って、iPhone向けの3D物理演算パズルゲーム「からくりらぼ」を作成してください。

このドキュメントをプロジェクト全体の仕様書として扱ってください。

---

# プロジェクト基本情報

## アプリ名

```text
からくりらぼ
```

## Unityプロジェクト名

```text
KarakuriLabo
```

## Unityテンプレート

```text
Universal 3D
```

Universal Render Pipeline（URP）を使用します。

High Definition Render Pipeline（HDRP）は使用しません。

理由：

- iPhone向けゲームである
- 高負荷なフォトリアル表現は不要
- Low Poly / Toy / Miniature風の見た目を想定している
- モバイルで安定したパフォーマンスを優先する

---

# 対象プラットフォーム

```text
iOS / iPhone
```

最終的にはApp Storeへの公開を想定します。

## 画面向き

```text
Landscape
```

横画面を基本としてください。

理由：

- ステージ全体を広く表示できる
- からくり装置を横方向につなげやすい
- パーツのドラッグ配置がしやすい

---

# ゲームコンセプト

ピタゴラスイッチやRube Goldberg Machineのように、複数の物理オブジェクトを配置し、連鎖反応によってボールをゴールまで運ぶゲームです。

プレイヤーはステージ上に用意されたパーツを配置します。

配置が終わったら「PLAY」ボタンを押し、Unityの物理演算を開始します。

ボールがゴールに入ればステージクリアです。

ゲームの基本的な気持ちよさは、

```text
配置する
↓
PLAYする
↓
からくりが連鎖する
↓
うまく動く
↓
ゴールする
```

という体験にあります。

---

# Unityらしさを重視する

このアプリは、SwiftUIでも作れる一般的なiPhoneアプリではなく、

```text
3D
物理演算
衝突
重力
Joint
Particle
Animation
```

など、Unityを使う意味があるゲームにしてください。

一覧画面・フォーム入力・文字入力などを中心にはしないでください。

---

# ビジュアル

リアル系ではなく、

```text
Low Poly
Toy
Miniature
```

のような、玩具・工作・ミニチュア感のある3D表現を目指します。

## デザイン方針

- 子どもでも直感的に理解できる
- 明るく楽しい
- オブジェクトの役割が見ただけで分かる
- 物理挙動が見やすい
- UIの文字量を少なくする

---

# 操作方針

キーボード入力や文字入力は極力使用しません。

基本操作は、

```text
タップ
ドラッグ
ボタン
```

だけで完結させます。

将来的に必要であれば、

```text
ピンチズーム
カメラ移動
カメラ回転
```

を追加できますが、MVPでは不要です。

---

# 基本ゲームフロー

1. ステージ開始
2. BallとGoalが表示される
3. 使用可能なパーツが画面下部に表示される
4. プレイヤーがパーツを配置
5. 必要に応じてパーツを回転
6. PLAYを押す
7. 物理演算開始
8. Ballやパーツが動く
9. 連鎖反応が発生
10. BallがGoalに入る
11. CLEAR
12. 次のステージへ

失敗した場合は、

```text
RESET
```

して配置状態へ戻します。

---

# MVP

最初は以下のみ実装してください。

```text
Ball
Goal
Ramp
Domino
Seesaw
```

ただし、最初から全部実装しないでください。

Phaseごとに段階的に追加します。

---

# Ball

物理演算されるボールです。

Unityの以下を使用してください。

```text
Rigidbody
SphereCollider
```

必要な要素：

- 重力
- 摩擦
- バウンド
- 衝突

PLAY MODEになるまで物理演算で勝手に動かないようにしてください。

---

# Goal

Ballが入るゴールです。

Trigger Colliderを使用してください。

BallがGoalのTriggerに入ったら、

```text
CLEAR!
```

と表示してください。

GameStateをClearへ変更します。

---

# Ramp

Ballを転がす坂です。

EDIT MODEではプレイヤーが、

```text
移動
回転
```

できるようにしてください。

---

# Domino

倒れるドミノです。

```text
Rigidbody
Collider
```

を使用してください。

複数配置可能な設計にしてください。

---

# Seesaw

シーソーです。

中央に支点を置き、物理演算で回転させます。

可能であれば、

```text
HingeJoint
```

を使用してください。

---

# 初期ステージ

最初のステージは非常にシンプルにします。

```text
BALL


 ↓


      Ramp


                GOAL
```

まずはBallがRampを転がってGoalに入るだけで構いません。

---

# GameState

ゲームには以下の状態を持たせてください。

```csharp
public enum GameState
{
    Edit,
    Playing,
    Clear
}
```

GameManagerが状態を管理します。

---

# EDIT MODE

パーツを配置するモードです。

この状態では物理演算を停止してください。

プレイヤーは、

```text
パーツ移動
パーツ回転
パーツ削除
```

ができます。

---

# PLAY MODE

PLAYボタンを押した後の状態です。

物理演算を開始してください。

PLAY MODE中は、プレイヤーによるパーツ編集は禁止してください。

---

# RESET

RESETを押した場合、

PLAY開始直前の状態に完全に戻してください。

復元対象：

```text
Position
Rotation
Rigidbody velocity
Rigidbody angularVelocity
```

必要に応じて、

```text
isKinematic
constraints
```

などの物理状態も正しく復元してください。

---

# パーツ移動

iPhoneのタッチ操作を前提とします。

パーツを指でドラッグして移動します。

MVPでは複雑な3D操作は不要です。

ステージ平面上をドラッグして移動できれば構いません。

---

# パーツ回転

パーツを選択した状態で、

```text
↺
```

ボタンを押して回転します。

最初は、

```text
15度
```

ずつ回転で構いません。

将来的には自由回転へ拡張可能な設計にしてください。

---

# パーツ削除

選択したパーツに対して、

```text
🗑
```

ボタンから削除できるようにします。

---

# カメラ

初期の MVP は斜め上から見下ろす固定カメラとします。

2026-10-05 の操作性調整として、画面の上下を保つ注視点まわりの回転とします。
全ステージで空いている場所を1本指でスワイプすると、横方向は床に対して水平に360度回転します。
縦方向は見上げ・見下ろし、斜めは横・縦を組み合わせ、横倒しや回転の蓄積による傾きを防ぎます。
俯角は−75〜80度で制限し、真上・真下を通過して逆さまにならないようにします。
上下の限界までスワイプした後も、指の移動を逆にすると直ちに戻り始めます。
2本指のピンチで拡大・縮小、同方向のドラッグで平行移動します。
ズーム倍率は0.45〜2.5（投影サイズ比）、移動範囲にも上限を設けます。
PC は左ドラッグで回転、右／中ドラッグで移動、ホイールで拡大・縮小します。

編集中のパーツのドラッグと視点操作は同時に行わず、UI 上から始まる操作は視点へ引き継ぎません。
2本指操作は両指を空いている場所に置いて開始し、片方を離すと残った指で跳ねずに回転を続けます。
VIEW RESET で位置・角度・倍率を初期状態へ戻します。PLAY／CLEAR 中も操作でき、配置の RESET は視点を維持します。
視点を手動で回転・移動・ズームすると自動追従を停止し、FOLLOW BALL で再開します。
区間ボタンでは角度とズーム倍率を保持して注視する区間を切り替えます。

2026-10-04 の追加要件として、ステージ5は画面外まで続く3区間の縦長コースとします。
上段・中段・下段・全体のボタンをタップして表示範囲を切り替えます。
各区間の高さに対応する坂を1本ずつ配置し、合計3つの欠けた区間をつないでゴールを目指します。
PLAY 中はボールの高さに合わせて区間を自動追従します。区間ボタンで手動表示に切り替わり、
FOLLOW BALL で再開します。パーツのドラッグ・視点スワイプ中は区間移動を受け付けません。

---

# UI

画面下部に使用可能パーツを表示します。

例：

```text
┌──────────────────────────┐

 Ramp   Domino   Seesaw

└──────────────────────────┘
```

画面右下：

```text
▶ PLAY
```

PLAY MODE中：

```text
↩ RESET
```

---

# Safe Area

iPhoneの、

```text
Dynamic Island
Home Indicator
Safe Area
```

を考慮してください。

UIがSafe Area外にはみ出さないようにします。

---

# CLEAR演出

BallがGoalに入ったら、画面中央に大きく、

```text
CLEAR!
```

を表示してください。

簡単な演出も追加します。

例：

```text
Particle System
軽いアニメーション
効果音
```

ただし演出はMVP完成後でも構いません。

---

# サウンド

現行版では、木琴と柔らかい鍵盤を中心としたオリジナルBGM「Clockwork Afternoon」と
クリア音を再生します。BGMは80秒のループで、配置中から流れ、PLAY・RESET・NEXTでも
再生位置を維持します。SOUNDボタンで両方を消音・解除でき、バックグラウンド中は休止します。
音源の制作方法は `Assets/Audio/README.md` を参照してください。

将来的な追加候補：

```text
Ball転がり音
衝突音
Goal音
```

AudioManagerなどを作成し、後から追加しやすい構造にしてください。

MVPでは音源がなくても構いません。

---

# プロジェクト構成

可能な限り責務を分離してください。

例：

```text
Assets/

  Scenes/
    Main.unity

  Scripts/

    Game/
      GameManager.cs
      GameState.cs

    Physics/
      PhysicsObject.cs
      BallController.cs
      GoalController.cs

    Placement/
      PlacementManager.cs
      DraggableObject.cs
      RotatableObject.cs

    UI/
      UIManager.cs

    Stage/
      StageManager.cs

  Prefabs/
    Ball/
    Goal/
    Ramp/
    Domino/
    Seesaw/

  Materials/

  Audio/

  Particles/
```

必要であれば、より適切な構成へ改善して構いません。

---

# PhysicsObject

物理オブジェクトには可能な限り共通処理を持たせてください。

例えば、

```text
PhysicsObject
```

という基底クラスまたは共通Componentを作ります。

PLAY直前の、

```text
Position
Rotation
Velocity
AngularVelocity
```

などを保存し、

RESET時に復元できるようにしてください。

---

# Prefab化

再利用するゲームパーツはPrefabとして管理してください。

対象例：

```text
Ball
Goal
Ramp
Domino
Seesaw
```

将来的に新しいパーツを追加しやすい構成にします。

---

# 将来的な追加パーツ

バネ台（Spring）、カーブレール、漏斗、リフト（Elevator）、送風機（Fan）は実装済みです。
全て既存の追加・ドラッグ・回転・削除・RESET を利用し、機構は PLAY 中のみ動作します。
カーブと漏斗は衝突形状に沿ってボールを運びます。バネの強さとリフトの高さは固定、
送風機は向きと配置で調整し、壁の向こうへ風を通しません。

MVP完成後、以下を追加できるようにします。

```text
Trampoline
Magnet
Conveyor Belt
Pendulum
Rope
Cannon
Bomb
Switch
Door
Moving Platform
```

新しいパーツを追加するたびにGameManagerなどを大幅修正する設計は避けてください。

---

# Puzzle Mode

決められたパーツのみを使ってゴールするモードです。

例：

```text
Ramp ×1
Domino ×5
Spring ×1
```

限られたパーツをどう配置するか考えるゲームです。

---

# Sandbox Mode

自由にパーツを配置して、自分だけの「からくり装置」を作れるモードです。

将来的には、

```text
保存
読み込み
共有
```

なども検討できます。

MVPでは実装不要です。

---

# 将来的なアイデア

必要になった場合、以下も検討できます。

```text
ステージ選択
スター評価
最小パーツ数チャレンジ
タイムアタック
ユーザー作成ステージ
毎日チャレンジ
AI生成ステージ
```

ただし現時点では実装しないでください。

---

# 開発フェーズ

最初から全機能を実装しないでください。

必ず段階的に進めます。

## Phase 1

以下だけ実装してください。

```text
Ball
Ramp
Goal
```

目標：

```text
BallがRampを転がり、
Goalに入り、
CLEAR!と表示される
```

---

## Phase 2

EDIT MODEを追加。

```text
Rampをドラッグ移動
Rampを回転
```

---

## Phase 3

```text
PLAY
RESET
```

を追加。

PLAY前の状態を保存し、RESETで正確に戻せるようにします。

---

## Phase 4

```text
Domino
```

を追加。

---

## Phase 5

```text
Seesaw
```

を追加。

---

## Phase 6

UIを改善。

```text
パーツ選択
削除
Safe Area
タッチ操作
```

などを整えます。

---

## Phase 7

演出を追加。

```text
CLEAR演出
Particle
Audio
簡単なAnimation
```

---

# 開発環境

開発はMacで行います。

使用する主なツール：

```text
Unity Hub
Unity Editor
Xcode
Git
Codex CLI
Unity MCP
```

Unity HubはEditorのインストール・バージョン管理・プロジェクト起動に使用します。

実際のゲーム制作はUnity Editorで行います。

---

# Codex + Unity MCP

Codex CLIからUnity MCPを利用してUnity Editorを操作します。

概念：

```text
Codex CLI
    ↓
Unity MCP
    ↓
Unity Editor
```

Unity EditorでKarakuriLaboプロジェクトを開いた状態で作業してください。

Codexから可能な場合はUnity MCPを使って、

```text
Scene確認
GameObject作成
Component追加
Transform変更
Prefab操作
Unity Editor状態確認
```

などを行ってください。

C#コード編集だけで済む場合は通常のファイル編集でも構いません。

---

# Codexへの作業ルール

いきなり大量のコードやGameObjectを生成しないでください。

まず必ず現在のUnityプロジェクトを確認してください。

基本フロー：

```text
1. docs/SPEC.mdを読む

2. Unity MCPで現在のUnity EditorとSceneを確認

3. 現在のプロジェクト構造を確認

4. 必要な変更だけを実装

5. Unityのコンパイルエラーを確認

6. Scene上の状態を確認

7. 動作確認

8. 問題がある場合は修正

9. 正常に動いたら次のPhaseへ
```

Phaseを勝手に先へ進めないでください。

---

# コンパイルエラー

変更後はUnity Editorのコンパイル状態を確認してください。

エラーがある状態で次のPhaseへ進まないでください。

特に、

```text
NullReferenceException
MissingReferenceException
Compile Error
Missing Script
```

などを残さないようにしてください。

---

# Scene編集

Sceneを変更する場合、可能ならUnity MCPを利用してください。

Scene YAMLを直接大量編集する方法は、必要性がない限り避けてください。

---

# Git

Gitで管理することを前提とします。

Unity向けの.gitignoreを使用してください。

以下のようなUnity生成物はGit管理しません。

```text
Library/
Temp/
Logs/
Obj/
Build/
Builds/
```

以下は原則Git管理します。

```text
Assets/
Packages/
ProjectSettings/
```

---

# 仕様書

このファイル自体を、

```text
docs/SPEC.md
```

としてUnityプロジェクト内に保存することを推奨します。

Codexは開発開始時にこのファイルを読んでください。

---

# 最初にやること

Unity EditorでKarakuriLaboプロジェクトが起動していることを確認してください。

その後、

```text
docs/SPEC.md
```

を読み、

Unity MCPを使って現在のSceneとプロジェクト状態を確認してください。

その上で、

```text
Phase 1
```

だけを実装してください。

最初の目標は、

```text
BallがRampを転がる

↓

Goalへ入る

↓

CLEAR!
```

です。

Phase 1がUnity Editor上で正常に動作するまでは、

```text
Domino
Seesaw
パーツ配置UI
Sandbox
その他の高度な機能
```

を実装しないでください。
