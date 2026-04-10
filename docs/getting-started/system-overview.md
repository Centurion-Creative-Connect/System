---
sidebar_position: 3
---

# システムの構成を理解する

Centurion System は大きく 3 つのモジュールに分けることができます

## Player System

プレイヤー関連の基礎的な処理を受け持つシステムです。本格的に API を触るのでなければ、基本的には弄るところはないです。

以下の処理などを担当しています

- コライダー追従
- ダメージ付与
- チーム処理
- プレイヤーデータ同期
- プレイヤー関連のイベント発火

以下のクラスなどがこれに当たります

- PlayerManagerBase
- CenturionPlayerManager
- PlayerBase
- CenturionPlayer

`CenturionPlayerSystemSample.prefab` として Prefab にまとめてあります。

## Gun System

銃関連の処理を受け持つシステムです。銃の設定等をする際に触れるため、一番触れることになるモジュールです。

以下の処理などを担当しています

- 両手持ち位置計算
- 弾の発射
- プールされた銃の管理
- 銃ステート同期
- 銃関連のイベント発火

以下のクラスなどがこれに当たります

- GunManagerBase
- CenturionGunManager
- GunBase
- CenturionGun
- GunController

`CenturionGunSystemSample.prefab` として Prefab にまとめてあります。

## Gimmicks

Player System や Gun System が発火するイベントを用いて System を拡張し、ゲームの見た目や挙動を作り上げるモジュールです。
無限の可能性があります。

以下の処理などを担当しています

- プレイヤータグ
- ヒット表示
- 通知
- フラッグ (Objective)
- スコアボード
- 自動蘇生

以下のクラスなどがこれに当たります

- CenturionPlayerTag
- HeadUILocalHitEffect
- NotificationProvider
- FlagButton
- ScoreboardManager
- AutoReviver