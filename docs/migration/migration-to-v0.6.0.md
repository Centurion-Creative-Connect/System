# Migration Info: 0.6.0

:::info
必要な Migration 作業量が多くなってしまっているため、わからないことがあれば気軽にご相談ください。
:::

## TL;DR

- Prefab を unpack せずに利用しており、その他 System に関連するスクリプトが存在しない場合はそのまま更新することが可能です。

- Prefab を unpack
  して利用している場合、[追加が必要な Component](#追加が必要な-Component)、[今までの仕様と同等にするために必要な Component](#今までの仕様と同等にするために必要な-Component)
  を追加する必要があります。

- System に関連するスクリプトがある場合、それに対応した Assembly Definition と U# Assembly Definition が必要です。

## 削除が必要なファイル

基本的には VCC が削除してくれますが、更新後に下記のフォルダが残っている場合、削除してください。

- `Assets\Experimental\PlayerCounter`
- `Assets\ResetStatusDisplay`

## 対応が必要になるファイル

下記の Assembly を参照している CSharp-Assembly に属するスクリプトは、新規で Assembly Definition とペアになる U# Assembly
Definition を作成し、Assembly Definition 内の Assembly Definition Reference を設定する必要があります。

- `VRC.Udon`
- `UdonSharp.Runtime`
- `CenturionCC.System`
- `CenturionCC.System.Commands`

:::note
`Assets\` 下にある `.cs` ファイルで、上層に `Assembly Definition` アセットが無い物が Assembly-CSharp 内に該当します。
更新後にコンパイルエラーが出るファイルと同層に Assembly Definition & U# Assembly Definition を設定してください。
:::

## 追加が必要な Component

新しく追加され、追加が必要な Component の説明です。これらが無い場合、独自でヒット状態の Sync を実装しなければ正常に動作しません。

:::note
`MassPlayerManager` Prefab を利用している場合は既に追加されているため、新しく追加する必要はありません。
:::

### DamageDataSyncer

`0.6.0` 以前は `PlayerBase` 内でヒットデータを Sync していましたが、これからは `DamageDataSyncer` がデータを Sync
するようになります。

:::tip
`DamageDataSyncer` は必ずしも `PlayerBase` の数と同じである必要はありませんが、
二分の一の数である場合、二人のプレイヤーが一つの `DamageDataSyncer` を共有することになります。
このとき、できるだけデータのロスを無くす処理をしていますが、Udon の仕様上、データロスが発生する可能性があります。
:::

### DamageDataSyncerManager

上記の `DamageDataSyncer` を纏め、データの受け渡しをします。

### DamageDataResolver

`0.6.0` 以前は `PlayerBase` 内でヒットデータを受け取った場合にヒットという判定にしていましたが、これからは
`DamageDataResolver` がヒット判定の有無を決定します。
`DamageDataResolver` は、上記の `DamageDataSyncerManager` からヒットデータを受け取り、判定が有効であった場合に
`LastHitData` へデータを受け渡します。

### AutoReviver

`0.6.0` 以前はヒット後の死んだ状態は時間ベースでの計算でしたが、`PlayerBase#IsDead` の追加によって死んだ状態を表現することができるようになりました。
また、蘇生処理を外部に出すことによって、個別の処理追加を容易にできるようになっています。これによって、`0.6.0`
以前における自動蘇生の挙動を再現するためには、`AutoReviver` をScene上に追加する必要があります。

## 今までの仕様と同等にするために必要な Component

機能が分割され、今までと同じようにするために追加が必要な Component の説明です。追加をしなくても動作はしますが、0.6.0
以前の挙動をしなくなります。

:::note
`MassGunManager` Prefab を利用している場合は既に追加されているため、新しく追加する必要はありませんが、エッジケースにおいて、若干の挙動の変更があります。
:::

### ShootingRule

#### 概要

`GunManager` の機能として、新しく `ShootingRule` を設けました。これによって、撃てる状態を簡単にカスタマイズすることができるようになります。
この変更により、

- NoShootingInAirRule
- NoShootingWhileDeadRule
- NoShootingWhileNotInGameRule

が追加で実装され、これら Component を追加することによって、プレイヤーの撃てる状態に対して制限を設けることができるようなりました。

#### `0.6.0` 以前の挙動を再現する方法

下記の Component を Scene 内に設置してください。

- NoShootingInAirRule
- NoShootingWhileDeadRule

:::warning
`MassGunManager` Prefab に使用されている `DefaultShootingRuleSet` には、`NoShootingWhileNotInGameRule` が含まれているため、
`0.6.0` 以前の挙動とは異なります。
:::

## Unity 2022 について

Unity 2022 の新規プロジェクトにて動作することを確認していますが、Unity 2019 -> 2022 の段階で起こる可能性のある問題に関しては未テストです。
`0.6.0` から VRCSDK `3.5.0` と共存できるようになっているため、更新することが可能です。