# からくりラボ / KarakuriLabo

Unity **6000.6.4f1**・URP の iPhone 向け物理パズルです。
坂・シーソー・バネ台などの道具を配置し、ボールをゴールへ運びます。
要件は [spec.md](spec.md)、開発規約は [AGENTS.md](AGENTS.md) を参照してください。

## 現在の9ステージ

ドミノを使う問題と追加操作は廃止しました。クリア後の **つぎへ** で次へ進みます。
既存アセットの参照を保つため、シーンのファイル名は従来の名前のままです。

| 表示番号 | 問題 | シーン | 使用できるパーツ |
| --- | --- | --- | --- |
| 1 | はじめの さか：ボールの下に坂を置く | `Assets/Scenes/StageOne.unity` | さか × 1 |
| 2 | ゆらゆら シーソー：ボールでシーソーを傾ける | `Assets/Scenes/StageThree.unity` | シーソー × 1 |
| 3 | おくから てまえへ：奥の島から手前へ橋を架ける | `Assets/Scenes/StageFour.unity` | たかい さか × 1・ひくい さか × 1 |
| 4 | ながい くだりみち：3つの高さをつなぐ大型コース | `Assets/Scenes/StageFive.unity` | うえの さか・なかの さか・したの さか 各1 |
| 5 | ぴょんと ジャンプ：バネで奥の高い足場へ跳ぶ | `Assets/Scenes/StageSpring.unity` | ばね × 1 |
| 6 | くるっと カーブ：カーブで奥のレーンへ曲がる | `Assets/Scenes/StageCurve.unity` | カーブ × 1 |
| 7 | すとんと おとそう：漏斗で受けて下のゴールへ落とす | `Assets/Scenes/StageFunnel.unity` | じょうご × 1 |
| 8 | ぐんぐん のぼろう：リフトで上のゴールへ運ぶ | `Assets/Scenes/StageLift.unity` | リフト × 1 |
| 9 | かぜで ころがそう：風でボールを上り坂へ押す | `Assets/Scenes/StageFan.unity` | せんぷうき × 1 |

ステージ1が起動シーンです。最終ステージをクリアすると **ぜんぶ クリア！ よく できたね！** を表示します。
配置や進行の保存は未実装で、再起動するとステージ1から始まります。
左上の **ステージ** から全9ステージを直接選べます。EDIT／CLEAR／失敗後に利用でき、
選び直すとそのステージの初期配置から始まります。BGM と消音状態は引き継ぎます。

## 子ども向けの日本語表示

ボタン・ステージ名・道具・操作案内・失敗理由は、ひらがなとカタカナ中心です。
「さか／あと 1こ」のように残り数を表示し、失敗後は「おしい！」と次の工夫を案内します。
日本語フォント Zen Maru Gothic をアプリに同梱するため、端末のフォントに依存しません。
共通の文言は `Assets/Scripts/UI/GameText.cs`、シーンの表示設定は `Assets/Editor/JapaneseSceneSetup.cs` にあります。

## 遊び方

1. **さか** などでパーツを追加し、ドラッグして配置します。
2. **まわす** で向きを変え、**スタート** でボールを転がします。
3. **やりなおす** で PLAY 直前の配置へ戻し、調整して再挑戦します。
4. **しまう** で選択したパーツを削除すると、その種類の在庫が戻ります。

高さと坂の傾斜は固定です。ドラッグでは左右・奥行きを調整します。
シーソーは板をドラッグすると支点と一緒に動きます。ステージ2ではボールが実際に板に触れ、
その後6度以上傾くことが必要で、ボールだけがゴールへ入ってもクリアになりません。
ステージ3・4は、必要な坂が欠けたり位置・向きが違ったりするとゴールへ届きません。

PLAY 中・CLEAR 後・失敗後は配置を変更できません。RESET はボール・部品・達成判定・紙吹雪を戻します。
音の ON／OFF は RESET と NEXT の後も保持します。ボタンは横画面の Safe Area 内に配置しています。
CLEAR 後は配置用ボタンを隠し、NEXT と RESET を表示します。

## 失敗と再挑戦

ボールがコース外へ落ちるか、3.5秒間ほぼ進まなくなると **おしい！** と原因を表示し、
失敗した位置で物理演算を止めます。リフトによる移動も進行として扱い、動き続ける場合は45秒で区切ります。
ゴールに入っていても必要な道具を使っていない場合は、その理由を表示します。

- **なおす**：PLAY 直前の配置へ戻して編集。部品と在庫は保持します。
- **もういちど**：同じ配置ですぐに再実行します。
- 失敗後もカメラを動かして原因を確認でき、視点と BGM は維持します。

## 新しい道具

5種類とも、追加・ドラッグ・15度回転・削除は既存のパーツと共通です。各ステージ1個まで使えます。

- **ばね**：上から踏むと、矢印の方向へ跳ね上げます。強さは固定で、位置と向きを調整します。
- **カーブ**：側壁のある90度の下りレールです。入口から出口まで、実際の衝突と重力で転がります。
- **じょうご**：広い口で受け、狭い筒から下へ落とします。吸引や瞬間移動は使いません。
- **リフト**：ボールが載ると3.2メートル上昇し、出口のゲートが開きます。RESET で台とゲートを戻します。
- **せんぷうき**：羽根とリボンの方向へ風を送ります。範囲外・背面・壁の向こうには力が届きません。

新ステージでは、道具を実際に使ってゴールへ運ぶことがクリア条件です。
レールと漏斗は入口・出口の順に通る必要があります。道具の動作と力は PLAY 中だけ有効です。

## BGM

木琴と柔らかい鍵盤を中心にしたオリジナル曲 **Clockwork Afternoon** が流れます。
80秒で自然にループし、PLAY・RESET・NEXT で曲の先頭へ戻りません。
**おと：あり／なし** で BGM とクリア音をまとめて切り替えます。
アプリをバックグラウンドへ移すと休止し、戻ると続きから再生します。
制作方法・音量・取り込み設定は [Assets/Audio/README.md](Assets/Audio/README.md) を参照してください。

## 視点と大型コース

空いている場所を1本指でスワイプすると、左右は床に対して水平に360度回転し、上下は見上げ・見下ろしになります。
斜めスワイプは両方を組み合わせます。画面は横倒しにならず、上下は真上・真下の手前で止まります。
2本指を広げる／狭めると拡大・縮小、同じ方向へ動かすと平行移動します。
PC は左ドラッグで回転、右／中ドラッグで移動、ホイールで拡大・縮小します。

編集中にパーツから始めた操作は配置専用、ボタンから始めた操作は UI 専用です。
2本指操作は両方の指を空いている場所に置いて始めます。**みかたを もどす** で位置・角度・倍率を初期状態へ戻します。
通常の **やりなおす** は視点を維持します。横から見てパーツを動かしにくい場合は、少し上からの角度に戻してください。

ステージ4は奥行き約17メートル、高低差約6メートルの3区間のコースです。

- **うえ／なか／した**：その区間へ表示を移動。
- **ぜんたい**：全体を見渡す。パーツを追加すると対応する高さの区間へ移動。
- **スタート**：ボールの高さに合わせて表示区間を自動追従。
- 実行中の区間選択・回転・移動・ズームは自動追従を止める。**ボールを みる** で再開。
- 区間切り替えでは回転角とズーム倍率を維持。

パーツのドラッグ・視点回転中は、別の指による区間移動や追加・回転・削除を受け付けません。

## 見た目とシーン編集

丸みのある木製の箱、塗装した坂、真鍮の軸、艶のあるコーラル色のボールで、
からくり玩具の工房をイメージしています。
素材・生成方法・フォントのライセンスは [Assets/Art/README.md](Assets/Art/README.md) にあります。

シーンを保存して Play を停止し、必要に応じて次の Editor メニューを実行します。

- **Karakuri Labo > Apply Japanese UI**：日本語フォント・案内・ボタンを全シーンへ再適用。
- **Karakuri Labo > Set Up Campaign**：9ステージの番号・NEXT・ビルド順・ステージ選択を設定。
- **Karakuri Labo > Set Up New Toys and Stages**：5種類の Prefab と未作成の新ステージを生成。既存シーンの配置は保持。
- **Karakuri Labo > Apply Atelier Art Direction**：共有素材、メッシュ、照明、UI を再適用。
- **Karakuri Labo > Set Up Swipe Camera**：視点回転を再設定。その後に Atelier を再適用。

過去のステージ生成スクリプトの番号はファイル名に対応します。再生成した場合は最後に
**Set Up Campaign** を実行してください。既存のシーンや Prefab の `.meta` は保持してください。
`StageTwo.unity` と `Main.unity` は旧仕様の Editor 用検証データで、アプリのビルド対象外です。
旧デモ・実機確認の履歴は [docs/DEVELOPMENT_HISTORY.md](docs/DEVELOPMENT_HISTORY.md) に保存しています。

## 主なファイル

- `Assets/Scripts/Game/`：開始・クリア・失敗・リセットと状態管理。
- `Assets/Scripts/Physics/`：ボール・ゴールと物理状態の復元。
- `Assets/Scripts/Placement/`：タッチ・選択・移動・回転・パーツ在庫。
- `Assets/Scripts/Stage/`：ステージ遷移、シーソー条件、視点回転、区間移動。
- `Assets/Scripts/UI/`・`Audio/`：操作パネル、クリア演出、音。
- `Assets/Editor/CampaignSceneSetup.cs`：現在のステージ構成と iOS プロファイル。
- `Assets/Editor/ToyWorkshopSetup.cs`・`ToyGeometry.cs`：新しい道具、曲面メッシュとステージの生成。
- `Assets/Prefabs/Placement/`：パーツの形状と固定の高さ。
- `Assets/Art/`：メッシュ、木目、素材、照明、Zen Maru Gothic 日本語フォント。

## 実行と検証

Unity で対象のシーンを開き、**Play** で確認します。
PlayMode テストは **Window > General > Test Runner** から実行できます。
`CampaignTests` は実際の9ステージ構成、ドミノの不在、全ステージの自然クリアと NEXT を検証します。
`ToyStageTests` は5種類の物理動作、失敗例、風の遮蔽、リセットとライブ再生を検証します。
`ToyInteractionTests` はタッチでの追加・移動・回転・削除とステージ選択を検証します。
配置、RESET、在庫、Safe Area、水平360度回転、上下の限界・反転防止、斜め操作、ピンチ・移動、指の持ち替え、区間移動の回帰テストもあります。
`AttemptFailureTests` は自動失敗、再挑戦と復元、停止・進行の判定、クリア優先、リフト停止、9ステージの失敗UIと入力競合を検証します。
`JapaneseUITests` は全9ステージの状態別表示、同梱フォントの字形、1000×540の表示枠での文字収まりを検証します。
`BackgroundMusicTests` は再生、ループ、消音、NEXT での継続、中断・復帰と後片付けを検証します。
2026-10-05 に日本語表示を含む115件すべて成功しました。見出しの高さ調整後も全9ステージの表示を再検証し、最新結果を `Logs/japanese-ui-playmode-tests.json` に保存しています。
元音源の音割れ・無音・ループ端点の確認結果は `Logs/bgm-waveform-audit.json` にあります。
Editor 専用の旧シーンは `TestSceneLoader` で読み込み、製品のビルドリストへ追加しません。

バッチ実行時は、このプロジェクトの Unity Editor を閉じてから実行します。

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_EDITOR" -batchmode -projectPath "$PWD" -runTests \
  -testPlatform PlayMode -assemblyNames KarakuriLabo.PlayModeTests \
  -testResults /tmp/karakuri-playmode.xml
```

iOS Build Profile で `Builds/iOS/` へ書き出し、生成した Xcode プロジェクトから実機へインストールします。
ビルド順は `StageOne → StageThree → StageFour → StageFive → StageSpring → StageCurve → StageFunnel → StageLift → StageFan` です。

2026-10-05 に新しい道具5種類・全9ステージ版の Unity 書き出しと署名付き Xcode ビルドが成功しました。
iPhone 18 Pro へのインストール・起動と、プロセスが継続して動くことも確認済みです。
実機で全9ステージを操作してクリアする確認は未実施です。左上の STAGES から新ステージへ移動できます。
ログは `Logs/new-toys-ios-build.log`、書き出し結果は `Logs/new-toys-unity-build-summary.json`、
実機の記録は `Logs/new-toys-device-install.json` です。
ビルドレポートで9シーン・新しい Prefab 5種類・BGM の収録を確認しています。
曲面レールと漏斗の衝突メッシュは事前生成し、生成設定変更後の道具テスト9件も再度成功しています。
旧ステージ・旧デモ・ドミノ Prefab の除外確認は `Logs/no-domino-packaged-assets.json` にあります。

2026-10-05 の失敗・再挑戦版を iPhone 18 Pro に反映済みです。実機でステージ1の自動失敗、RETRY、失敗後のカメラ操作、ADJUST、配置修正後の CLEAR を確認しました。6画面の確認記録は `Logs/failure-flow-device-verification.json`、実機テストは `Builds/DeviceQA/FailureFlow.xcresult` にあります。

2026-10-05 の日本語版を iPhone 18 Pro に反映し、実機テスト2件が成功しました。14枚の画面で日本語のホーム・ステージ選択・道具の案内、縦長コースの移動、ステージ1の失敗・再挑戦・カメラ操作・配置修正後のクリアを確認しました。実機で全9ステージをクリアする確認は未実施です。確認記録は `Logs/japanese-ui-device-verification.json`、実機テストは `Builds/DeviceQA/JapaneseUI.xcresult` にあります。
