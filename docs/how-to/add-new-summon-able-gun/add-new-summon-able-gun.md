# 召喚できる銃を追加する

To Add New Summon-able Gun, You'll have to create new [GunVariantDataStore](/docs/components/gun/datastore/gunvariantdatastore).

1. In the hierarchy, Head over to `Logics/System/SamplelMassGun/VariantData/` .
2. Create new empty GameObject, Rename it as you like, and
   add [GunVariantDataStore](/docs/components/gun/datastore/gunvariantdatastore)
   component.
3. Change `Unique Id` to unassigned new ID.
4. Change `Weapon Name` to represent this new gun's name

Now you technically have an empty gun variant available to use!

## Configuring `GunVariantDataStore`

To make actually usable gun, You'll need to
configure [GunVariantDataStore](/docs/components/gun/datastore/gunvariantdatastore).

To configure [GunVariantDataStore](/docs/components/gun/datastore/gunvariantdatastore), follow these steps:
1. Set gun source `Model` and `Model Offset` property. 
   1. I'd recommend putting model GameObject under `GunVariantDataStore` like this: // TODO: put up an image
   2. You could attach `Animator` component to Model object to receive parameters from the Gun. See more at [Setup Animator for a Gun](/docs/how-to/setup-animator-for-a-gun).
2. Set gun `Shooter Offset` for
3. Choose one of  [Gun Behaviour](/docs/components/gun/behaviour) component for this
`GunVariantDataStore`
   1. Add it to the GameObject that has `GunVariantDataStore` attached.
   2. Configure it as you like, See each Gun Behavior's information at each behaviour's documentation page.

