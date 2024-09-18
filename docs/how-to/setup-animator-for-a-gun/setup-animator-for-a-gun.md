# 銃の Animator を設定する

銃の Model として指定している GameObject に Animator Component を付けることによって、銃の情報を Animator から取得することができます

## Animator Parameters

| Parameter Name   | Type    | Synced    | Description                                                          | Version |
|------------------|---------|-----------|----------------------------------------------------------------------|---------|
| IsPickedUp       | bool    | No        | Is the gun picked up by local player?                                |         |
| IsPickedUpGlobal | bool    | Yes       | Is the gun picked up by anyone?                                      | 0.6.0~  |
| IsInSafeZone     | bool    | Simulated | Is the gun inside of the SafeZone?                                   | 0.6.0~  |
| IsInWall         | bool    | Simulated | Is the gun obstructed by the collider?                               | 0.6.0~  |
| IsLocal          | bool    | No        | Is the gun controlled by local player?                               | 0.6.0~  |
| IsVR             | bool    | Yes       | Is the gun controlled by VR player?                                  | 0.6.0~  |
| HasBullet        | bool    | Yes       | Has the bullet in chamber?                                           |         |
| HasCocked        | bool    | Yes       | Has the gun cocked?                                                  |         |
| IsShooting       | trigger | Yes       | Turns on when shooting                                               |         |
| IsShootingEmpty  | trigger | Yes       | Turns on when failing to shoot                                       |         |
| SelectorType     | int     | No        | FireMode enum                                                        |         |
| State            | int     | Yes       | GunState enum                                                        |         |
| Trigger          | int     | No        | TriggerState enum                                                    | 0.6.0~  |
| TriggerProgress  | float   | No        | Raw analog trigger input that can be used to animate gun trigger     |         |
| CockingProgress  | float   | Simulated | Progress of the pulling process of cocking (for some GunBehaviours)  |         |
| CockingTwist     | float   | Simulated | Progress of the twisting process of cocking (for some GunBehaviours) |         |
