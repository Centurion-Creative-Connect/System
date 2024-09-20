# FireMode

`FireMode` is used to determine how guns should behave when the trigger is pulled.

## Types

| Name             | Description                                                                                  |
|------------------|----------------------------------------------------------------------------------------------|
| Unknown          | Should not be used regularly, as it indicates the received data was invalid or uninitialized |
| Safety           | Weapon cannot be fired                                                                       |
| SemiAuto         | Weapon can be fired one shot at a time                                                       |
| FullAuto         | Weapon can fire continuously while the trigger is held down                                  |
| TwoRoundsBurst   | Weapon fires 2 rounds when the [TriggerState](./triggerstate) is in `Firing` state           |
| ThreeRoundsBurst | Weapon fires 3 rounds when the [TriggerState](./triggerstate) is in `Firing` state           |
| FourRoundsBurst  | Weapon fires 4 rounds when the [TriggerState](./triggerstate) is in `Firing` state           |
| FiveRoundsBurst  | Weapon fires 5 rounds when the [TriggerState](./triggerstate) is in `Firing` state           |

:::info
Generally, you can cancel bursts if you are in the middle of cool down between 1st to 2nd bullet. You cannot cancel
after the 2nd shot of the burst.
:::