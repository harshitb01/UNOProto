UNO Slayer – Game Prototype

Networking -
- This project uses Photon PUN 2 for all multiplayer functionality.
- Photon Cloud handles rooms and player connections.
- A custom room-code system is used for Create/Join (no random matchmaking).
- The Master Client performs turn resolution and broadcasts results (cards played, scores, energy, deck counts, timer, turn index).
- All sync events use RaiseEvent for lightweight, event-driven communication.

JSON Card System -
All cards are defined in a single JSON file (cards.json).
Each card includes:
{
  "id": 1,
  "name": "Shield Bearer",
  "cost": 2,
  "power": 3,
  "abilities": ["BlockNextAttack"]
}

JSON is used to:
- Build each player's 12-card deck
- Populate their hand
- Provide data for UI (name, cost, power, readable ability text)
- Drive gameplay logic (cost, power, abilities)


How to Run & Test -
1. Open the Lobby scene in Unity (2022.3 LTS).
2. Press Play (Editor) or build the APK for Android.
3. On one device, tap Create Room → share the generated code.
4. On another device, enter the same code to Join Room.
5. Game begins automatically once 2 players join.

Game Flow:
- 12-card deck → draw 3 at start → draw 1 each turn
- +1 energy per turn (max 6)
- 30-second turn timer
- Simultaneous reveal & ability resolution
- Match ends after 6 turns with a WIN/LOSE/DRAW screen