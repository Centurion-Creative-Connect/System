# 銃剣を設定する
銃のトリガーを引くことで有効化される銃剣の設定をします

## サンプル
- CenturionSystemSampleScene をご覧ください
- Hierarchy: `Logics/System/SampleMassGun/VariantData/SampleSRBayonet`

## 前提知識
- [アニメーションイベントの仕様 - Unity マニュアル](https://docs.unity3d.com/ja/current/Manual/script-AnimationWindowEvent.html)
- [銃の Animator を設定する](../setup-animator-for-a-gun/setup-animator-for-a-gun.md)

## 手順
1. 銃の [GunVariantDataStore](/docs/components/gun/datastore/gunvariantdatastore.md) (以下"VariantData") を作成します
2. VariantData に指定されている Model の中に銃剣となる GameObject を追加します
3. 銃剣になる GameObject に対して、`LocalDamageable` コンポーネントを追加し、設定をします
   1. `LocalDamageable` は以下の設定が必要です
      - レイヤー: GameProjectile
      - コライダー: Is Trigger をオンに
4. VariantData に指定されている Model に `LocalDamageableManipulator` コンポーネントを追加します
5. `LocalDamageable` を `LocalDamageableManipulator` の `Damageable Objects` に追加します
6. 銃の Animator を設定します
   1. ここでは `AC_DefaultCocking` を Override した Animator Override Controller (以下"AOC") を利用します
   2. AOC の LOCAL_TRIGGER に Animation Clip を設定します
      - Animation Clip は 10 フレーム長の Loop なしを前提とします
   3. 設定した Animation Clip に Animation Event を設定します
      - Function: `SendCustomEvent`
      - String: 
        - 7 フレーム目: `DisableDamageableObjects` 
        - 8 フレーム目: `EnableDamageableObjects`
7. 動作確認をして設定完了です