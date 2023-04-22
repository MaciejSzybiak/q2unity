## q2unity
This project is an attempt to bring Quake II movement physics to Unity engine.

Presentation video: [click](https://www.youtube.com/watch?v=IuklKuGx-G8)

![screenshot](docs/img/screenshot.png)

#### Important note
The project's goal has been meet and this repository is no longer actively developed. Feel free to ask
questions about this implementation if you have any.

#### Movement support
The project provides a fully functional Quake II movement in Unity __while using BSP maps__.
The entire movement code is based on original id Software's sources and works exactly the same
as the original.

#### Game file support
The project includes a partial support of Q2 BSP map files including textures, lightmaps, collision detection and a few entities.
Resources can be loaded directly from Quake II folders or .pak files provided with Quake II installation.

### Getting started
In order to play the game you will need the following:
* Unity 2021.3.23f1
* A clone of this repository
* Quake II installation (full version or demo)

When you have all the prerequisites follow these steps to load a map:
1. Import the project in Unity
2. Open scene "SampleScene"
3. Click the Play button
4. In the Game window go to Settings -> Folders
5. Provide the full path to your Quake II installation in "Full Quake II game path" field (e.g. C:/Quake2)
6. Make sure "Mod folder" is set to "baseq2"
7. Click "Apply and save"
8. Go back to main menu, hit Play and select a map to load it