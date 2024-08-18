---
sidebar_position: 1
---

# サンプルシーンを見る

Centurion System のシステムの根幹はヒエラルキー上の `Logics/System/` に存在しています。

## RoleManager の設定をする

シーンのビルドをする前に、まずは [RoleManager](https://docs.derpynewbie.dev/newbie-commons/rolemanager) の設定をする必要があります。

1. ヒエラルキー上から `Logics/System/RoleManager` を選択します
2. `RoleManager` コンポーネントの `Players` プロパティを選択します 
   - サンプルシーンの初期値では `ExamplePlayer` と `DerpyNewbie` が存在しています
3. どちらかの `DisplayName` をあなたの VRChat アカウントの表示名に変更してください。

これであなたは Centurion System 上でモデレーターとして設定されました。モデレーターは以下の処理ができます。

- モデレーター専用のメソッドを実行できる
- モデレーター専用のコンソールコマンドを実行できる
- スタッフタグを頭上に表示できる

## 概要

Centurion System は3つのモジュールに分けることができます:

- プレイヤー
- 銃
- ゲームに関するギミック

## プレイヤー

プレイヤーに関するギミックです。ゲームプレイヤーの設定をしたり、チーム、プレイヤーヒットが処理されているところです。

- [PlayerManager](/docs/components/player/playermanager)
- [PlayerCollider](/docs/compnents/player/playercollider)
- [PlayerModel](/docs/components/player/massplayer/playermodel)

ヒエラルキー上の `Logics/System/PlayerManager` を確認して、どのような構造になっているか確認してみてください。

## Gun

銃に関するギミックです。銃の召喚や射撃などの銃に関する処理がされているところです。

- [GunManager](/docs/components/gun/gunmanager)
- [GunVariantDataStore](/docs/components/gun/datastore/gunvariantdatastore)
- [GunModel](/docs/components/gun/massgun/gunmodel)

ヒエラルキー上の `Logics/System/GunManager` を確認して、どのような構造になっているか確認してみてください。

## ゲームに関するギミック

雑多なギミックです。プレイヤー速度や、通知、モデレーターなどの処理がされているところです。

- [PlayerController](/docs/components/misc/playercontroller)
- [HeadUINotification](/docs/components/misc/head-ui/headuinotification)
- [RoleManager](/docs/components/newbiecommon/rolemanager)