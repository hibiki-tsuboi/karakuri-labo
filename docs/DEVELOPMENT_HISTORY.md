# ドミノ廃止前の開発記録

2026-10-04 の構成変更前の記録です。現在の遊び方とビルド構成はルートの README.md を参照してください。
以下の旧ステージ番号・ドミノ・連鎖デモは現行アプリの構成を表しません。

# からくりらぼ / KarakuriLabo

Unity 6000.6.4f1・URP で制作する、iPhone 向けの物理演算パズルです。
要件は [spec.md](../spec.md)、開発規約は [AGENTS.md](../AGENTS.md) を参照してください。

## 見た目：Atelier

角を丸めた木製の箱、塗装された坂、真鍮の軸を持つシーソー、艶のあるコーラル色のボールで、
からくり玩具の工房をイメージしています。全5ステージとデモで素材・照明・操作画面を統一しました。

`Assets/Art/` に専用メッシュ・木目・反射・フォントを収録しています。
再適用は、シーンを保存して Play を停止し、**Karakuri Labo > Apply Atelier Art Direction** を実行してください。
形状の装飾には当たり判定を追加せず、既存の Collider・Rigidbody・Joint の設定が変わらないことも検査します。
素材の生成方法とフォントのライセンスは [Assets/Art/README.md](../Assets/Art/README.md) を参照してください。

## ステージ1を遊ぶ

1. Unity 6000.6.4f1 で `Assets/Scenes/StageOne.unity` を開き、Editor の **Play** を押します。
2. ボールとゴールだけの状態で始まります。**+ RAMP (1)** で坂を1個追加します。
3. 坂をボールの下へドラッグし、ボールがゴールへ転がる配置を考えます。
4. **PLAY** で試し、届かなければ **RESET** して配置を直します。
5. ゴールすると **CLEAR!** と紙吹雪・チャイムが出ます。

坂は1個だけ使えます。追加すると残数が0になり、**DELETE** すると1に戻ります。
RESET は PLAY 直前の配置を復元し、坂の使用数も維持します。
何も置かずに PLAY してもクリアできません。クリア後の **NEXT** でステージ2へ進みます。
配置や進行の保存は未実装で、アプリを再起動するとステージ1から始まります。

`StageOne` が起動シーンです。従来の連鎖デモは `Main.unity` に残しています。
ステージ1を新規生成する場合は、Phase 7 まで設定済みの Main から
**Karakuri Labo > Set Up Stage 1 Puzzle** を実行します。既存の StageOne は上書きしません。

## ステージ2を遊ぶ

ドミノの連鎖でスイッチを押し、**ゲートを開いてボールをゴールへ運ぶ**問題です。
**+ DOMINO (3)** で追加し、ボールの通路の横にある黄色いガイドに沿って並べます。
最後のドミノがスイッチへ届く配置を考えて PLAY してください。

ボールが先頭を押すとドミノが連鎖し、ボールは専用の通路へ進みます。
最後のドミノがスイッチを押すとゲートが上がり、待っていたボールが坂道を転がってゴールします。
倒れたドミノをボールの足場にする必要はありません。
左上に倒れた数と **GATE CLOSED / OPEN** を表示します。
3個を倒すだけでは開かず、スイッチへの実際の接触も必要です。
ボールが直接スイッチに触れたり、無関係な力でドミノが倒れたりしても開きません。

DELETE で在庫が戻り、RESET で配置・ゲート・スイッチ・達成判定を戻せます。
クリア後の **NEXT** でステージ3へ進み、音の ON／OFF も引き継ぎます。
Editor では `Assets/Scenes/StageTwo.unity` を開いてください。
新規生成メニュー **Karakuri Labo > Set Up Stage 2 Puzzle** は既存の StageTwo を上書きしません。
**Refine Stage 2 Domino Gate** はステージ2の固定通路・スイッチ・ゲートを再生成します。

## ステージ3を遊ぶ

固定の坂から転がるボールで、**シーソーを傾けてゴールへ入れる**問題です。
**+ SEESAW (1)** で1台追加し、木製の板をドラッグして坂の出口付近へ置きます。
ボールが板に触れ、その後6度以上傾くと **SEESAW 1 / 1** になります。
ボールだけのゴールや、ボールが触れずに傾いたシーソーではクリアできません。

RESET は板・支点・ボールを PLAY 直前へ戻し、達成判定をやり直します。
DELETE はシーソー全体を削除し、在庫を1に戻します。
クリア後の **NEXT** でステージ4へ進みます。
Editor では `Assets/Scenes/StageThree.unity` を開いてください。
新規生成メニュー **Karakuri Labo > Set Up Stage 3 Puzzle** はステージ2へ NEXT を追加し、
ステージ3を作成します。既存の StageThree は上書きしません。

## ステージ4を遊ぶ

**高さの違う2本の坂で、奥の島から手前のゴールまで橋を架ける**立体パズルです。
**+ HIGH (1)** の青緑の坂と **+ LOW (1)** の黄土色の坂を、それぞれ1本ずつ使います。
ドラッグで左右・奥行きを調整し、**ROTATE +15°** で進行方向を変えてつないでください。
坂の高さと傾斜は固定です。高い坂でボールを受け、低い坂へ渡し、手前の受け皿を目指します。
途中に谷があるため、坂が足りない場合や奥行き・角度が合わない場合は届きません。

RESET は2本の位置・角度とボールを試行前へ戻します。DELETE は選んだ高さの在庫を返します。
クリアすると **NEXT** でステージ5へ進みます。
Editor では `Assets/Scenes/StageFour.unity` を開いてください。
新規生成メニュー **Karakuri Labo > Set Up Stage 4 Depth Puzzle** はステージ3へ NEXT を追加します。
既存の StageFour と専用 Prefab は上書きしません。

## ステージ5を遊ぶ

**3つの高さをつなぐ、画面より大きな縦長のコース**です。スタートからゴールまで奥行き方向に
約17メートル、高さは約6メートルあります。固定の坂の間を **+ UPPER / + MIDDLE / + LOWER** の
3本でつなぎます。それぞれ1本だけ使え、高さは固定です。位置と向きを考えて配置してください。

- **01 UPPER / 02 MIDDLE / 03 LOWER**：タップしてその区間へ移動。左右スワイプの角度は維持します。
- **ALL / MAP**：コース全体を見渡します。パーツ追加時は対応する区間へ自動的に移動します。
- **PLAY**：上段からボールを追い、通過した高さに合わせて表示区間が切り替わります。
- 実行中に区間ボタンを押すと手動表示になり、**FOLLOW BALL** で自動追従へ戻せます。
- **VIEW RESET**：上段の初期視点へ戻ります。通常の **RESET** は表示位置・角度を維持し、配置を戻します。

3本のうち1本でも欠けたり、つなぐ向きが違ったりするとゴールへ届きません。
最下段のゴールで **ALL STAGES CLEAR!** を表示します。現在の最終ステージです。
Editor では `Assets/Scenes/StageFive.unity` を開いてください。
新規生成は **Karakuri Labo > Set Up Stage 5 Long Descent**。既存の StageFive は上書きしません。

## 視点を回す

全ステージで、**パーツやボタンのない場所を左右にスワイプ**すると、ステージの周りを360度見渡せます。
PC では同じ場所をマウスの左ボタンでドラッグします。スワイプでは高さとズームは変わりません。
ステージ5は区間ボタンで表示範囲を移動し、ALL / MAP で全体へ縮小できます。
パーツから始めたドラッグはパーツの移動になり、途中で視点回転へ切り替わりません。
カメラとパーツは同時に操作できず、操作中は別の指による追加・回転・削除・視点リセットを防ぎます。

右上の **VIEW RESET** で、そのステージの最初の角度に戻ります。
PLAY 中・CLEAR 後も視点を回せます。通常の **RESET** は配置だけを戻し、見ている角度は維持します。
ステージを進むと新しいステージの初期視点になります。
既存のシーンへ組み込む場合は **Karakuri Labo > Set Up Swipe Camera** を実行してください。

## 連鎖デモ（Phase 7）を試す

1. Unity Hub からこのフォルダーを Unity **6000.6.4f1** で開きます。
2. `Assets/Scenes/Main.unity` を開き、Editor の **Play** を押します。
3. **EDIT MODE** で起動し、ボール・シーソー・4個のドミノは停止したままになります。
4. 青緑の Ramp、木製の Seesaw、アイボリーの Domino をタップ／クリックすると、枠で選択が表示されます。
5. 指／マウスでドラッグして、ステージ平面（XZ）上を移動します。高さと指との距離は保持します。
6. **ROTATE +15°** を押すと、傾斜を保って上方向の軸を中心に15度ずつ回転します。
7. **PLAY** を押すと物理演算が始まります。初期配置ではボールがシーソーを傾け、ドミノを連鎖させてゴールへ進みます。
8. **RESET** を押すと、PLAY 直前の配置・回転・物理状態へ戻り、再び編集できます。
9. 下部の **+ RAMP / + DOMINO / + SEESAW** で、台の手前にパーツを追加できます。
   追加直後は選択状態になるので、そのままドラッグ・回転して配置してください。
10. **DELETE** で選択中のパーツを削除します。シーソーは支点と板をまとめて削除します。
11. ゴールすると **CLEAR!** が弾むように表示され、ゴールから紙吹雪が出て短いチャイムが鳴ります。
12. 右上の **SOUND ON / SOUND OFF** で効果音を切り替えられます。

何もない場所をタップすると選択解除します。回転・削除ボタンは選択時のみ有効です。
ドミノは1個ずつ移動・回転できます。Playing / Clear 中は追加・削除を含めて配置操作できません。
ドラッグ中も追加・回転・削除を無効化し、別の指による誤操作を防ぎます。
ボタンは Safe Area 内に配置し、操作バーの隙間をタップしても背後のパーツは動きません。
シーソーは板をタップして選択します。移動・回転時は板と支点が一緒に動きます。
板は HingeJoint の支点を中心に回転し、RESET で板の姿勢・速度も戻ります。
支柱も Ramp と一緒に動きます。移動範囲は各部品の中心位置を基準に制限しており、
部品全体の収まりや他の部品との重なりは判定しません。

RESET はクリア後だけでなく、ボールが転がっている途中やステージから落ちた後も使えます。
保存する配置は PLAY を押すたびに更新します。起動時の配置へ戻したい場合は、
Editor の再生を停止して再開するか、iPhone でアプリを終了して再起動してください。
追加したパーツも RESET の対象です。PLAY 前に削除したパーツは RESET では戻りません。
パーツを続けて追加すると同じ位置に出現するため、移動してから次を追加してください。
RESET は演出中も使えます。紙吹雪と再生中の音を止め、CLEAR の文字サイズも元に戻します。
音の ON／OFF は RESET 後も保持し、アプリを再起動すると ON に戻ります。
紙吹雪は見た目だけの演出で、ボールやドミノの物理演算には干渉しません。

## 主なファイル

- `Assets/Scripts/Game/`: ゲーム状態と進行管理。
- `Assets/Scripts/Physics/`: Ball の物理開始、Goal 判定、`PhysicsObject` による動的部品の開始・状態復元。
- `Assets/Scripts/Placement/`: 選択、ドラッグ、15度回転、タッチ・マウス入力。
- `Assets/Scripts/Placement/PartSpawner.cs`: Prefab の生成、選択、復元登録と種類別の個数制限・削除時の返却。
- `Assets/Scripts/UI/`: CLEAR 表示、配置操作 UI、Safe Area への配置。
- `Assets/Scripts/UI/ClearCelebration.cs`: CLEAR 時の文字アニメーションと紙吹雪の開始・停止。
- `Assets/Scripts/Audio/AudioManager.cs`: クリア音とミュートの制御。
- `Assets/Scripts/Stage/`: ステージ遷移、連鎖・シーソーの達成判定、`StageCameraOrbit` による視点回転。
- `Assets/Scripts/Stage/StageViewNavigator.cs`: 大きなステージの区間移動・全体表示・ボール追従。
- `Assets/Editor/StageFiveSceneSetup.cs`: 3区間の縦長コースと3本の坂、ステージ4からの NEXT。
- `Assets/Scripts/UI/CameraHUD.cs`: VIEW RESET と操作中の無効化。
- `Assets/Editor/CameraOrbitSceneSetup.cs`: 各シーンの回転中心と視点操作 UI の設定。
- `Assets/Editor/AtelierArtSetup.cs`・`AtelierGeometry.cs`: 全シーン・Prefab の見た目と描画品質を設定。
- `Assets/Art/`: 木目、丸みのあるメッシュ、素材、照明、Barlow フォント。
- `Assets/Audio/ClearChime.wav`: 合成波形から生成したオリジナルの短いチャイム。
- `Assets/Prefabs/`: Ball・Ramp・Goal・Domino・Seesaw の Prefab。
- `Assets/Editor/PhaseOneSceneBuilder.cs`: ベースシーンの作成。既存の Main は上書きしません。
- `Assets/Editor/PhaseTwoSceneSetup.cs`: 既存の Main に配置操作を組み込む Editor 用セットアップ。
- `Assets/Editor/PhaseThreeSceneSetup.cs`: PLAY / RESET、復元対象の登録、ボタン配置のセットアップ。
- `Assets/Editor/PhaseFourSceneSetup.cs`: ドミノの Prefab と連鎖を試せる初期配置のセットアップ。
- `Assets/Editor/PhaseFiveSceneSetup.cs`: 支点付きシーソーの Prefab と、ボールが板を傾ける初期配置のセットアップ。
- `Assets/Editor/PhaseSixSceneSetup.cs`: 追加用 Prefab と、追加・回転・削除・PLAY / RESET 操作バーのセットアップ。
- `Assets/Editor/PhaseSevenSceneSetup.cs`: 紙吹雪、チャイム、CLEAR アニメーション、音の切替ボタンのセットアップ。
- `Assets/Editor/StageOneSceneSetup.cs`: Main を維持したまま、坂1個のパズルと起動シーン順を設定。
- `Assets/Editor/StageTwoSceneSetup.cs`: 固定の坂とドミノ3個のパズル、NEXT とビルド順を設定。
- `Assets/Editor/StageTwoGateSetup.cs`: ステージ2の専用通路、スイッチ、昇降ゲートを設定。
- `Assets/Scripts/Stage/DominoGate.cs`・`DominoSwitch.cs`: 実接触による開門、表示と RESET を制御。
- `Assets/Editor/StageThreeSceneSetup.cs`: 固定の短い坂とシーソー1台のパズル、NEXT とビルド順を設定。
- `Assets/Editor/StageFourSceneSetup.cs`: 高低2本の坂・谷・奥から手前へ渡る配置範囲、NEXT とビルド順を設定。

連鎖デモには保存済みの Main を使用してください。デモを最初から作り直した場合のみ、
**Karakuri Labo > Set Up Phase 2 Editing**、**Set Up Phase 3 Play and Reset**、
**Set Up Phase 4 Dominoes**、**Set Up Phase 5 Seesaw**、**Set Up Phase 6 Part Controls**、
**Set Up Phase 7 Clear Effects** の順で適用します。
Phase 4／5 は追加済みの部品配置を上書きしません。

`Domino.prefab` は Rigidbody・BoxCollider・PhysicsObject・DraggableObject を持ちます。
ステージ制作者が個数を増やす場合は Prefab を複製し、GameManager の `Reset Objects` に
各 PhysicsObject を登録してください。`Simulate On Play` が有効な部品だけが PLAY で動き出します。
ゲーム内で追加したパーツは自動登録され、削除時は登録も解除されます。

`Seesaw.prefab` は固定支点のルート Rigidbody と、子 `Board` の動的 Rigidbody を持ちます。
HingeJoint はルートへ接続し、Z 軸の回転を ±18° に制限します。
複製時は GameManager の `Reset Objects` にルート、Board の順で両方を登録してください。
板の補間は編集時に無効、PLAY 中に有効になり、タッチ編集後の姿勢のずれを防ぎます。
`Assets/Prefabs/Placement/` は操作バーから追加する専用 Prefab です。
Phase 6 セットアップ時に Main の高さ・傾斜・支柱を含めて作成し、以降は既存アセットを維持します。

## iPhone での確認

ビルド対象は StageOne、StageTwo、StageThree、StageFour、StageFive、Main の順です。既存の iOS Build Profile を使って Xcode プロジェクトを
再出力し、前回と同じ Team・Bundle Identifier で実機へ再インストールしてください。
ステージ5版は 2026-10-04 に Unity の書き出しと署名付き Xcode ビルドが成功しました。
この時点では iPhone 18 Pro が未接続（unavailable）のため、実機へのインストール・操作確認は未実施です。
テストは84件成功し、Editor の Mobile 描画で全体マップ・区間表示・自然クリア・ゴールでの静止を確認しました。
ビルドログは `Logs/stage5-ios-build.log`、書き出し結果は `Logs/stage5-unity-build-summary.json`、
画面例は `Temp/stage-five-overview.png` です。Unity では `StageFive.unity` を開いてすぐ試せます。

ステージ5追加前の Atelier・視点回転版は、2026-10-04 に iOS ビルドと同じ iPhone への
インストールが完了しました。Editor の78件は成功し、Mobile の描画設定で画面も確認しています。
この版の実機タッチ操作・描画速度の確認は、端末のロック解除待ちです。
ビルドログは `Logs/atelier-ios-build.log`、Unity の結果は `Logs/atelier-unity-build-summary.json`、
画面例は `Temp/atelier-stage-three-ui.png` にあります。実機操作の準備済みスクリプトは
`Builds/DeviceQA/PhaseTwoDeviceUITests.swift`（CameraOrbitDeviceUITests）です。
以下は視点回転と Atelier を追加する前の実機記録です。

2026-10-04 にステージ4入りの iOS ビルドと iPhone 18 Pro（iOS 27.0.1）へのインストールが完了しました。
実際のタッチ操作と16枚の画面から、ステージ1〜3のクリア・NEXT、ステージ4の高低2本の坂の
配置・回転、2回の自然クリア、未配置・坂1本での失敗、RESET と高さ別の在庫返却を確認しました。
ステージ2のドミノ連鎖・スイッチ・ゲートを通る自然クリアも、今回の実機で確認済みです。
追加上限と在庫再利用の2組は画像データも一致しました。RESET は目視確認です
（一部の撮影に OS の通知が重なったため、全画面の完全一致判定には使用していません）。
結果は `Builds/DeviceQA/StageFour.xcresult`、画像は `Temp/device-stage4/`、
ログは `Logs/stage4-ios-build.log` と `Logs/stage4-device-qa.log` です。

2026-10-03 に iPhone 18 Pro（iOS 27.0.1）で、ステージ1から NEXT を使ってステージ3まで進み、
シーソーの自然クリアと紙吹雪・全ステージ完了表示を、実際のタッチ操作と18枚の画像で確認しました。
未配置・誤配置では未クリアになり、音の設定も移動先へ引き継ぎます。
未配置と誤配置の RESET、追加上限、クリア後の板・支点の復元、削除返却と再利用の6組は
画像データも一致しています。NEXT はステージ1・2で同じ位置に揃えています。
結果は `Builds/DeviceQA/StageThree.xcresult`、画像は `Temp/device-stage3/`、
ログは `Logs/stage3-ios-build.log` と `Logs/stage3-device-qa.log` です。

2026-10-03 に同じ iPhone 18 Pro で、ステージ1の CLEAR → NEXT → ステージ2の連鎖クリアまで
実際のタッチ操作と16枚の画像で確認しました。消音設定の引き継ぎ、ボールだけのゴールでは
未クリアになること、ドミノ3個の配置・上限・削除返却、当時の最終ステージの完了表示も確認済みです。
初期配置への RESET、上限での追加操作、3個の配置復元、在庫の再利用の4組は画像データも一致しています。
結果は `Builds/DeviceQA/StageTwo.xcresult`、画像は `Temp/device-stage2/`、
ログは `Logs/stage2-ios-build.log` と `Logs/stage2-device-qa.log` です。

2026-10-03 に iPhone 18 Pro（iOS 27.0.1）でステージ1を確認済みです。
実際のタッチ操作と14枚の画像から、未配置・誤配置ではクリアしないこと、
坂1個の追加上限、削除時の返却、ドラッグ配置からの自然クリアと紙吹雪を確認しました。
RESET 前後の配置、追加上限での連打、削除で戻した空配置の6組は画像データも一致しています。
結果は `Builds/DeviceQA/StageOne.xcresult`、画像は `Temp/device-stage1/`、
ログは `Logs/stage1-ios-build.log` と `Logs/stage1-device-qa.log` です。

Phase 1 の自動転がり・CLEAR 表示は実機確認済みです。
実機確認では、起動時の停止、ドラッグ、回転、PLAY→CLEAR→RESET、
配置を変えた後の PLAY→RESET を確認してください。
2026-10-03 に iPhone 18 Pro（iOS 27.0.1）へ Phase 2 をインストールし、
XCTest による実機タッチ操作と7枚の画面撮影で、静止・選択・ドラッグ・回転・選択解除を確認済みです。
実機テスト結果は `Builds/DeviceQA/PhaseTwo.xcresult`、確認画像は `Temp/device-phase2/` にあります。
同日に Phase 3 も実機確認済みです。XCTest のタッチ操作と12枚の画面撮影で、
PLAY→CLEAR→RESET、転がっている途中の RESET、移動・回転後の配置復元を確認しました。
起動直後と RESET 後、編集した配置の PLAY 前と RESET 後は画像データも一致しています。
Phase 3 の実機結果は `Builds/DeviceQA/PhaseThree.xcresult`、画像は `Temp/device-phase3/`、
ビルドと実行ログは `Logs/phase3-ios-build.log`、`Logs/phase3-device-qa.log` です。
同じ iPhone 18 Pro で Phase 4 も確認済みです。実際のタッチ操作と13枚の画像で、
4個の連鎖→CLEAR、ドミノ1個の移動・回転、途中RESET、編集後の配置復元を確認しました。
起動直後と RESET 後、編集した配置の PLAY 前と RESET 後は画像データも一致しています。
結果は `Builds/DeviceQA/PhaseFour.xcresult`、画像は `Temp/device-phase4/`、
ログは `Logs/phase4-ios-build.log` と `Logs/phase4-device-qa.log` です。
同じ実機で Phase 5 も確認済みです。XCTest のタッチ操作と13枚の画像で、
シーソーの傾斜→4個のドミノの連鎖→CLEAR、シーソー全体の移動・回転、
途中 RESET と編集後の配置復元を確認しました。起動直後と各 RESET 後、
編集した配置の PLAY 前と RESET 後の画像データも一致しています。
結果は `Builds/DeviceQA/PhaseFive.xcresult`、画像は `Temp/device-phase5/`、
ログは `Logs/phase5-ios-build.log` と `Logs/phase5-device-qa.log` です。
Phase 6 も同じ実機で確認済みです。XCTest のタッチ操作と16枚の画像で、3種類の追加・削除、
追加したシーソーの移動・回転・RESET、削除した部品が RESET で復活しないこと、
初期配置での連鎖→CLEAR を確認しました。追加後の配置復元と各削除後の画面も画像データで一致しています。
操作バーは横画面の Safe Area 内に収まっています。
結果は `Builds/DeviceQA/PhaseSix.xcresult`、画像は `Temp/device-phase6/`、
ログは `Logs/phase6-ios-build.log` と `Logs/phase6-device-qa.log` です。
Phase 7 は同じ実機で、XCTest のタッチ操作と14枚の画像から、CLEAR の拡縮と紙吹雪、
3回のクリア、SOUND ON／OFF の切替、演出途中の RESET を確認しました。
音の切替状態を含め、RESET 後の画面は PLAY 前の画像データと一致しています。
音声は PCM データと Editor での再生状態を検証しています（実機のスピーカー音は未聴取）。
結果は `Builds/DeviceQA/PhaseSeven.xcresult`、画像は `Temp/device-phase7/`、
ログは `Logs/phase7-ios-build.log` と `Logs/phase7-device-qa.log` です。

## テスト

**Window > General > Test Runner > PlayMode** から `KarakuriLabo.PlayModeTests` を実行します。
物理開始・ゴール到達に加え、Edit 中の静止、ドラッグの高さと距離、移動範囲、
回転、別指による割り込み防止、非 Edit 中の操作禁止を検証します。
Main シーンには Touchscreen 入力イベントによるドラッグ→回転ボタンの統合テストもあります。
Phase 3 では Rigidbody の速度・角速度・各種フラグの復元、毎回の PLAY 直前の配置保存、
CLEAR の解除、再編集、連続クリアとタッチによる PLAY / RESET を検証します。
Phase 4 ではボールからドミノへの実衝突、ドミノ同士の連鎖、個別のタッチ配置、
複数の動的部品の停止・開始・復元を追加しています。
Phase 5 では実際の衝突による板の回転、Joint の角度制限と支点維持、
組み立て全体のタッチ配置、移動・回転後の連続 PLAY／RESET を検証します。
Phase 6 ではタッチによる追加・削除、新規部品の連続 PLAY／RESET、削除後の復活防止、
Playing / Clear とドラッグ中の操作制限、Safe Area と狭い画面でのボタン配置を検証します。
Phase 7 では実際のゴール到達による文字アニメーション・単発の紙吹雪、演出中の RESET、
連続クリアと重複発火防止、タッチによる音切替と消音状態の維持、音声データを検証します。
入力テスト中は専用の設定コピーを使用し、Game View のフォーカスに依存せず実行します。
終了時に元の入力設定へ戻し、製品の設定アセットは変更しません。
ステージ1では未配置・誤配置でクリアしないこと、タッチ配置後の自然クリア、
少しずれた配置の許容、坂1個の在庫・即時返却、RESET とコンパクト画面の配置を検証します。
ステージ2では自然な3個の連鎖とスイッチへの接触・開門・ゴール、ボールが後続ドミノに乗らないこと、
未配置・配置不足・無関係な転倒・先に倒れた部品の拒否を検証します。
ボールによるスイッチ操作の拒否、開門途中の RESET と再クリア、在庫、NEXT と消音設定の継続も検証します。
ステージ3では実接触後の板の傾斜と自然クリア、2通りのタッチ配置、無接触の傾斜の拒否、
板と支点の RESET、削除時の即時返却、ステージ2からの NEXT 遷移を検証します。
ステージ4では2本の坂への実接触と奥行き移動、タッチでの配置・回転、2通りの自然クリア、
誤った奥行き・角度・不足した坂の失敗、RESET と高さごとの在庫返却、ステージ3からの NEXT を検証します。
NEXT 表示中も幅1000の Safe Area に操作ボタンが収まることを確認します。
視点操作では左右回転・一周・微小な指ぶれ・初期視点への復帰、回転後のパーツ配置、
別指との競合、UI 上からのドラッグ、タッチの中断とアプリのフォーカス喪失、マウス操作、
PLAY／CLEAR 中の回転と RESET 後の視点維持を検証します。
2026-10-04 時点で、ステージ5と区間移動を含む PlayMode テスト84件が成功しています。
直近の実行結果は `Logs/stage5-playmode-tests.json` に保存しています。
NEXT の位置をステージ間で揃えた後も、関連6件を再実行して成功しています
（`Logs/stage3-layout-playmode-tests.json`）。

Editor を閉じた状態では、プロジェクトルートから以下でも実行できます。

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
"$UNITY_EDITOR" -batchmode -projectPath "$PWD" -runTests \
  -testPlatform PlayMode -assemblyNames KarakuriLabo.PlayModeTests \
  -testResults /tmp/karakuri-playmode.xml
```
