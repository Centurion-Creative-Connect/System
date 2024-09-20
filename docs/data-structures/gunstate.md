# GunState

`GunState` is used to determine which state the gun is in for the cocking

## Types

| Name              | Description                                             |
|-------------------|---------------------------------------------------------|
| Unknown           | When `GunState` conversion (byte to `GunState`) failed  |
| Idle              | When a gun is not in the middle of cocking              |
| InCockingPull     | When a slide, cocking handle, or a bolt is being pulled |
| InCockingPush     | When a slide, cocking handle, or a bolt is being pushed |
| InCockingTwisting | When a cocking bolt is being twisted                    |