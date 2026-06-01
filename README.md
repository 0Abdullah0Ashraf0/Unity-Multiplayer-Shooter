# 1v1 FPS — Unity Multiplayer Shooter

A small-scale, fully networked 1v1 first-person shooter built in Unity 6 using Netcode for GameObjects (NGO) and Unity Transport (UTP).

---

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | Unity 6 |
| Networking | Netcode for GameObjects (NGO) |
| Transport | Unity Transport (UTP) — port 7777 |
| Input | Unity New Input System |
| Movement | CharacterController (not Rigidbody) |
| Position sync | ClientNetworkTransform (client-authoritative) |
| Editor testing | Multiplayer Play Mode (MPPM) |

---

## Features

- Networked 1v1 hitscan combat with server-side hit validation
- Health system with death, respawn, and teleport to spawn points
- Ammo system with pickup support and per-round reset
- Round-based scoring — first to N kills wins
- End screen with win/loss result
- Map selection (host picks, scene loads for both players)
- Full HUD — health, ammo, score, hit marker, round banner, countdown
- 3D positional audio for remote players; flat audio for local player
- Menu UI sounds

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── PlayerMovement.cs        # CharacterController movement, crouch, jump
│   ├── MouseMovement.cs         # Camera look, vertical clamp
│   ├── PlayerHealth.cs          # Health NetworkVariable, death, respawn
│   ├── Weapon.cs                # Shoot, reload, ammo, hitscan ServerRpc
│   ├── ScoreManager.cs          # Kill tracking, round flow, respawn trigger
│   ├── HUDManager.cs            # All UI updates, hit marker, countdown
│   ├── GameNetworkManager.cs    # Extends NetworkManager
│   ├── MainMenu.cs              # Host/join, map selection, spawn logic
│   ├── ClientNetworkTransform.cs  # Client-authoritative transform sync
│   └── ClientNetworkAnimator.cs   # Client-authoritative animation sync
```

---

## Player Hierarchy

```
Player (root)
├── PlayerInput          ← must be on root, not a child
├── CharacterController
├── PlayerMovement
├── ClientNetworkTransform
├── Head
│   ├── Camera
│   └── MouseMovement
└── Body
    └── (mesh renderers)
```

---

## Scene Setup

1. All playable scenes must be registered under **File → Build Settings → Scenes In Build**.
2. `NetworkManager` must have **Enable Scene Management** checked.
3. Tag all spawn point GameObjects with `SpawnPoint`.
4. `ScoreManager` must live in the **game/map scene** — not the menu scene (which gets destroyed on `LoadSceneMode.Single`).
5. Active Input Handling in Project Settings must be set to **"Input System Package (New)"** only — not "Both".

---

## Networking Architecture

| Concern | Authority |
|---|---|
| Player position | Client (`ClientNetworkTransform`) |
| Health & damage | Server |
| Hit validation | Server (ServerRpc raycast) |
| Ammo state | Local client only |
| HUD updates | Targeted `ClientRpc` via `OwnerClientId` |
| Round/score flow | Server (`ScoreManager`) |
| Scene loading | Server (`NetworkSceneManager`) |

---

## How to Play (LAN — Built Game)

Both machines must be on the same local network.

### Host — Firewall Setup (required before first run)

Windows Firewall blocks incoming connections by default. Run this in an elevated PowerShell **once** on the host machine:

```powershell
New-NetFirewallRule -DisplayName "Unity FPS" -Direction Inbound -Protocol UDP -LocalPort 7777 -Action Allow
```

Then find your local IP:

```powershell
ipv4config
```

Look for the `IPv4 Address` under your active network adapter (e.g. `192.168.1.x`). Share this with the client.

### Host
1. Launch the game and click **Host**.
2. Select a map — this loads the game scene for both players.

### Join
1. Launch the game on a second machine and click **Join**.
2. Enter the host's local IP address and click **Connect**.

> **Editor / MPPM testing:** No firewall setup needed. Both virtual players connect over `127.0.0.1` (localhost) automatically.

### In Game
| Action | Input |
|---|---|
| Move | WASD |
| Look | Mouse |
| Jump | Space |
| Walk | Left Shift |
| Crouch | Left Control |
| Shoot | Left Mouse Button |
| Reload | R |

---

## Editor Testing (MPPM)

Use **Multiplayer Play Mode** to run two clients in the editor without a build:

1. Open **Window → Multiplayer Play Mode**.
2. Enable a second virtual player.
3. Enter Play Mode — one window acts as Host, the other as Client.

---

## Architecture Decisions

- **`CharacterController` over `Rigidbody`** — gives direct, predictable control over player movement without physics interference. Paired with `ClientNetworkTransform` (not `NetworkRigidbody`, which is physics-only) for position sync.
- **Client-authoritative movement** — the owning client drives its own transform for responsive, lag-free movement. All damage and hit validation remains server-authoritative to prevent cheating.
- **Hitscan over projectiles** — raycast-based shooting avoids the complexity of spawning and syncing networked bullet objects across clients, which is unnecessary for a short-range 1v1 format.
- **Renderer toggling on death** — on player death, renderers are disabled rather than calling `SetActive(false)` on the NetworkObject root, which would break NGO's object tracking.
- **`PlayerInput` on the root GameObject** — required by Unity's New Input System; placing it on a child object prevents input from being correctly routed to sibling and parent components.
- **`ScoreManager` in the game scene** — placed in the map scene rather than the menu scene because `LoadSceneMode.Single` destroys all objects from the previous scene, including anything marked `DontDestroyOnLoad` that wasn't explicitly protected.

---

## Known Issues

- **No Return to Menu button** — the end screen shows the result but there is currently no button to navigate back to the main menu. The game must be relaunched to play again.
- **Ammo pickup only applies to host** — ammo pickup logic runs locally without a ServerRpc, so only the host's ammo state is updated when a pickup is collected. The picked-up ammo is also non-functional as `PickupAmmo()` is a private method not reachable from the pickup object.

---

## Build

1. Add all scenes to Build Settings.
2. Set platform to **Windows / Mac / Linux Standalone**.
3. Build and distribute — clients connect to the host's LAN IP on port 7777.
