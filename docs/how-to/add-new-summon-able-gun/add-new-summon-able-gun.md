# 銃を追加する

銃を追加するには、新しい `GunVariantDataStore` を作成する必要があります。

## 追加方法

### イメージに近いサンプル銃を複製する

追加する銃のイメージに近いサンプル銃を複製し、`Unpack Prefab` します。

### `Unique Id` と `Weapon Name` を変更する

`Unique Id` は銃の召喚時に使用する一意の Id です。重複しない `0 ~ 255` までの Id を割り振ってください。

`Weapon Name` はヒット時などに表示される武器名です。こちらには特に制限はありません。

### 追加する銃のモデルを `GunVariantDataStore` の子にする

追加したい銃のモデルを Drag & Drop で複製した `GunVariantDataStore` の子にし、銃のグリップ位置を MainHandle の位置に合わせます。

また、両手持ちできる銃であれば、SubHandle を選択し、持つ位置を調整します。

### 弾の発射位置を調整する

`Offset References` 内 `Shooter Offset` に入っている `Transform` を移動し、 弾の発射位置を調整します。

### 射撃レートの調整をする

`Fire Mode Settings` 内 `Rounds Per Second` もしくは `Rounds Per Minute` で、射撃レートを調整します。

### GunSummoner を設定する

サンプルとして置かれている `SampleGunSummoners` を元に、複製して `Gun Variation Id` を
召喚したい `GunVariantDataStore` の `Unique Id` に変更します。

### ClientSim や VRChat クライアントで召喚できることを確認する

GunSummoner のインタラクトや、NewbieConsole にて `gun summon <Unique Id>` の形で召喚できることを確認します。

------

## Advanced

ここからはちょっと高度な設定方法の説明です。

### 銃モデルにアニメーションを追加する

`Animation Settings` 内 `Animator` は、銃の状態を Animator Parameter として受け取ることができます。

特殊な挙動をする銃でない限り、基本的には `AC_DefaultCocking` を元にした `Animator Override Controller` を作成し、
対応する `Animation Clip` を作成 & Override 指定することで簡単にアニメーションを実装できます。

利用できる Animator Parameter は
[銃の Animator を設定する](../setup-animator-for-a-gun/setup-animator-for-a-gun.md#animator-parameters) をご参照ください。