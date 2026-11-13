# Dancing Mad (Ultimate) P1: strategy spec (Mana / Yan Flash)

Phase 2 deliverable. For every P1 mechanic and role: where each player goes, why, and the source.
Mechanic facts (IDs, timings, variants) live in [p1-evidence.md](p1-evidence.md) and are not
repeated here.

## Sources and how they are used

- **Primary: Yan Flash** 「絶妖星乱舞 攻略特設」 (`yan-flash.com/ultimate/yosei-ranbu#ult-phase-1`,
  updated 2026-07-28).
  - The page's P1 text is quoted verbatim below.
  - Its 23 P1 diagrams were read image by image. Coordinates taken from them carry
    **[Yan fig.]** and are rounded to 0.5 y. The figures' arena edge is r = 20, so the scale comes
    from the arena circle, not from the waymarks drawn in the figures.
- **Cross-check: Kitten** (`kitten-erp-guides.moe/ultimates/dmu/p1/graven_image_{1,2,3}`). Kitten
  says the Mana PF strategy follows Yan:
  > "both Elemental and Mana follows Yan for the stack and spread positions"
  
  Kitten diagrams are cited as **[Kitten fig.]**.
- **Corroboration: the static's own execution.** The 2026-11-11 pulls show the owner's static
  playing exactly this strategy. These are median positions from ACT combatant and ability rows
  across the pulls of each variant, marked **[static]**. They are observed play, not game rules.
  They mainly confirm that the figures were read correctly.
- **Waymarks** (the static's preset, from the trace):

  | Waymark | Position | Waymark | Position |
  |---|---|---|---|
  | A | (100, 87.5) | 1 | (90.5, 90.5) |
  | B | (113.8, 100) | 2 | (109.4, 90.4) |
  | C | (100, 113.8) | 3 | (109.5, 109.4) |
  | D | (86.3, 99.9) | 4 | (90.5, 109.5) |

  The Yan figures draw the same layout.

Roles: MT, ST, H1, H2, D1–D4 (Yan's labels). Coordinates use world (x, y) with the centre at
(100, 100) and +y south. Kefka stands at the centre facing north for everything except GI3's final
section.

Labels:

- **Rule**: stated in a source.
- **Inferred**: a gap I filled. The reason is given, and the gap is listed in §5 for review.

---

## 1. Opening: Revolting Ruin III #1 (GI1 scenario start)

**Yan:**

> ばりばりルインガ
> タンク強攻撃（90度扇範囲）
> 1回目はヘイト1位、2回目はヘイト2位にきます。

**Kitten:**

> "Once the tankbuster marker appears on the MT, have the ST provoke immediately with stance on
> while the MT shirks, so that the MT naturally becomes the 2nd highest aggro."

The owner's brief says the same: the ST provokes during the cast bar.

| Role | Position / action | Basis |
|---|---|---|
| MT | Tanks north of Kefka, about (100, 93.5). Takes both hits (kitchen sink). Shirks after the ST's provoke | Rule (Kitten). Position [static]: MT at (100.0, 93.6) at hit 1 |
| ST | Provokes **during the cast bar** (cast start to hit 1). Becomes enmity 1; the MT becomes enmity 2 and takes hit 2 | Rule (Kitten, owner). Evidence: works anywhere in the bar |
| Others | Outside the 120° north cone: behind Kefka (south half) or wide to the sides | Rule (cone 120°, retail trace; Yan says 90°). The position is Inferred (standard tankbuster spacing) |

Afterwards the ST holds aggro until the GI2 swap.

## 2. Graven Image 1

### 2.1 Mystery Magic 1 (fire + ice) with the knockback tether

**Yan:**

> タンヒラかDPSに線がつき、その後像からノックバックされます。
> ブリザガとファイガの真偽を判断します。
> 個々のマーカーがついていた場合は散開で、頭割りマーカーがついていたら頭割り。
> 嘘の場合はその逆の処理をします。
> ブリザガも同様で見えている範囲が本当で、嘘だとAoEを踏みにいきます。

The fire table on the page:

| | 本当 | 嘘 |
|---|---|---|
| 個々のマーカー | 散開 | 頭割り |
| 頭割りマーカー | 頭割り | 散開 |

Truth shows as blue fire/ice orbs; a lie shows red 「?」 orbs [Yan fig.].

**Rule [Yan fig., Kitten fig.]:**

- Supports always resolve in the **west half** and DPS in the **east half**.
- Each group takes whichever of its two quadrants is safe from the real cones:
  - cones NE+SW → supports NW, DPS SE;
  - cones NW+SE → supports SW, DPS NE.

Spread spots, read from the bright and faded icons of the Yan spread figure:

| Role | Safe NW/SE (cones NE+SW) | Safe SW/NE (cones NW+SE) | [static] median |
|---|---|---|---|
| MT | (98, 90) | (99, 109.5) | (99.2, 106.8) for NW+SE cones |
| ST | (92.5, 98.5) | (92, 101) | (95.3, 100.7) |
| H1 | (88.5, 90) | (89.5, 111) | (92.9, 112.7) / (93.7, 86.8) |
| H2 | (81.5, 98.5) | (82, 102.5) | (82.2, 100.1) / (82.0, 98.5) |
| D1 | (102, 108.5) | (101, 90.5) | (100.7, 94.3) / (101.0, 107.7) |
| D2 | (108.5, 101.5) | (110.5, 99) | (105.7, 98.4) / (106.7, 100.8) |
| D3 | (111.5, 111.5) | (110, 90) | (111.3, 97.2)* / (107.5, 109.6) |
| D4 | (118.5, 101.5) | (118, 98) | (113.9, 99.4) / (114.9, 101.5) |

\* D3 is the only notable deviation: the static's D3 stood further east-inner than Yan's NE spot.

Stack spots [Yan fig.]. Each stack is centred in its group's safe quadrant:

| Group | Safe NW/SE | Safe SW/NE | [static] median |
|---|---|---|---|
| Supports | (95.5, 94.5) | (96, 105.5) | (96, 98) / (95.5, 103) |
| DPS | (107.5, 106) | (106.5, 94.5) | (106, 101.5) / (106, 98.5) |

The static stacks about 3 y closer to the centre than Yan's figure. Both are safe; the sim uses
Yan's figure.

**Tethered group (Kitten):**

> "If your role group is tethered, then you must have already figured out where you have to go
> before the knockback happens … Using Sprint is recommended here."

- **Inferred** pre-knockback spot: Pulse Wave pushes 13 y directly away from the statue at
  (100, 56), so almost due south. The tethered group stands about 13 y north of its final spot at
  the knockback (34.9), then walks in.
- [static] The tethered supports stood around y ≈ 89–93 at 34.8, i.e. in the north half of their
  side, and walked to their spots after the knockback.

### 2.2 Wave Cannon and towers

**Yan:**

> 像からタンヒラ2人、DPS2人の計4人にビームが来るので横一列に並びます（ビームを受けた場所が塔の出現場所になる）。
> ビームを受けていない人で塔に入る

The line-up is one horizontal row through the centre, west to east [Yan fig., Kitten fig., static]:

| | H2 | H1 | ST | MT | D1 | D2 | D3 | D4 |
|---|---|---|---|---|---|---|---|---|
| x [Yan fig.] | 80.5 | 86 | 91 | 96 | 103.5 | 108.5 | 113.5 | 118.5 |
| x [static] | 82.1 | 86.9 | 93.0 | 97.4 | ~102 | 107.2 | 111.5 | 115.6 |

All at y ≈ 100.

Towers:

- The two beamed players of a group leave towers at their own spots.
- The two **unbeamed players of the same group** soak them (Kitten: "soak the towers of their own
  role group").
- Pairing (owner, §5 G2): a simplified conga line per side. The westernmost unbeamed player takes
  the westernmost tower on their side, and the easternmost takes the easternmost.

### 2.3 Double-trouble Trap #1 (pop at 49.5) and Mystery Magic 2 (ice + thunder)

**Yan:**

> つぎつぎトラップの詠唱完了でランダム2名（タンヒラ1名、DPS1名）にデバフがつき、これをフェーズ1で受け渡していきます。
> 効果時間0秒で自分中心に爆発（頭割り）+ノックバックがあるのでデバフ持ちはターゲットサークルの外で待機し、他はタンヒラ、DPSで分かれてノックバックと頭割りを受けます。
> その後なぞなぞマジック（ブリザガ + サンダガ）が来るのでケフカの真偽を確認してギミックを処理します。

**Kitten:**

> "get ready to knockback each group through the middle to the other side of the arena. After the
> knockback, resolve the Mystery Magic by moving to the safe tiles"

| Who | Position | Basis |
|---|---|---|
| Support group (3 non-holders) | Just west of Kefka's ring, (95, 100) | [Yan fig.] (96, 100); [static] (94, 100) |
| Support holder | Outside the ring, on the far (west) side of the group, about (92.5, 100). The knockback carries the group east, through the middle | Rule (Yan, Kitten). Exact spot Inferred from the figure |
| DPS group / holder | Mirror image: (105, 100), holder about (107.5, 100); knocked west | Same |
| Everyone after the knockback | Nearest safe tile of Mystery Magic 2 (quadrant cones ∩ 10 y thunder stripes) | Rule (Kitten). **No per-role spot in either guide** [static: no fixed spots] |

Then Mystery Magic 2 resolves (53.5). **GI1 ends here** (owner decision).

## 3. Graven Image 2

### 3.1 Set 1 (south)

**Yan:**

> 全員に線がつくので、自分はどちらの線を持っているか確認します。
> この線はタンヒラ、DPSで分かれてつきます。
> 線を確認し南で重力弾の頭割りをします。
> 像を見て右下の線→南で頭割り後にそれぞれ散開する
> 像を見て左上の線→重力弾が来るので南で頭割り後に真ん中に移動する
> 散開したとき、自分にくる攻撃判定に重力弾が重なるとワイプになるので、重力弾に重ならないようにしっかり距離を取る

Reading 「右下の線 / 左上の線」 against the evidence:

- **lower-right** = the east statue (126, 41.5, height 7): **Vitrophyre → spread**;
- **upper-left** = the middle statue (102.5, 27, height 22.5): **Gravitas → stack, then middle**.

This matches Kitten:

> "Middle-statue-tethered group gets Gravitas … East-statue-tethered group gets Vitophyre"

| Step | Everyone / role | Position | Basis |
|---|---|---|---|
| Stack (Gravitas 87.4, during Blizzard III Blowout) | All 8 | South, about (100, 109), on the safe side of the C line (the south quadrant away from the real cone) | [Yan fig.] (100, 109); [static] (100.2–100.7, 108.0–109.8). The safe-side shift is Inferred: the stack spot sits on a quadrant boundary |
| Gravitas group after the stack | 4 | Centre, (100, 100): 「真ん中に移動」 | Rule (Yan). [Yan fig.] (100, 100) |
| Vitrophyre spread (91.4) | Spread group | West/east and SW/SE spots below. The spread group may be supports or DPS, independently each set | [Yan fig.], [Kitten fig.], [static] |

South spread spots:

| Slot | If supports spread | If DPS spread | Position [Yan fig.] | [static] |
|---|---|---|---|---|
| West | MT | D1 | (90.5, 103) | D1 (90.4, 100.0) |
| East | ST | D2 | (108.5, 103) | D2 (108.3, 99.4) |
| SW | H1 | D3 | (90, 116) | D3 (88.5, 113.0) |
| SE | H2 | D4 | (109.5, 115.5) | D4 (108.4, 116.6) |

The puddle is centred about (100, 109.5) with r = 5; every slot keeps more than 5 y from it.

### 3.2 Revolting Ruin III #2 + half-room cleave 1

**Yan:**

> ばりばりルインガ（タンク強攻撃）中に半面AoEが来るので像のエフェクトを確認

**Kitten:**

> "Other tank provokes on the marker."
> "purple = west, yellow = east"

The owner's brief also says the MT provokes this time.

| Role | Action | Basis |
|---|---|---|
| ST (current enmity 1, marked) | Tanks north, about (100, 93). Takes both hits | [static] (99.9, 92.9) at hit 1. Rule (Kitten) |
| MT | **Provokes during the cast bar** (92.2 → hit 1 at 97.25). Becomes enmity 1 and tanks after | Rule (owner, Kitten). Evidence: the provoke can land as late as just after hit 1 |
| Everyone | Out of the 120° north cone, then into the half the statue does **not** indicate before 101.25 | Rule (Yan, Kitten) |

Which statue indicates the cleave:

- the east indicator (116, 43) animates for an **east** cleave (Intemperate Will);
- the west indicator (92, 27) for a **west** cleave (Gravitational Wave).

[Yan fig.] shows the party bunched just west of the centre for an east cleave, with the tank north.

### 3.3 Set 2 (north) and half-room cleave 2

**Yan:**

> 再び全員に線がつきますので先ほどと同様に処理します。
> 散開は1回目の上下反転
> その後半面AoEが来ます。

- Stack north, about (100, 91) [Yan fig.]. The Gravitas group then goes to the centre again.
- The set-2 spread is the vertical mirror of set 1 [Yan fig., Kitten fig.]:

| Slot | Supports spread | DPS spread | Position |
|---|---|---|---|
| West | MT | D1 | (90.5, 97) |
| East | ST | D2 | (108.5, 97) |
| NW | H1 | D3 | (90.5, 84) |
| NE | H2 | D4 | (109.5, 84) |

- Cleave 2 (about 114.3): go to the safe half again. It is independent of cleave 1.

### 3.4 Gravitas resolution: trap pass into the puddles

**Yan:**

> つぎつぎトラップのデバフを受け渡すために西タンヒラ、東DPSで分かれて頭割り+ノックバックを受けます。
> ノックバックでグラビガを踏みにいき、デバフ持ちは追って沼に入ります。
> ここで2分バースト

| Who | Position | Basis |
|---|---|---|
| Support group | West of centre, about (96.5, 97) | [Yan fig.] (97, 96.5); [static] (95.8, 98.6) |
| Support holder | South-west of its group, about (95, 100). The knockback pushes the group NE into the **north puddle** (100, 90) | [Yan fig.] |
| DPS group | East/south-east of centre, about (103, 103.5) | [Yan fig.] (102.5, 103.5); [static] (104.9, 100.6) |
| DPS holder | North-east of its group, about (104, 100). The knockback pushes the group SW into the **south puddle** (100, 110) | [Yan fig.] |
| Holders after the pop | Walk into their group's puddle | Rule (Yan) |
| Gravity III (120.8) | Supports in the north puddle, DPS in the south | [Yan fig.]; [static] Gravity III hit supports north, DPS south in every successful pull |

## 4. Graven Image 3

### 4.1 Tele-trouncing: Pinoy box

**Yan:**

> 自分についたデバフを確認します。
> 同じ矢印のデバフの場合→赤枠のポジションに
> 違う矢印のデバフの場合→青枠のポジションに（自分についてる矢印を辿るとポジションがわかりやすいかも）

**Kitten:**

> "you must place the arrow teleporters exactly like how it is in the diagram in the correct order"

The 16 drop spots [Yan fig.] = [Kitten fig. Mana]. Each player owns one box (two spots) and drops
one arrow on each:

| Quadrant | Red box (same arrows) | Blue box (different arrows) |
|---|---|---|
| NW | ↓ (87, 87), ↓ (87, 93) | ← (94, 87), ↑ (94, 93) |
| NE | ← (106, 87), ← (113, 87) | → (106, 93.5), ↑ (113, 93.5) |
| SE | ↑ (113, 105.5), ↑ (113, 112.5) | ↓ (106, 105.5), → (106, 112.5) |
| SW | → (87, 112.5), → (94, 112.5) | ↓ (87, 106), ← (94, 106) |

[static] Every arrow EObj in the recorded pulls landed on these spots within about 1 y, with the
expected facing.

Assignment:

- **Same arrows, direction X → red box:**
  - ↓ S → NW;
  - ← W → NE;
  - ↑ N → SE;
  - → E → SW.
- **Different arrows:** the second arrow is always the first rotated 90° clockwise (evidence). The
  two arrows name the quadrant:
  - {W, N} → NW;
  - {N, E} → NE;
  - {E, S} → SE;
  - {S, W} → SW.
- Each arrow goes on the blue spot drawn with that arrow. Drop the 7 s one first on its spot, then
  move to the other spot for the 10 s one (Kitten: the order matters).

This is Yan's 「自分についてる矢印を辿る」.

### 4.2 Trap pass #3 (167.8)

**Yan:**

> つぎつぎトラップのデバフを受け渡すために西タンヒラ、東DPSで分かれて頭割り+ノックバックを受けます。

**Kitten:**

> "Aim carefully so that you do not end up getting knocked back into a teleporter."

- Supports about (95.5, 100), DPS about (105, 100) [Yan fig.; static].
- Holders stand on the outer side of their group, about (92, 100) and (108, 100), so each group is
  knocked inward along the E–W axis, between the boxes. This is **Inferred**: the axis is free of
  boxes, and the outer holder matches the earlier passes.

### 4.3 Teleport spread: confusion / sleep

**Yan:**

> テレポ散開は近接内、外遠隔で固定（線を見なくてOK）。
> フィールドマーカーの四角の角っちょを意識して置く。

**Kitten:**

> "One role group will be put to sleep, while the other role group will be confused."
> "try to edge close to the teleporter circle without touching it"

| Role | Spot | [static] at the Wills |
|---|---|---|
| MT | NW inner (96, 95.5) | (95.2, 95.7) |
| ST | NE inner (103.5, 95.5) | (102.8, 98.2) |
| D1 | SW inner (95.5, 103.5) | (95.8, 104.5) |
| D2 | SE inner (103.5, 103.5) | (105.2, 104.1) |
| D3 | NW outer (90, 90.5) | — |
| D4 | NE outer (109.5, 89.5) | (107.3, 91.8) |
| H1 | SW outer (90.5, 109.5) | (91.7, 108.4) |
| H2 | SE outer (109, 109.5) | (108.7, 109.3) |

Positions are [Yan fig.] = [Kitten fig. Mana]. The rule is fixed by role; the tethers don't matter.

- **Confused players** automatically walk toward the nearest player. From these spots they cross
  their quadrant's box and chain through the teleporters (Kitten:
  > "trick the confused player to step on all tele-portent"
  ).
- **Sleeping players** stay put.
- The chain path, and how far each arrow teleports, are not in either guide and were not measured
  (§5 G6).

### 4.4 Gaze + final Mystery Magic (fire + thunder)

**Yan:**

> その後像から視線ギミック+真偽サンダガ+真偽ファイガが来ます。
> 近接組：MTは4、STは3、D1は1、D2は2
> 遠隔組：H1は南、H2は東、D3は西、D4は北
> タンヒラは3で、DPSは1で頭割りをする
> ※殴れないパターンがあるので注意

**Kitten:**

> "Ave Maria: Look at it! / Indolent Will: Don't look!"
> "the gaze mechanic will check the view of your character, not your camera"

Gaze:

- Ave Maria: face the Ave Maria statue (95, 25).
- Indolent Will: face away from the Indolent Will statue (105.25, 34).

Both are north-ish, so **Inferred**: face north for Ave Maria and south for Indolent Will.

Spread: role anchors, shifted onto the safe side of the 10 y thunder stripes. [Yan fig.] gives one
orientation; [Kitten fig. Mana] spread1/2 give both:

| Role | Anchor | Spot (orientation +45° / −45°, Kitten Mana) | Yan fig. (+45°) |
|---|---|---|---|
| MT | Waymark 4 (inner) | (93.5, 106.5) / (93.5, 106.5) | (92, 107.5) |
| ST | Waymark 3 (inner) | (105, 106.5) / (108, 109) | (107.5, 107.5) |
| D1 | Waymark 1 (inner) | (95, 95.5) / (95, 95.5) | (92, 92.5) |
| D2 | Waymark 2 (inner) | (107.5, 92.5) / (105, 95.5) | (107, 92.5) |
| H1 | South edge | (106, 118.5) / (100, 118.5) | (94.5, 119) or (104.5, 119) |
| H2 | East edge | (119, 103) / (119, 99) | (119, 95.5) or (119, 104) |
| D3 | West edge | (82.5, 95) / (81.5, 106) | (80.5, 95.5) or (80.5, 104.5) |
| D4 | North edge | (96.5, 81.5) / (105.5, 82.5) | (95, 81) or (104.5, 81) |

Each edge role picks whichever of its two edge spots is in a safe stripe.

Stack (rule): supports at waymark 3, DPS at waymark 1, each shifted into the safe stripe.

- [Yan fig.]: DPS about (92.5, 92.5), supports about (106, 106).
- [Kitten fig. Mana] stack1/2: the same anchors.
- Yan warns that some patterns lose uptime (「殴れないパターン」).

---

## 5. Owner review (2026-11-12) and remaining inferences

Kitten documents both Mana (Yan Flash) and Elemental (Megabin/Lisbin). Only its Mana material is
used, and only as a cross-check. Yan Flash and the logs decide.

### Resolved

| # | Topic | Decision |
|---|---|---|
| K1–K3 | Small position differences against Kitten's figures | Not conflicts. Yan and the logs agree, so Kitten is taken as slightly off |
| G1 | Tethered group's spot before the GI1 knockback | Owner's practice: go **well north** when the final spot is in the NW quadrant (above all the MT's NW spread spot at (98, 90)). **Ignore the knockback** when the final spot is in the SW quadrant, because it carries you there. Logs, successful pulls with tethered supports: the MT stood at y ≈ 90.3–92.7 before the knockback for NW finals and ≈ 92.3–93.5 for SW finals. NW finals ended about (96–97, 97–99) and SW finals about (95–96, 103) |
| G2 | Wave Cannon tower pairing | **Simplified conga line.** Each side (supports west, DPS east) always gets exactly two beams, so two towers and two unbeamed players. The westernmost unbeamed player of a side takes that side's westernmost tower, and the easternmost takes the easternmost |
| G6 | Teleport distance and the confused players' path | A teleport carries a player at least from one paddle to the next in its arrow's direction. The trace's ActorControl 241 warps put it at exactly 6 y from the paddle's centre (evidence §5.3); paddle spacing is 6–7 y, so a ride lands inside the next paddle. Confused players head straight for their nearest player. That is why partners stand diagonally across the box: the confused player has to walk through the arrow paddles |

### Still inferred (not contested)

| # | Gap | Inference |
|---|---|---|
| G3 | Mystery Magic 2 dodge after the trap knockback | Nearest safe tile. Neither Yan nor the static uses fixed per-role spots |
| G4 | Trap-holder spot (all three passes) | Outermost side of the group, at the ring edge, so the knockback goes toward the target (east, west or a puddle) |
| G5 | Facing for the gazes | Ave Maria: face north. Indolent Will: face south |
| G7 | GI2 stack position under each Blizzard diagonal | (100, 109), shifted 2 y into the safe south quadrant |

Early pulls (2, 4, 5) show what failure looks like on a lie. The static stood in the unshown
quadrant, and the real un-omened cones (BA9E) hit them. The fake omened cones (BA9B) "hit" whoever
stood in the shown quadrant without effect, which is exactly Yan's 「嘘だとAoEを踏みにいきます」.
