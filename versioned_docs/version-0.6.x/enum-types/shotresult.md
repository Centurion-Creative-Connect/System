# ShotResult

Used internally by Gun Behaviours. Behaviours can change how it reacts after a shot has occurred depending on this.

:::info
See `Gun#TryToShoot` for more detailed information.
:::

## Types

| Name                  | Description                                                                                                                                         |
|-----------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| Succeeded             | Shooting request was succeeded and should not continue shooting                                                                                     |
| SucceededContinuously | Shooting request was succeeded and should continue trying to shoot for next possible frame                                                          |
| Paused                | Shooting request was paused until a Gun is able to shoot next possible frame                                                                        |
| Cancelled             | Shooting request was cancelled due to a systematic behavior such as safe zones. This **will not** invoke the `OnShootEmpty` event                   |
| Failed                | Shooting request was failed due to mechanical issue such as [FireMode](./firemode) being in `Safety`. This **will** invoke the `OnShootEmpty` event |
