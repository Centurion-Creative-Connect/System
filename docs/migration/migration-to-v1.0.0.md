# Migration Info: 1.0.0

:::info
わからないことがあれば、お気軽に [Discord サーバー (#tech-support)](https://centurioncc.org/discord) にてご相談ください!
:::

- 以下の Prefab はそのまま引き継ぐことはできません。
    - `MassGunManager.prefab`
        - 詳しくは [GunVariantDataStore の移行方法](#GunVariantDataStore-の移行方法) をご参照ください
    - `SampleMassLightweightPlayerManager.prefab`
        - `CenturionPlayerSystemSample.prefab` に置き換えてください。
    - `SampleMassPlayerManager.prefab`
        - `CenturionPlayerSystemSample.prefab` に置き換えてください。
- セーフゾーンの設定は `CenturionPlayerArea` を用いるようになりました。
    - `SafeZone` Prefix は利用できなくなりました。
    - 詳しくはサンプルシーンのセーフゾーン設定をご確認ください。
- `System.Commands` パッケージは内包されるようになったため、VCC 上で削除してください。
- 一部事前共有済みのギミックを含んでいるため、データが競合する可能性があります。
    - 以下のギミックは事前に削除した上で更新を行ってください。
        - StickFlag
        - Defuser

## GunVariantDataStore の移行方法

:::danger

この作業は 1.0.0 更新前に行ってください。
更新後は Prefab の参照が切れるため、データを引き出すことが難しくなる可能性があります。

:::

1. `SampleMassGunManager.prefab`(SampleMassGun) を用いている場合は Prefab を 2 回 Unpack してください。
    - Unpack Completely は使わないでください。
      入れ子になっている Prefab まで Unpack してしまうため、維持が困難になります。

2. VariantData と VariantData 内で参照している GameObject を全て選択します。
    - サンプルの銃が参照しているのは以下 3 つの GameObject です。
        - DefaultAudioData
        - DefaultHapticData
        - MessageData

3. GameObject を 右クリック -> `Create Empty Parent` で全てをまとめた GameObject を作成します。
4. 全てをまとめた GameObject を Prefab として保存します。
5. Centurion System を `1.0.0` に更新します。
6. 既存の SampleMassGun を削除し、`CenturionGunSystem.prefab` をシーン上に設置します。
7. Step 4. で作成した Prefab を Scene 上に配置し、Unpack します。
8. Step 7. で Unpack した Prefab の中にある VariantData の中身を、
   シーン上に配置した `CenturionGunSystemSample/VariantData` 内に移動します。
9. 上部メニューバーから `Centurion System/Control Panel` を開き、Migration タブから Perform Migration ボタンを押下します。
10. (各 GunVariantDataStore) Fire Mode Settings の Rounds Per Second / Rounds Per Minute が Infinity
    になっている場合、射撃することができないため、できるだけ大きい値へ変更してください。(9999 RPS等)
11. (各 GunVariantDataStore) ObjectMarker Settings の Tags に NoFootstep が入っている場合、所持中に足音が鳴らなくなるため、この項目を削除してください。
12. (各 GunVariantDataStore) (CockingGunBehaviour を利用している場合のみ) `Prefabs/Systems/Gun/CustomGunHandle.prefab` を
    GunVariantDataStore の子として配置し、CockingGunBehaviour の `Custom Handle` で参照します。

## 主要な System の変更点

### Player システムはインスタンス人数によって自動的にスケールするようになりました

#### 0.6.0 or older

インスタンス最大人数分の Player インスタンスを用意する必要がありました。

#### 1.0.0~

PlayerObjects を利用することにより、インスタンス人数分自動的に用意されるようになりました。

------

### Player は HP を持つようになり、複数回ダメージを耐える設定が可能になりました

#### 0.6.0 or older

プレイヤーはワンヒットアウトであり、ダメージを耐えるようなギミックは作成できませんでした。

#### 1.0.0~

Player は Health と MaxHealth、DamageData は DamageAmount の設定を持つようになり、
Health が 0 になったタイミングでヒット表示が出る仕組みになりました。

この変更により、Centurion System を用いた VRFPS がより簡単に実装できるようになりました。

------

### GunVariantDataStore に複数の Gun Behaviour を指定できるようになりました

#### 0.6.0 or older

Gun Behaviour は Variant と 1 対 1 の関係でした。

#### 1.0.0~

Gun Behaviour は Variant に複数組み付けることができます。

- CockingGunBehaviour + GunSprintBehaviour でコッキング可能かつ SubHandle を Use で走れる Variant
- DefaultGunBehaviour + InteractReloadBehaviour で電動ガン + インタラクトでリロードできる Variant

上記ののような形でカスタムできるようになったため、実装できる銃の幅が広がりました。

------

### 射撃モード毎に射撃レート/指切りレートを変更できるようになりました

#### 0.6.0 or older

射撃レートは Variant と 1 対 １ の関係で、かつ指切りの上限は射撃レートでした。

#### 1.0.0~

射撃レートを射撃モード毎に指定できるようになり、かつ指切りレートの上限も指定できるようになりました。

:::note

Inspector 上では可能であると警告が表記されていますが、射撃レートを超える指切りは今まで通り不可となっています。
`1.0.1` にて修正予定です。

:::

------

### 簡易的なリロードを設定できるようになりました

#### 0.6.0 or older

無限に射撃でき、コッキングや射撃レート以外の制限方法はありませんでした。

#### 1.0.0~

Variant 毎にマガジンの弾数とリロード時間を指定できるようになり、バランス調整の手段が増えました。

また、リロード方法は以下の 2 つから自由に選択することが可能です。

1. GunController で指定されたアクションのコンビネーション (デフォルト = 銃口を下げてジャンプボタン)
    - Inspector 上で unbind し、インタラクトリロードのみにすることも可
2. InteractReloadBehaviour を用いたインタラクトでのリロード
    - GunController のリロードと共存可

------

### ライセンスが CC-BY-NC 4.0 から MIT に変わりました

#### 0.6.0 or older

Source Available な、商用利用に関して制限があるライセンスでした。

#### 1.0.0~

Open Source な、商用利用の制限がないライセンスになりました。

胸を張って Open Source なサバゲーシステムであると宣伝することができます! 💪