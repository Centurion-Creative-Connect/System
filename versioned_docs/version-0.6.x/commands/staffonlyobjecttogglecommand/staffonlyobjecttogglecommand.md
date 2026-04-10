# StaffOnlyObjectToggleCommand

## 概要

スタッフやプレイヤーをターゲットし、GameObject の状態をトグルできます。なお、実行にはスタッフ権限 (`moderator` タグ) が必要です。

| 名前                                       | 説明                                                      |
|------------------------------------------|---------------------------------------------------------|
| Default State                            | Instance Owner がワールド Join 時に適用される、デフォルトの状態。             |
| Global Objects To Enable                 | 全てのプレイヤーに対して、指定したオブジェクトを Default State と同じなアクティブ状態にします。 |
| Global Objects To Disable                | 全てのプレイヤーに対して、指定したオブジェクトを Default State と反対なアクティブ状態にします。 |
| Moderator Only Objects To Enable         | スタッフのみに対して、指定したオブジェクトを Default State と同じなアクティブ状態にします。   |
| Moderator Only Objects To Disable        | スタッフのみに対して、指定したオブジェクトを Default State と反対なアクティブ状態にします。   |
| Moderator Only Objects To Always Enable  | スタッフのみに対して、指定したオブジェクトを常にアクティブな状態にします。                   |
| Moderator Only Objects To Always Disable | スタッフのみに対して、指定したオブジェクトを常に非アクティブな状態にします。                  |
| Player Only Objects To Enable            | プレイヤーのみに対して、指定したオブジェクトを Default State と同じなアクティブ状態にします。  |
| Player Only Objects To Disable           | プレイヤーのみに対して、指定したオブジェクトを Default State と反対なアクティブ状態にします。  |
| Player Only Objects To Always Enable     | プレイヤーのみに対して、指定したオブジェクトを常にアクティブな状態にします。                  |
| Player Only Objects To Always Disable    | プレイヤーのみに対して、指定したオブジェクトを常に非アクティブな状態にします。                 |