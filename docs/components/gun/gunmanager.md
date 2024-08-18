# Gun Manager

## Overview

The `GunManager` is responsible for managing guns. It handles spawning, resetting, shooting rules, and various events
related to gun behaviour.

| Properties               | Description                                                                                     |
|--------------------------|-------------------------------------------------------------------------------------------------|
| `Variant Root`           | Reference to the root GameObject containing [GunVariantData](datastore/gunvariantdatastore.md). |
| `Managed Gun Root`       | Reference to the root GameObject containing [ManagedGun](managedgun.md).                        |
| `Bullet Holder`          | Reference to the ProjectilePool used for managing bullets.                                      |
| `Fallback Variant Data`  | Fallback gun variant data if a specific variant is not found.                                   |
| `Fallback Behaviour`     | Fallback behavior for guns if a specific behavior is not found.                                 |
| `Allowed Ricochet Count` | (Obsolete & Unused) Was used to specify maximum ricochet count to deal damage.                  |
| `Optimization Range`     | (Obsolete) Used to specify optimization range for [ManagedGun](managedgun.md).                  |
| `Handle Re-Pickup Delay` | The delay for handling repickup of guns.                                                        |
| `Max Hold Distance`      | The maximum distance for holding guns.                                                          |
| `Use Debug Bullet Trail` | A boolean flag indicating whether to use a debug bullet trail.                                  |
| `Use Bullet Trail`       | A boolean flag indicating whether to use a bullet trail.                                        |
| `Use Collision Check`    | A boolean flag indicating whether to use collision checking.                                    |

## Events

Various events are invoked to notify listeners about gun manager state changes, such as gun reset, gun occupancy change,
variant change, local pickup/drop, shooting, etc.

See [GunManagerCallbackBase](gunmanagercallbackbase.md) for more information.