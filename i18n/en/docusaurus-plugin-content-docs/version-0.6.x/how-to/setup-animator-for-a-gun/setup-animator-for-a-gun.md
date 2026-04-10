# Setup Animator for a Gun

You can retrieve gun properties by attaching Animator component for GameObject, that has been assigned as Model in Guns
data.

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
