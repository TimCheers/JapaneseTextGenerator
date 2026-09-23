# FSRS-7 reference vectors (upstream-generated)

- Upstream repository: https://github.com/open-spaced-repetition/fsrs-rs
- Upstream commit: `c137ee6e096f9217632397a8fb2bdb6f6e1b92ae`
- Generator: `examples/conformance_vectors.rs` (temporary cargo example, not
  checked in), run with `cargo run --example conformance_vectors` from a
  checkout of the pinned commit. It drives the public API
  (`FSRS::new(&DEFAULT_PARAMETERS)`, `next_states`,
  `next_states_with_elapsed_days`, `current_retrievability`,
  `interval_at_retrievability`) with default parameters and desired
  retention 0.90.
- Values below are Rust `f32` `Display` output (shortest round-trip).
  `Fsrs7ConformanceTests` asserts them with float-appropriate tolerances
  (absolute 1e-6 for retrievability/difficulty, relative 1e-5 with an
  absolute 1e-6 floor for stability/interval). Bitwise equality across
  Rust and .NET is not required.

## A. First review (elapsed 0 days, retention 0.90)

| rating | stability | difficulty | stability_fast | interval (days) |
| ------ | --------- | ---------- | -------------- | --------------- |
| Again  | 0.1104    | 6.1686     | 0.08832        | 0.000038275626  |
| Hard   | 2.2395    | 5.261278   | 1.7916001      | 0.5974598       |
| Good   | 3.9221    | 3.5307243  | 3.13768        | 4.7777247       |
| Easy   | 11.7841   | 1          | 9.427279       | 53.869392       |

These match the published `next_states` doctest in `src/inference.rs`.

## B. Forgetting curve

State `(stability=12, difficulty=5, stability_fast=9.6)`, elapsed 10 days:

```text
retrievability = 0.91965944
```

## C. Interval solving

State `(stability=10, difficulty=5, stability_fast=8)`:

```text
interval at 0.90 = 12.750781
interval at 0.95 = 2.0461018
```

## D. Mixed sequence (retention 0.90)

| step | rating | elapsed (days) | retrievability before | stability | difficulty | stability_fast | interval (days) |
| ---- | ------ | -------------- | --------------------- | --------- | ---------- | -------------- | --------------- |
| D0   | Good   | 0              | —                     | 3.9221    | 3.5307243  | 3.13768        | 4.7777247       |
| D1   | Good   | 0.25           | 0.95115894            | 7.227843  | 3.4977171  | 101.86815      | 14.995506       |
| D2   | Hard   | 1.5            | 0.9628668             | 10.180039 | 6.0976644  | 218.94925      | 11.329172       |
| D3   | Again  | 0.1            | 0.9872349             | 2.310639  | 9.474576   | 0.4219594      | 0.008041879     |
| D4   | Easy   | 2              | 0.776888              | 4.367909  | 9.169398   | 23.296722      | 0.7417048       |

Elapsed times: D1 is 6h after D0, D2 is 1.5d after D1, D3 is 2.4h after D2,
D4 is 2d after D3.

## E. Long-gap successful review

Good, then Good 30 days later:

```text
retrievability before = 0.8127792
stability = 17.902279
difficulty = 3.4977171
stability_fast = 219.67287
interval = 47.952896
```
