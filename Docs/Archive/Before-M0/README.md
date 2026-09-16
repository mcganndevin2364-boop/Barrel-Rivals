> Historical material from before M0. Superseded by Docs/Plan and the current root instructions. Completion grades, engine versions, section numbering and some dimensions below were not validated.

# Barrel Rivals — 1v1 Competitive Barrel Racing

**Barrel Rivals** is a turn-based 1v1 competitive rodeo barrel racing game built in **Unity 2022.3 LTS (URP)** with **Photon Fusion 2 Host Mode**, engineered for locked 60 FPS on mobile (<150 draw calls).

---

## 🏆 Match Structure & Winner Rules

Matches are structured into **3 Rodeo Rounds**. In each round, **both players get 1 run** (6 total runs per match):
1. **Round 1:** Player A runs $\to$ Player B runs.
2. **Round 2:** Player A runs $\to$ Player B runs.
3. **Round 3:** Player A runs $\to$ Player B runs.

### ⏱️ Scoring & Penalties
- **Run Time Calculation:**
  $$\text{Round Time} = \text{Raw Elapsed Seconds} + \Big(\text{Knocked Barrels} \times 5.0\text{s}\Big)$$
- **Knocking a Barrel:** Adds a **$+5.0\text{s}$ penalty** to that round's time.
- **Winner Determination:** At the end of 3 rounds, each player's 3 round times are averaged:
  $$\text{Average Time} = \frac{\text{Round 1 Time} + \text{Round 2 Time} + \text{Round 3 Time}}{3}$$
- **The player with the fastest (lowest) average time wins the match prize purse and trophies!**

---

## ⚡ Arcade Feel & Performance Aids
Drift charging and whip rhythm boosts are performance aids used to shave seconds off your raw time:
- **Mario Kart 3-Tier Drift Turbo:** Blue ($+6\%$), Orange ($+12\%$), Purple ($+20\%$) speed boosts coming out of barrel apexes.
- **Rhythm Whip Bursts:** Up to 3 timed whips per run to accelerate your horse down the home stretch.

---

## 🏟️ Arena Tier & Economy Progression

| Tier | Arena Name | Entry Fee | Win Prize | Par Time | Trophy Win / Loss | Max Trophy Cap |
|:---:|---|:---:|:---:|:---:|:---:|:---:|
| **0** | Bronze Arena (Oak Ridge) | 500 🪙 | 900 🪙 | 16.0s | +20 / -5 | 200 🏆 |
| **1** | Silver Arena (Dusty Gulch) | 2,000 🪙 | 3,600 🪙 | 15.0s | +22 / -12 | 500 🏆 |
| **2** | Gold Arena (Lone Star) | 8,000 🪙 | 14,400 🪙 | 14.0s | +24 / -18 | 1,000 🏆 |
| **3** | Diamond Arena (Royal Stampede) | 25,000 🪙 | 45,000 🪙 | 13.0s | +25 / -22 | 2,000 🏆 |
| **4** | Champion Arena (Triple Crown) | 75,000 🪙 | 135,000 🪙 | 12.5s | +25 / -25 | ∞ (Uncapped) |
