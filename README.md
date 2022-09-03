# Centurion System

Airsoft game system made for Centurion VR Survival Game Field.

## Features

- pooling bullets, guns, and players for the VRChat environment.
- pretty realistic BB physics.
- kinda good Hit Registration.
- create custom gun behaviours to customize guns furthermore.

## How to Import

### Import using Unity Package Manager

#### Requirements

- VRChat SDK - Base `3.1.x` or later
- VRChat SDK - Worlds `3.1.x` or later
- UdonSharp `1.1.x` or later
- [NewbieCommons](https://github.com/DerpyNewbie/NewbieCommons) `0.2.x`
- [NewbieLogger](https://github.com/DerpyNewbie/NewbieLogger) `0.1.x`

#### Installation

1. Make sure [Git](https://git-scm.com/) is installed on your PC
2. Open Unity Package Manager
3. Press the upper left `+` button
4. Import packages by pressing `Add package from git URL` and pasting these URLs
    1. `https://github.com/Centurion-Creative-Connect/System.git?path=/Packages/org.centurioncc.system`
    2. `https://github.com/Centurion-Creative-Connect/System.git?path=/Packages/org.centurioncc.system.commands`
5. Setup layers and collision matrix from the menu (`Centurion-Utils/Setup Layers`)
6. Done!

## FAQ

### How do I use this?

Please see the sample scene included in this package!

You can Import the sample
package [using Unity's Package Manager](https://docs.unity3d.com/2019.4/Documentation/Manual/upm-ui.html)!

### How do I attribute this in my world?

Placing `NewbieConsole` with `GameManagerCommand` (which allows `game license` to work) should be good enough for
attribution!

If unable to do the above, place a uGUI or TMPro Text which contains the URL
to [this repo](https://github.com/Centurion-Creative-Connect/System) or [Twitter](https://twitter.com/vrsgf_centurion)!

### I found a bug! How do I report it?

Please [create an Issue](https://github.com/Centurion-Creative-Connect/System/issues/new) with detailed information
or [create an PullRequest](https://github.com/Centurion-Creative-Connect/System/pulls) for it!

### I want to contribute to this!

I've been developing this system alone for so long (about an year and a half), and there is so much more that needs to
be done!
so feel free to expand this project!
