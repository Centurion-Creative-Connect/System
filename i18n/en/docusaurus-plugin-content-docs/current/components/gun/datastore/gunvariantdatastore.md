# Gun Variant Data Store

Stores entire gun data for [GunManager](../gunmanager).

| Parameter                           | Description                                                                                         |
|-------------------------------------|-----------------------------------------------------------------------------------------------------|
| Unique Id                           | Unique Id to identify variant                                                                       |
| Weapon Name                         | Human-readable name of this variant                                                                 |
| Holster Size                        | Minimum required [GunHolster](../gunholster) size                                                   |
| Available Firing Modes              | Firing modes of this variant. Can be cycled through using Jump button in the controller             |
| Max Rounds Per Second               | Limit of how many bullets can be shot in 1 second. Can be set as `Infinity`                         |
| Model                               | Model of this variant as GameObject                                                                 |
| Projectile Data                     | ProjectileDataProvider ([GunBulletDataStore](gunbulletdatastore)) of this variant                   |
| Audio Data                          | [GunAudioDataStore](gunaudiodatastore) of this variant                                              |
| Haptic Data                         | [GunHapticDataStore](gunhapticdatastore) of this variant                                            |
| Camera Data                         | [GunCameraDataStore](../guncamera/guncameradatastore) of this variant for [GunCamera](../guncamera) |
| Behaviour                           | [GunBehaviourBase](../behaviour) of this variant                                                    |
| Is Double Handed                    | Is this variant two handed?                                                                         |
| Use Re Pickup Delay For Main Handle | Use unable-to-pickup delay after dropping main handle?                                              |
| Use Re Pickup Delay For Sub Handle  | Use unable-to-pickup delay after dropping sub handle?                                               |
| Use Wall Check                      | Enable in-wall check?                                                                               |
| Use Safe Zone Check                 | Enable safe-zone check?                                                                             |
| Model Offset                        | Transform of where `Model` GameObject should be                                                     |
| Shooter Offset                      | Transform of where bullets should shoot                                                             |
| Main Handle Offset                  | Transform of where [MainHandle](../gunhandle) should be                                             |
| Main Handle Pitch Offset            | VR-only. Offset for pitch when holding a gun                                                        |
| Sub Handle Offset                   | Transform of where [SubHandle](../gunhandle) should be                                              |
| Collider Setting                    | Box Collider representation of this variant                                                         |
| Desktop Tooltip                     | Tooltip for holding gun with this variant in desktop                                                |
| VR Tooltip                          | Tooltip for holding gun with this variant in VR                                                     |
| Object Type                         | [ObjectMarker](docs/components/misc/objectmarker)'s ObjectType of this variant                      |
| Object Weight                       | [ObjectMarker](docs/components/misc/objectmarker)'s Weight of this variant                          |
| Tags                                | [ObjectMarker](docs/components/misc/objectmarker)'s Tag of this variant                             |