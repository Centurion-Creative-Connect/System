# TriggerState

`TriggerState` is used to determine how firing should behave when trigger is pulled.

## Types

| Name   | Description                                                       |
|--------|-------------------------------------------------------------------|
| Idle   | When the trigger cannot be pulled                                 |
| Armed  | When the trigger can be pulled                                    |
| Firing | When the trigger is pulled and the gun should be firing           |
| Fired  | When the trigger is pulled but the gun should no longer be firing |