# Migration Info: 1.1.0

:::info
わからないことがあれば、お気軽に [Discord サーバー (#tech-support)](https://centurioncc.org/discord) にてご相談ください!
:::

- VRChat SDK (Worlds) `3.10.6` 以上が必要になりました。
    - プレイヤータグに `VRCBillboard` を利用するようになったためです。
    - 更新前に VCC / ALCOM 上で VRChat SDK - Worlds を更新してください。
- Unity の `Post Processing` (`com.unity.postprocessing`) パッケージに依存するようになりました。
    - Centurion System の更新時に自動でインストールされます。
- `PlayerController` から銃関連の機能 (Gun Sprint / Combat Tag) が `PlayerControllerGunExtension` に分離されました。
    - 詳しくは [PlayerController の移行方法](#playercontroller-の移行方法) をご参照ください。
- プレイヤータグ (`CenturionHeadFollower`) はプレイヤーの方向を向く処理を行わなくなりました。
    - 同梱の `CenturionPlayerObject.prefab` をそのまま利用している場合は対応不要です。
    - 独自のプレイヤータグを利用している場合は、タグの GameObject に `VRCBillboard` コンポーネントを追加してください。
- 独自スクリプトで Centurion System の API を利用している場合は、[スクリプトの変更点](#スクリプトの変更点) をご確認ください。

## PlayerController の移行方法

:::danger

Gun Sprint / Combat Tag の値を `PlayerController` 上で変更していた場合、その値は引き継がれません。
更新前に値を控えておいてください。

:::

1. シーン上の `PlayerController` の Inspector を開き、以下の項目の値を控えます。
    - Gun Sprint 関連 (Use Gun Sprint / Gun Sprint Walk Speed / Gun Sprint Run Speed / Gun Direction Threshold)
    - Combat Tag 関連 (Use Combat Tag / Combat Tag Speed Multiplier / Combat Tag Time)
2. Centurion System を `1.1.0` に更新します。
3. `PlayerController` の子にある `PlayerControllerGunExtension` を選択します。
    - `Samples/Prefabs/Systems/PlayerController.prefab` を利用している場合は、既に配置されています。
    - Prefab を利用せず独自に `PlayerController` を配置している場合は、
      `PlayerControllerGunExtension` と `PlayerControllerGunExtensionHelper` を追加してください。
4. Step 1. で控えた値を `PlayerControllerGunExtension` に設定します。

## スクリプトの変更点

独自スクリプトから以下の API を利用している場合のみ対応が必要です。

| 1.0.x                                                        | 1.1.0~                                                           |
|--------------------------------------------------------------|------------------------------------------------------------------|
| `PlayerController` の名前空間 `CenturionCC.System.Utils`     | `CenturionCC.System.Utils.PlayerLocomotion`                      |
| `PlayerController.SubscribeCallback` / `UnsubscribeCallback` | `PlayerController.Subscribe` / `Unsubscribe`                     |
| `PlayerController` の Gun Sprint / Combat Tag 関連プロパティ | `PlayerControllerGunExtension` に移動                            |
| `GunManagerBase.UseBulletTrail`                              | `GunManagerBase.BulletTrailMode` (`UseBulletTrail` は Obsolete)  |
| `ProjectileBase.SetDamageSetting` / `ResetDamageSetting`     | `ProjectileBase.SetProjectileDataProvider`                       |
| `GunReloadHelper._DoSimplifiedReload(bool force)`            | `_DoSimplifiedReload(int bulletsToLoad = 0, bool force = false)` |
| `AreaPlayerCounter` のコールバックは `UdonSharpBehaviour`    | `AreaPlayerCounterCallback` を継承                               |

## 主要な System の変更点

### 部位ごとにダメージ倍率を設定できるようになりました

#### 1.0.x

どこに当たっても同じダメージでした。

#### 1.1.0~

`GunBulletDataStore` に頭・胴体・腕・脚ごとのダメージ倍率を設定できるようになりました。

ヘッドショットで大ダメージ、手足は軽傷、といったバランス調整が可能になり、VRFPS がより作りやすくなりました。

------

### HP やヒットを表示するギミックが追加されました

#### 1.0.x

HP の状態をプレイヤーに伝える手段は自作する必要がありました。

#### 1.1.0~

以下のギミックが追加されました。シーンに配置するだけで利用できます。

- `HealthBarDisplay` / `HealthBarTag` : 自分や他プレイヤーの HP をバーで表示
- `HitMarkerSpawner` : ダメージを与えた際にヒットマーカーを表示
- `PostProcessHitEffect` (`DamagePostProcessEffect`) : 被弾時・ヒット時に画面にエフェクトをかける
    - 一定時間で元に戻る (Decay) 設定も可能です

また、ヒット表示やクリティカルヒット表示の色を変更できるようになりました。

:::note

`PostProcessHitEffect` を利用するには、VRC Scene Descriptor の Reference Camera に `Post-process Layer` が設定されている必要があります。

:::

------

### 1 発ずつのリロードが可能になりました

#### 1.0.x

リロードは常にマガジンを満タンにするものでした。

#### 1.1.0~

`InteractReloadBehaviour` の `Reload Amount` で、1 回のインタラクトで装填する弾数を指定できるようになりました。

`0` の場合は今まで通り満タンまで装填されます。ショットガンやボルトアクションのような 1 発ずつの装填を再現できます。

------

### 銃に新しい Gun Behaviour が追加されました

#### 1.0.x

アンダーバレルやパーツの切り替えなどは自作する必要がありました。

#### 1.1.0~

以下の Gun Behaviour が追加されました。

- `AlternativeShootBehaviour` : グレネードランチャーなどのアンダーバレル武器を実装
- `ObjectToggleBehaviour` : アクションに応じてオブジェクトの表示を切り替え (ライト、レーザーなど)
- `SendCustomEventBehaviour` : アクションに応じて任意の UdonBehaviour にイベントを送信
- `SmoothingOnActionBehaviour` : Use ボタンを押している間だけ銃の位置にスムージングをかける

サンプルとして `SampleAlternativeShootRifle.prefab` と `SampleObjectToggleAssaultRifle.prefab` が同梱されています。

------

### アバターの身長に応じて移動速度を変えられるようになりました

#### 1.0.x

移動速度はアバターの大きさに関係なく一定でした。

#### 1.1.0~

`PlayerController` の `Use Avatar Eye Height For Movement Multiplier` を有効にすると、
`アバターの目の高さ / Base Avatar Eye Height` が全ての移動速度に倍率としてかかるようになります。

小さいアバターが有利になりすぎる、といった問題を抑えられます。

------

### 弾道の軌跡 (Trail) を細かく設定できるようになりました

#### 1.0.x

軌跡は全員分表示するか、全く表示しないかの 2 択でした。

#### 1.1.0~

`BulletTrailMode` により、以下の 4 つから選択できるようになりました。

- `All` : 全ての弾に軌跡を表示 (従来の ON と同じ)
- `Local` : 自分の弾のみ軌跡を表示
- `Other` : 他プレイヤーの弾のみ軌跡を表示
- `None` : 軌跡を表示しない (従来の OFF と同じ)

------

### その他の変更点

- `RigidbodyWindZone` (WindZone) が追加され、エリア内の Rigidbody (弾など) を風で流せるようになりました。
- 銃のホルスター処理が改善され、`GunHolsterSettings` でホルスターの有効化・ハイライト・表示・編集をまとめて切り替えられるようになりました。
- `GunPositioningHelper` にスムージングのオプションが追加されました。
- Gun Sprint に `Gun Sprint Direction Margin` が追加され、銃口の向きが閾値付近でも速度が急に切り替わらなくなりました。
- `SteelChallenge` のフットフォールト判定エリアを設定できるようになりました。
- `PlayerAreaBase` に `IsInside` API が追加され、`AreaPlayerCounter` も `PlayerAreaBase` を継承するようになりました。
- Control Panel の Info タブから、ログ出力や実験的機能 (Gun Physics) のスクリプトシンボルを切り替えられるようになりました。
    - Gun Physics を有効にすると、`GunVariantDataStore` の `Use Gravity` で銃に重力を適用できます。
- エリアから退出した際にプレイヤー数がずれる不具合や、VR でデスクトップ用入力が反応する不具合など、多数の不具合が修正されました。
