# Third-party notices

This project contains an independent C# port of mathematical formulas and
constants from the Open Spaced Repetition FSRS projects. The ported code lives
under `backend/KotobaApi/Srs/` (notably `Scheduling/Fsrs7Scheduler.cs` and
`Scheduling/Fsrs7Parameters.cs`).

Reference projects (pinned upstream commit `c137ee6e096f9217632397a8fb2bdb6f6e1b92ae`):

- https://github.com/open-spaced-repetition/fsrs-rs
  (`src/model.rs`, `src/model_v7.rs`, `src/inference_v7.rs`,
  `src/parameter_clipper.rs`, `src/parameter_clipper_v7.rs`)
- https://github.com/open-spaced-repetition/srs-benchmark
  (`models/fsrs_v7.py`)

`fsrs-rs` is distributed under the BSD 3-Clause license, reproduced below.
Redistribution of this port must retain the copyright notice, the list of
conditions, and the disclaimer. Review the upstream repositories' current
license files when integrating.

---

BSD 3-Clause License

Copyright (c) 2023, Open Spaced Repetition

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
