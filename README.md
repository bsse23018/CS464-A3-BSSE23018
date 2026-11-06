# CS464 Assignment 03 · Muhammad Tayyab · BSSE23018

- **Started from:** My Assignment 02 repo: https://github.com/bsse23018/CS464-A2-BSSE23018 — I extended its five levels.
- **Controls:** WASD / arrow keys move, SPACE jump, R restart the level, N skip to next level (testing)
- **To win a level:** collect the required coins (shown as Score x / y on screen), then step on the goal. It turns from red to green when it is open.

## Levels

| Level | Screenshot | New idea | Combined with |
|---|---|---|---|
| Level01 | <img src="Docs/a3/level01.png" width="320"> | Coins, a score, a goal, and a bounce pad (VelocityChange) | Nothing hurts yet |
| Level02 | <img src="Docs/a3/level02.png" width="320"> | Spikes that cost one heart at a time, with i-frames and knockback | Coins next to the spikes (risky detours) |
| Level03 | <img src="Docs/a3/level03.png" width="320"> | A guard that patrols, chases, gives up and returns | Spikes beside its route, a checkpoint |
| Level04 | <img src="Docs/a3/level04.png" width="320"> | The crusher: rest, yellow warning, then a 2-damage strike | A ping-pong guard between the crushers |
| Level05 | <img src="Docs/a3/level05.png" width="320"> | Two speed power-ups (second pickup refreshes the timer) | Everything so far, plus a second guard |

## The numbers

| Number | Value | Why this value |
|---|---|---|
| Patrol speed | 2 m/s (2.5 for the ping-pong guard in Level05) | At 2 m/s (one-third of the player's 6 m/s speed), the guard moves steadily so the player has time to observe the route, learn the pattern, and navigate past safely. |
| Chase range | 6 m start chasing, 9 m give up | The 3 m hysteresis gap between detection and give-up prevents rapid back-and-forth state flickering at the edge, giving the player a clear and fair opportunity to break pursuit. |
| Knockback | 9–12, ForceMode.Impulse, steering off 0.25 s | Throws the player back roughly 2–2.5 meters. Cutting steering input for 0.25 s prevents movement keys from instantly cancelling the impulse, ensuring the player is shoved clear off hazard tiles. |
| I-frames | 1 s (2 s after a respawn) | 1 second gives ample recovery time to step off a 2x2 spike tile without taking chained damage; 2 seconds on respawn prevents immediate spawn-killing near active hazards. |
| Power-up duration | 5 s, x1.6 speed | Boosts speed from 6 m/s to 9.6 m/s for 5 seconds—long enough to cross a dangerous hazard alley or outrun guards, but short enough to avoid trivializing the whole level. |
| Chase speed | 3.5 m/s | Moderately slower than the player's 6 m/s speed, ensuring an attentive player running in a straight line can always escape, while still creating tension in tight corridors. |
| Crusher warning | 0.8 s | Comfortably exceeds human reaction time (~0.25 s), giving players adequate visual telegraphing to spot the yellow danger marker and back away before the 2-heart slam. |

## Short answers

**1. Why does a trigger need a Rigidbody on at least one of the two objects?**

Unity's PhysX collision pipeline requires at least one of the two participating colliders to have an attached Rigidbody (dynamic or kinematic) to identify the interaction as dynamic rather than static-static world geometry, which is ignored for trigger callbacks. In our game, collectibles, spikes, and checkpoints are static trigger colliders that depend on the player's dynamic Rigidbody to trigger `OnTriggerEnter` and `OnTriggerStay`. The guard also carries a kinematic Rigidbody with a trigger CapsuleCollider, ensuring that trigger events register reliably between moving entities.

**2. Which ForceMode do your knockback and your Part A force use, and why each?**

`BouncePad` uses `ForceMode.VelocityChange` because it applies an instantaneous vertical boost that completely ignores the player's Rigidbody mass, producing a reliable, consistent launch height regardless of mass tweaks. Conversely, `Hazard` and `PlayerController.Knockback` use `ForceMode.Impulse` to deliver an instantaneous physical impact that accounts for mass. `Knockback` specifically zeroes sideways speed (`rb.linearVelocity = new Vector3(0f, v.y, 0f)`) before applying the impulse so that existing player momentum does not counteract or exaggerate the knockback force.

**3. What does your code do when a second power-up is picked up while one is running?**

Our code implements the refresh rule: collecting a second power-up while an existing boost is running resets the duration back to the full 5 seconds rather than stacking speeds or discarding the pickup. The timer and `BoostFor` coroutine live on the `PlayerController` component because the collectible `PowerUp` GameObject is immediately destroyed (`Destroy(gameObject)`) upon contact. When collected, `BoostSpeed` halts the running coroutine with `StopCoroutine(boostRoutine)`, restarts the timer for the full 5 seconds, and displays a refresh notification on the HUD.

**4. Why can `==` fail as an arrival check, and when does it work?**

Floating-point precision limits and variable frame displacement (`speed * Time.fixedDeltaTime`) mean an object's position will almost never land precisely on exact waypoint coordinates, causing a strict `==` equality check to overshoot and fail indefinitely. Using a distance tolerance like `Vector3.Distance(a, b) < arriveDistance` solves this by accepting any point within a small arrival bubble. Exact equality `==` only works reliably if positions are quantized onto discrete grid coordinates, or when `Vector3.MoveTowards` is used and the remaining distance to target is less than the current step size, in which case Unity explicitly clamps the position exactly onto the target.
