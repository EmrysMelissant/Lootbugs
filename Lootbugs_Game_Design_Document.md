# Lootbugs - Game Design Document [cite: 1]

## 1. General Information
**1.2 Game name**
Lootbugs [cite: 1]

**1.3 Tag Line**
The queen desires food, make sure it's not you. [cite: 1]

**1.4 Elevator Pitch**
In this survival roguelike game, the players roam through generated levels scavenging for lost and abandoned items to feed the queen. [cite: 1] If they satisfy her they can buy upgrades to go out again. [cite: 1] If they fail to satisfy her, she will take the closest thing to eat, you. [cite: 1]

**1.5 Date of last update**
(Not specified) [cite: 1]

---

## 2. Game Progression
**2.1 Game Concept**
You and up to 3 friends have to scour abandoned locations in search for items to feed the queen. [cite: 1] You have to collect enough to satisfy her or you will become her food. [cite: 1] If you can satisfy her you gain money to enhance yourself to help you survive your search for more food. [cite: 1]

**2.2 Target Audience**
Gamers that want to have fun together with their friends or solo. [cite: 1]

**2.3 Genre(s)**
Roguelike survival [cite: 1]

**2.4 Game Flow Summary**
* **Base:** Where you join to wait for your friends and start the game from. [cite: 1]
* **Looting:** You go to a random location and explore it to find items and bring them back to the start whilst avoiding the dangers. [cite: 1]
* **Return:** Return to the base to feed the queen, you lose if you don't have enough. [cite: 1]
* **Shop:** If you gathered enough you get money to buy upgrades from the shop located in the base to help you on your next run. [cite: 1]
* **Repeat:** Repeat until you have played enough or lose. [cite: 1]

**2.5 Look and Feel**
* **Visual style:** Semi-realistic scifi style, lots of dark colors offset by neon lights. [cite: 1]
* **Color Palette:**
  * Light blue | `#09e6d7` | UI, player face [cite: 1]
  * Dark blue | `#114bad` | UI [cite: 1]
  * *Note:* Other colors will come from testing what fits best. [cite: 1] Mainly darker colors for the old stuff and a bit lighter for higher tech stuff. [cite: 1]
* **Lighting:** Little to no lighting depending on the location, the players will have a flashlight that can be upgraded in the shop. [cite: 1]
* **Vibe:** The game is supposed to feel both scary and not scary, being afraid of the dangers you could find but not full on horror. [cite: 1]

---

## 3. Game Play
**3.1 Objective**
The objective is to scour the generated map for objects tallying up to the amount required to survive and bring them back safely. [cite: 1] The required amount increases each time you pass it. [cite: 1]

**3.2 Game progression**
1. **Start:** When everyone has joined the same lobby or you are ready to start if you are playing solo. [cite: 1] You make your way to your vehicle that will bring you to the level and hit the button to go. [cite: 1]
2. **Looting:** The vehicle brings you to a randomly generated map. [cite: 1] You then start to explore the map, avoiding monsters whilst searching for items that are spread around. [cite: 1] You can only drag a certain amount of items, and when you are full, bring them back to your vehicle to deposit them. [cite: 1] Once you have collected as much as you can your vehicle will bring you back to your base. [cite: 1]
3. **Base:** Here you will feed what you collected to the queen. [cite: 1] If you collected enough you stay alive and can go to the shop to buy upgrades. [cite: 1] If you don't collect enough items, the queen will eat you and you have to start over. [cite: 1]
4. **Repeat:** This loop continues on until you fail to get enough items and lose. [cite: 1]

**Complications**
Spread around in the level are monsters that roam around and try to kill you. [cite: 1] You avoid the monsters by outrunning them and hiding. [cite: 1]

---

## 4. Mechanics
**4.1 Rules**
(Not explicitly detailed) [cite: 1]

**4.2 Model of the game universe**
* **Base:** A small hub base where you can walk around in and mess around until you are ready to start your run. [cite: 1] After your first run, a door opens that leads to the place where you feed the queen, and the shop opens where you can spend your money. [cite: 1]
* **Level:** The level is a connection of different rooms and corridors generated from a set amount of rooms. [cite: 1] Monsters and items are also randomly generated throughout the map, increasing in amount after each successful run. [cite: 1]

**4.3 Physics**
The physics will be realistic, everything will behave with standard gravity. [cite: 1] The players will have the ability to walk on walls but is otherwise also subject to standard physics. [cite: 1] Collision will also be physics based, allowing for players to move and knock around loose objects, but also to be knocked around by them. [cite: 1]

**4.4 Economy**
The player will gain money equal to the value of the collected items, this money scales with an upgradable multiplier. [cite: 1] This money can be used to upgrade the character's stats. [cite: 1]

**4.5 Character / Game Piece movement in the game**
* **Player:** Standard WASD movement, jump with space, can walk on any surface that is big enough. [cite: 1]
* **Monsters:** Walk only on the ground. [cite: 1]

**4.6 Actions, switches and buttons, interacting with objects, means of communication**
* **Player:** Can drag around items, a button for proximity voice chat. [cite: 1]
* **Base:** A button in the vehicle to start the run, the shop with upgrades you can buy. [cite: 1]

**4.7 Conflict**
The main conflict in the game is monsters. [cite: 1] Monsters roam the level, each with a different way to avoid them. [cite: 1] The monsters deal damage or just kill the player outright. [cite: 1]

**4.8 Screen Flow**
There are mainly just world screens, except for the main menu screen and options screens. [cite: 1]

**4.9 Game Options**
There are the standard options for the game: Audio, Video, controls. [cite: 1] Another important option is to choose the main camera mode, choosing between static or dynamic. [cite: 1] Static camera always stays upright, reducing motion sickness. [cite: 1]

---

## 5. Story and narrative
**5.1 Back story**
The game is set in a post-apocalyptic setting, most of the world is destroyed, humans are gone, and what they left behind is now inhabited by monsters and robots. [cite: 1] The players are robotic servants made to serve the queen, a big spider-like robot that needs to consume a lot of items left behind by humans. [cite: 1] The players are created to scavenge the world for things to feed the queen so she can make more robots. [cite: 1]

**5.2 Plot elements**
(Not explicitly detailed) [cite: 1]

**5.3 Story progression**
In a play session the players play out the life of just created robots that go and find food going on until they die. [cite: 1]

**5.4 Cut scenes**
* **Scene 1: Spawning**
  * **Actors:** Player [cite: 1]
  * **Setting:** The player's character opens their eyes in a side chamber and exit it to the base. [cite: 1]

---

## 6. Game world
* **Base:** The base is a small facility with tech like decorations. [cite: 1]
* **Locations:** Abandoned building with modern and high tech items strewn about. [cite: 1]

**6.1 General look and feel of world**
It's a post-apocalyptic world so all the locations are in a broken/abandoned state, most lights are broken or out of power. [cite: 1]

**6.2 Areas**
The levels consist of small rooms connected to each other, occasionally with corridors. [cite: 1] The level is generated from 4 unique rooms that come together to form the map. [cite: 1]

**6.3 General description and physical characteristics**
The rooms consist of some appropriate furniture, which can sometimes used to hide from the monsters. [cite: 1] The furniture will be in the style of the level. [cite: 1]

**6.4 How to relate to the rest of the world**
Most of the game world is in the same apocalyptic state. [cite: 1]

**6.5 What levels use it**
All levels are in an abandoned/broken state. [cite: 1]

**6.6 Connections to other areas**
There is no connection between the separate levels. [cite: 1]

---

## 7. Characters
1. **Player:** The player is a small spider-like, spherical robot with 8 legs total. [cite: 1] The players can choose their own color of their shell. [cite: 1]
2. **Queen:** The queen is a very big version of the player model, with some more details and a giant mouth, where the players need to deposit their items. [cite: 1]
3. **Shopkeeper:** The shopkeeper is the same model as the player, but with a hat. [cite: 1]

**7.4 Abilities**
* **Player:** The player's main ability is to scale any surface that is big enough. [cite: 1] They can drag items around, the more they carry the slower they move. [cite: 1] How much the items weigh them down is determined by their strength stat which can be upgraded. [cite: 1]

---

## 8. Levels
1. Base [cite: 1]
2. Randomly generated level [cite: 1]

---

## 9. Interface
The style of the interfaces is in a pixel art style, appearing on the screen above items when close enough, displaying its name, value or its abilities. [cite: 1] For the player, the hud displays a healthbar and a stamina bar. [cite: 1]
