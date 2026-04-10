---
sidebar_position: 1
---

# Migration Info: 0.2.0

TL;DR: `PlayerManager` の `Local Hit Effect` を設定しなおしてしてください

- `CockingBehaviour` の挙動に変更が入りました
    - デスクトップユーザーは自動でコッキングされるように
    - ボルトアクションのコッキング終了時に変な挙動をすることがあった問題を修正
- `ProjectilePool` に新しい API を追加
    - Property `bool HasInitialized`: Returns true if this pool has been initialized, false if not.
    - Method `ProjectileBase Shoot(...)`: Gets available projectile and shoots from specified location
- `PlayerTag` の挙動に変更が入りました [#9](https://github.com/Centurion-Creative-Connect/System/issues/9)
    - 基本的には同じチームに所属していないと表示されないようになりました
        - 例外として
          "チームに所属していない"
          "スタッフチームに所属している"
          場合には全てのプレイヤーから表示されるようになった！
- `ShooterPlayer` が `PlayerStats`
  を利用しなくなりました [#7](https://github.com/Centurion-Creative-Connect/System/issues/7)
    - `Stats` は `ShooterPlayer(PlayerBase)` から直接取れるようになりました
- `PlayerManager` に スタッフチーム の概念を追加しました (イベント運営用)
    - `PlayerManager` において、Team 情報を取る際に `Staff Team Id` にマッチするとチームカラーが上書きされます。下記のプロパティをご参照ください
        - `Staff Team Id`
        - `Staff Team Color`
- `PlayerManager` が `LocalHitEffect` への参照を持つようになりました
    - `LocalHitEffect` の Path は `Logics/System/LocalPlayerFollower/Player/LocalHitEffect` になります
        - これへの参照が `PlayerManager` に設定されていないと、クラッシュします
        - これにより、`GameManager` は `ShooterPlayer` へイベントを発火することがなくなります
