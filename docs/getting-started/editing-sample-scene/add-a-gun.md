# 銃を追加する 🚧

銃を追加する方法は二種類あります

- GunManager を通して追加する方法 (推奨)
- スタンドアローンで動作する銃を設置する方法 (高度)

## GunManager を通して銃を追加する

GunManager を使って銃を生成する場合は、ManagedGun, または GunModel にデータを提供する GunVariantDataStore (以下
VariantData) のセットアップが必要になります。

### サンプルを複製する

サンプルシーン上では `Logics/System/GunManager/VariantData` にそのサンプルがあります。

最初はこれらの中であなたの銃に一番近い VariantData を複製し、モデルを変更して改変していくのが良いでしょう。

### モデルを変更する

VariantData のモデルを変更するために、これらのプロパティを変更する必要があります

- `Model`
    - GunModel に複製される GameObject を指定するプロパティです
- `Model Offset`
    - GunModel に複製される GameObject に対して適用される Transform のオフセット(local-space)を指定するプロパティです

これらをあなたの新しい銃のモデルへ設定した後に、そのモデルの位置を微調整します。

### 射撃位置を調整する

VariantData の `Shooter Offset` に設定されている GameObject の位置が弾の出る位置です。

この GameObject が新しい銃口の先に位置するように調整してください。

:::tip
弾の出る向きは `Z+` が正面、`Y+` が上です。
向きはホップアップが掛かる方向に影響します。
:::

### ピックアップ位置を調整する

VariantData の `Main Handle Offset`、`Sub Handle Offset` に設定されている GameObject の位置が銃のピックアップ判定が出る位置です。

- `Main Handle Offset` 
  - 銃のトリガーがある持ち手の位置
- `Sub Handle Offset` 
  - 銃のハンドガードなどの両手持ち用の持ち手の位置
  - `Is Double Handed` のチェックを外すことでピックアップ判定を無効化できます

これらの位置をあなたの銃に合わせて調整し、使いやすい位置に設定してください。

### 音を変更する

### 弾道を変更する

## スタンドアローンで動作する銃を追加する

### Gun のベースを作る

### ProjectilePool を作成する

### Gun の設定をする