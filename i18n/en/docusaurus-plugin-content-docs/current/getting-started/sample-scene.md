---
sidebar_position: 1
---

# Taking a Look at Sample Scene

Centurion System's core system is located under `Logics/System/` in the hierarchy.

## Configuring RoleManager

Before Building the scene, First you will need to configure [RoleManager](https://docs.derpynewbie.dev/newbie-commons/rolemanager).

1. Select `Logics/System/RoleManager` GameObject in the hierarchy.
2. Select `RoleManager` component's `Players` list element. 
   - For default there's `ExamplePlayer` and `DerpyNewbie`.
3. Change either element's `DisplayName` to your VRChat account display name.

This will assign you as Moderator of System. Allowing you to

- Execute moderator-only methods
- Execute moderator-only console commands
- Show Staff tag above your head

## Overview

Centurion System can be roughly divided into 3 types of modules:

- Player
- Gun
- Game Utilities

Let's look one-by-one to learn how it works.

## Player

Player-related gimmicks, This is where assigning of game players, teams, and player hits are handled.

- [PlayerManager](/docs/components/player/playermanager)
- [PlayerCollider](/docs/compnents/player/playercollider)
- [PlayerModel](/docs/components/player/massplayer/playermodel)

Look around hierarchy at `Logics/System/PlayerManager` and learn how it's structured!

## Gun

Gun-related gimmicks, This is where gun summoning, shooting, and more gun related gimmicks are handled.

- [GunManager](/docs/components/gun/gunmanager)
- [GunVariantDataStore](/docs/components/gun/datastore/gunvariantdatastore)
- [GunModel](/docs/components/gun/massgun/gunmodel)

Look around hierarchy at `Logics/System/GunManager` and learn how it's structured!

## Game Utilities

Miscellaneous gimmicks, This is where LocalPlayer movement, notifications, and moderators are handled.

- [PlayerController](/docs/components/misc/playercontroller)
- [HeadUINotification](/docs/components/misc/head-ui/headuinotification)
- [RoleManager](/docs/components/newbiecommon/rolemanager)