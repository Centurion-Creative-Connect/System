# Experimental Defuser Gimmick

シージのディフューザーライクな挙動を実現することができるギミックです

## Warning!

このフォルダはギミックがSystemパッケージ内に追加されたタイミングでVCCにて自動的に削除されるように設定されます。
個別の設定は別のフォルダを作成した上で、その中に追加してください。

## How to use

1. `Assets/Scripts/Experiments/Prefabs`内にある `Defuser.prefab`, `Bomb.prefab`, `DefuserResetButtonUI.prefab`
   をシーン内に設置します。
2. `DefuserResetButtonUI.prefab` の中に `Defuser Reset Button` component が付属しています。これに、先ほどシーン内に追加した
   `Defuser` の中にある `DefuserLogic` オブジェクトを投げ入れます。
3. 遊べ!

## How it works

### Q. ディフューザーのエリア設定ってどうやってる?

A. `Bomb.prefab`内の`Bomb` component と、`Defuser.prefab`内の `DefuserBombDetector` がエリアの判定を担当しています。
具体的には、`Bomb` component が `DefuserBombDetector` component が付いているオブジェクトを OnTriggerEnter/Exit
で見つけたタイミングで設置が可能となります。

### Q. ディフューザーの時間設定は?

A. 4種類の時間設定があります、全て秒単位です。

- `Defuser` component (`DefuserLogic` オブジェクト)に表示されている
    - Plant Time: 設置までの準備時間 (両手で Use、またはデスクトップの場合LClick & Fを押下する必要のある時間)
    - Defuse Time: 設置後 Defuse Complete Audio が鳴るまでの時間
- `DefuserCancelInteraction` component (`DefuserCancelInteraction` オブジェクト)に表示されている
    - Cancel Time: 設置後解除するために必要な時間 (最初の殴り始めから、最後に殴り終わるまでに必要な最小の時間)
    - Max Progress Between Time: 必要な殴りの頻度、この設定時間を超える遅さで殴ると解除が途中でキャンセルされます

### Q. 音は?

A. `Defuser` component (`DefuserLogic` オブジェクト)の

- Defuse Ready Audio: 長押し後、設置可能になったタイミングで再生される音
- Defuse Cancel Audio: 設置後、解除されたタイミングで再生される音
- Defuse Complete Audio: 設置後、解除されずに Defuse Time に達したタイミングで再生される音
  がそれに当たります。
  これらは AudioDataStore (銃の音)と同じデータ構造で設定します。

### Q. アイコンってどういう仕様?

A. 自チームのタグのみ見える状態のチームタグと同じような挙動をします。
具体的には

- 自チームのプレイヤーが落としたディフューザーはアイコンが表示されます
- 敵チームのプレイヤーが落としたディフューザーはアイコンが表示されません
- グレーチーム、スタッフチームが落としたディフューザーはアイコンが誰にも表示されません
- グレーチーム、スタッフチームはその他のチームが落としたディフューザーのアイコンが表示されます

いずれの場合も、誰かが持っているときにはアイコンが表示されません。

## Licenses

- UbuntuMono is redistributed under UBUNTU FONT LICENSE Version 1.0, at `Assets/Scripts/Experiments/DefuserFlag/Fonts/`
