# FALL

> A fast, funny falling game made for a game jam. The theme was **FALL**.

You are a goofy character in free fall. Move left and right, catch the good fruits, dodge the rotten ones and the dangerous obstacles, and survive as long as you can. The longer you fall, the faster it gets.

**Play it:** [add your itch.io link here]

## Theme

The theme of the jam was **FALL**. In this game, the player never stops falling: the whole game is one long dive, and the background scrolls past to sell the feeling.

## How to play

Catch as many good fruits as you can without losing all 3 hearts.

| Item | Effect |
| --- | --- |
| Good fruit | **+10** points |
| Bad / rotten fruit | **-5** points |
| Golden / special fruit | **+50** points (bonus) |
| Dangerous obstacle | **-1 heart** |

- You start with **3 hearts**. At 0 hearts the game is over.
- A floating pop-up (+10, -5, +50) shows each score change.
- The game gets harder over time: fruits fall faster, appear more often, and come in trickier patterns.
- Your **high score** is saved between sessions (in your browser for the web version).

## Controls

| Action | Keyboard | Mouse / touch |
| --- | --- | --- |
| Move left | Left arrow or **A** | Hold the left half of the screen |
| Move right | Right arrow or **D** | Hold the right half of the screen |
| Pause / resume | **P** or **Esc** | Pause button at the top |

You can only move left and right. The falling is automatic.

## Characters

Pick your hero on the **Characters** screen. It is a carousel with a spinning 3D preview of each character.

1. Tung Tung Tung Sahur
2. Potato with Glasses
3. Potato with Moustache
4. Ballerina Cappuccina

## Features

- Main menu with Play, Characters, Settings and Quit (Quit is hidden in the web version)
- Character selection with arrows and a live 3D preview
- Settings: music ON/OFF and sound effects ON/OFF
- Pause menu: resume, restart, main menu
- Game over screen with final score, retry and main menu
- 3-heart health system and score with floating pop-ups
- Good, bad and golden fruits, plus dangerous obstacles
- Difficulty that increases over time
- Scrolling background for the falling effect
- Fixed 9:16 portrait shape on every screen, with black bars when the window is a different shape
- Hit feedback (screen flash and shake) and sound effects
- High score saving

## Built with

- **Unity 6** (6000.6)
- C#
- 3D character and fruit models, 2D pixel-art UI
- Web (WebGL) build, published on itch.io

## Run it from source

1. Install **Unity 6** (6000.6) with the **Web Build Support** module if you want to build for the web.
2. Clone this repository:
   ```bash
   git clone [your repository link]
   ```
3. Open the project folder in Unity Hub (**Add → Add project from disk**).
4. Open the scene `Assets/Scenes/MainMenu` and press **Play**.

The scenes are `MainMenu` (build index 0) and `Game` (build index 1). The code loads them by name.

## Build for the web

1. Open **File → Build Profiles** and select the **Web** profile.
2. Make sure both scenes are in the **Scene List**, `MainMenu` first.
3. Untick **Development Build**.
4. In **Player Settings → Publishing Settings**, choose **Gzip** with **Decompression Fallback**, or **Disabled**.
5. Click **Build** and choose an empty folder.
6. Zip **everything inside** the build folder (`index.html` must be at the root of the zip) and upload it to itch.io as an HTML game.

## Project structure

Most of the menus and the HUD are built from code, so there is very little to set up by hand.

```
Assets/Scripts
├── AspectLock.cs            keeps the game in a fixed 9:16 shape
├── CharacterSelection.cs    remembers the chosen hero
├── CharacterSpawner.cs      puts the chosen model on the player
├── DifficultySystem.cs      speed and spawn rate increase over time
├── Fruit.cs                 fruit and obstacle behavior
├── FruitModelLibrary.cs     3D models for fruits and obstacles
├── FruitSpawner.cs          random spawning from above
├── FruitVisual.cs           fruit rotation while falling
├── GameAudio.cs             music and sound ON/OFF settings
├── GameManager.cs           score, hearts, game over, high score
├── GameOverController.cs    game over screen
├── HitFeedback.cs           flash and shake when hit
├── HUDController.cs         hearts, score and coin
├── MainMenuController.cs    main menu, hero carousel, settings
├── PauseMenuController.cs   pause button and pause screen
├── PlayerAnimator.cs        character falling animation
├── PlayerController.cs      left / right movement and screen limits
├── PlayerVisual.cs          character look
├── ScorePopups.cs           floating +10 / -5 / +50
├── ScrollingBackground.cs   falling illusion
├── SoundManager.cs          button and gameplay sounds
└── UISkinner.cs             applies the 2D UI art to the generated UI
```

## Credits

- **Programming:** [name]
- **2D art and UI:** [name]
- **3D art and animation:** [name]
- **Music and sound:** [names, links and licenses of the tracks and sound effects you used]
- **Other assets:** [any free asset packs, fonts or models and their authors]

Made for **[name of the game jam]** by **[team name]**.

## License

[Choose a license, or write "All rights reserved" if you don't want others to reuse the project.]
