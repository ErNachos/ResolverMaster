"""Generate every sound effect of Resolver Master from code (ErNa-012): soft, "cozy" cartoon sounds, all with one character.

Output: Assets/Audio/Sfx/<name>.wav (44.1 kHz, mono, 16 bit). The game maps its events to these files in AudioCatalog.Sounds
(Assets/Scripts/Core/Audio/GameAudio.cs): similar events share a file with a small change of pitch.

Deterministic: the same script writes the same files (fixed seeds, no time, no randomness from outside).
Every sound is described by the readable parameters in SOUNDS below: change a number and run the script again.

Style rules (applied to every sound by finish()):
  - soft timbres (sine, marimba, soft bell, filtered noise), fundamentals kept low, a gentle low-pass on everything (LOWPASS_HZ);
  - same perceived loudness: K-weighted (ITU-R BS.1770) loudness through a small-speaker high-pass (SPEAKER_HZ), loudest 100 ms
    window, brought to TARGET_LOUDNESS_DB; every sound has its body above ~250 Hz (phone speakers);
  - peaks never above PEAK_LIMIT (no saturation); short fades at both ends (no click).

Usage (from the project root):  python Tools/Audio/make_sfx.py
Needs numpy and scipy (pip install numpy scipy).
"""
import os
import sys

import numpy as np
from scipy import signal
from scipy.io import wavfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Audio", "Sfx")

SR = 44100
LOWPASS_HZ = 6500            # nothing sharp: everything is gently low-passed
HIGHPASS_HZ = 45             # no rumble, no DC
TARGET_LOUDNESS_DB = -19.0   # loudness of the loudest 100 ms, as a phone speaker plays it (the same for every sound)
SPEAKER_HZ = 300             # phone speakers give little below this: the loudness is measured through this high-pass
PEAK_LIMIT = 0.85            # ~ -1.4 dBFS: never clipped
FADE_IN = 0.004              # seconds
FADE_OUT = 0.025
MAX_EFFECT = 1.0             # seconds: an effect is shorter than this (jingles: MAX_JINGLE)
MAX_JINGLE = 2.5

# ----------------------------------------------------------------------------------------------------------------------------------
# The sounds. "kind" picks the recipe (functions below), the rest are its parameters.
# Notes: scientific names (C5 = 523 Hz). Times and durations in seconds. "level_db": a small correction after the loudness match.
# ----------------------------------------------------------------------------------------------------------------------------------
SOUNDS = {
    # --- UI ---
    "ui_click":       dict(kind="blip", f0=900, f1=620, dur=0.07, tau=0.018, tone=0.8, noise=0.25, noise_hz=2500, level_db=-3),
    "ui_swipe":       dict(kind="whoosh", dur=0.22, f0=500, f1=1800, band_low=350, seed=11),
    "ui_panel_open":  dict(kind="notes", timbre="marimba", notes=[("G4", 0.00, 0.20), ("C5", 0.07, 0.28)], tau=0.09),
    "ui_panel_close": dict(kind="notes", timbre="marimba", notes=[("C5", 0.00, 0.20), ("G4", 0.07, 0.28)], tau=0.09),
    "star":           dict(kind="notes", timbre="bell", notes=[("E5", 0.00, 0.55)], tau=0.22, sparkle="B5"),
    "jingle_unlock":  dict(kind="notes", timbre="marimba", tau=0.16, sparkle="C6",
                           notes=[("G4", 0.00, 0.3), ("C5", 0.09, 0.3), ("E5", 0.18, 0.3), ("G5", 0.27, 0.7)]),
    # --- Plumber ---
    "pipe_rotate":    dict(kind="tick", clicks=[(0.0, 1.0), (0.045, 0.6)], f=1300, body=330, dur=0.11, seed=21),
    "bonk":           dict(kind="thump", f0=420, f1=260, dur=0.16, tau=0.05, noise=0.25, noise_hz=1100, seed=22),
    "water_flow":     dict(kind="bubbles", dur=0.85, count=11, f_low=380, f_high=900, rise=1.2, flow=0.35, seed=23),
    "water_arrive":   dict(kind="bubbles", dur=0.45, count=4, f_low=520, f_high=760, rise=1.6, flow=0.12, seed=24,
                           chime="G5"),
    # --- common jingles ---
    "jingle_win":     dict(kind="notes", timbre="marimba", tau=0.20, sparkle="C6",
                           notes=[("C5", 0.00, 0.25), ("E5", 0.11, 0.25), ("G5", 0.22, 0.25),
                                  ("C5", 0.40, 1.1), ("E5", 0.40, 1.1), ("G5", 0.40, 1.1), ("C6", 0.40, 1.1)]),
    "uh_oh":          dict(kind="notes", timbre="soft", tau=0.25, vibrato=0.006,
                           notes=[("E4", 0.00, 0.22), ("C4", 0.20, 0.42)]),
    "jingle_lose":    dict(kind="notes", timbre="soft", tau=0.30, vibrato=0.007,
                           notes=[("G4", 0.00, 0.26), ("E4", 0.22, 0.26), ("D4", 0.44, 0.26), ("C4", 0.66, 0.9)]),
    # --- Traffic jam ---
    "car_grab":       dict(kind="blip", f0=420, f1=700, dur=0.09, tau=0.035, tone=1.0, noise=0.0, noise_hz=2000),
    "car_snap":       dict(kind="thump", f0=420, f1=300, dur=0.09, tau=0.025, noise=0.45, noise_hz=1800, seed=31),
    "car_vroom":      dict(kind="vroom", dur=0.75, f0=130, f1=280, cutoff0=700, cutoff1=1900, seed=32),
    # --- Pino ---
    "ice_slide":      dict(kind="whoosh", dur=0.40, f0=1500, f1=900, band_low=600, seed=41),
    "snow_stop":      dict(kind="thump", f0=330, f1=220, dur=0.20, tau=0.06, noise=0.8, noise_hz=1300, seed=42),
    "fish_pop":       dict(kind="blip", f0=480, f1=1050, dur=0.13, tau=0.045, tone=1.0, noise=0.0, noise_hz=2000, sparkle="E6"),
    "igloo_open":     dict(kind="notes", timbre="bell", tau=0.20, notes=[("G4", 0.00, 0.45), ("D5", 0.12, 0.55)]),
    "happy_hop":      dict(kind="notes", timbre="marimba", tau=0.10,
                           notes=[("C5", 0.00, 0.16), ("E5", 0.06, 0.16), ("G5", 0.12, 0.30)]),
    "splash":         dict(kind="splash", dur=0.60, seed=43),
    "sink":           dict(kind="bubbles", dur=0.70, count=7, f_low=220, f_high=420, rise=0.6, flow=0.15, seed=44,
                           glide=(440, 230)),
    # --- common ---
    "hint":           dict(kind="notes", timbre="bell", tau=0.16,
                           notes=[("E5", 0.00, 0.3), ("G#5", 0.07, 0.3), ("B5", 0.14, 0.45)]),
    "undo":           dict(kind="blip", f0=720, f1=430, dur=0.14, tau=0.05, tone=1.0, noise=0.1, noise_hz=1500),
}

NOTE_INDEX = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1, "A": 0, "A#": 1, "B": 2}


def note(name):
    """Frequency of a note name (A4 = 440 Hz)."""
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2.0 ** ((NOTE_INDEX[pitch] + 12 * (octave - 4)) / 12.0)


def times(dur):
    return np.arange(int(round(dur * SR))) / SR


def osc(freq):
    """Sine with a frequency that may change over time (array) without phase jumps."""
    freq = np.asarray(freq, dtype=np.float64)
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


def decay(n, tau, attack=0.004):
    """Soft attack (raised cosine), exponential decay."""
    t = np.arange(n) / SR
    env = np.exp(-t / max(tau, 1e-4))
    a = max(1, int(attack * SR))
    env[:a] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(a) / a)
    return env


def lowpass(x, hz, order=2):
    return signal.sosfilt(signal.butter(order, hz, "low", fs=SR, output="sos"), x)


def bandpass(x, lo, hi, order=2):
    return signal.sosfilt(signal.butter(order, [lo, hi], "band", fs=SR, output="sos"), x)


def sweep_lowpass(x, f_start, f_end):
    """Two one-pole low-passes whose cutoff glides (exponentially) from f_start to f_end: a soft, moving filter."""
    n = len(x)
    fc = f_start * (f_end / f_start) ** (np.arange(n) / max(1, n - 1))
    a = 1.0 - np.exp(-2 * np.pi * fc / SR)
    y1 = np.zeros(n)
    y2 = np.zeros(n)
    s1 = s2 = 0.0
    for i in range(n):
        s1 += a[i] * (x[i] - s1)
        s2 += a[i] * (s1 - s2)
        y1[i] = s1
        y2[i] = s2
    return y2


def place(buf, x, start):
    i = int(round(start * SR))
    end = min(len(buf), i + len(x))
    buf[i:end] += x[:end - i]


# ----------------------------------------------------------------------------------------------------------------------------------
# Timbres and recipes
# ----------------------------------------------------------------------------------------------------------------------------------

def voice(freq, dur, timbre, tau, vibrato=0.0):
    """One note. marimba: woody, quick decay; bell: soft glassy partials; soft: rounded sine with a little warmth."""
    n = int(round(dur * SR))
    t = np.arange(n) / SR
    f = freq * (1 + vibrato * np.sin(2 * np.pi * 5.5 * t) * np.clip(t / 0.15, 0, 1))
    if timbre == "marimba":
        x = osc(f) + 0.10 * osc(f * 2) * decay(n, tau * 0.4) + 0.06 * osc(f * 3.98) * decay(n, tau * 0.15)
        env = decay(n, tau, 0.003)
    elif timbre == "bell":
        x = osc(f) + 0.28 * osc(f * 2.0) * decay(n, tau * 0.7) + 0.08 * osc(f * 3.01) * decay(n, tau * 0.4) \
            + 0.03 * osc(f * 4.2) * decay(n, tau * 0.25)
        env = decay(n, tau * 1.6, 0.004)
    else:  # soft
        x = osc(f) + 0.30 * osc(f * 2) + 0.08 * osc(f * 3)
        env = decay(n, tau * 2.0, 0.02)
    return x * env


def sparkle_note(name, dur=0.35):
    """A quiet high bell on top: the 'shine' of a positive sound (kept soft and below 1.6 kHz for the fundamental)."""
    return 0.18 * voice(note(name), dur, "bell", 0.10)


def r_notes(p):
    length = max(s + d for _, s, d in p["notes"]) + 0.05
    buf = np.zeros(int(round(length * SR)))
    for name, start, dur in p["notes"]:
        place(buf, voice(note(name), dur, p["timbre"], p["tau"], p.get("vibrato", 0.0)), start)
    if p.get("sparkle"):
        last = max(s for _, s, _ in p["notes"])
        place(buf, sparkle_note(p["sparkle"]), last + 0.03)
    return buf


def r_blip(p):
    n = int(round(p["dur"] * SR))
    t = np.arange(n) / SR
    f = p["f0"] * (p["f1"] / p["f0"]) ** np.clip(t / (p["dur"] * 0.6), 0, 1)
    x = p["tone"] * (osc(f) + 0.12 * osc(2 * f)) * decay(n, p["tau"], 0.002)
    if p["noise"] > 0:
        rng = np.random.default_rng(5)
        x += p["noise"] * lowpass(rng.standard_normal(n), p["noise_hz"]) * decay(n, 0.008, 0.001)
    if p.get("sparkle"):
        buf = np.zeros(n + int(0.3 * SR))
        place(buf, x, 0)
        place(buf, sparkle_note(p["sparkle"], 0.25), 0.04)
        return buf
    return x


def r_whoosh(p):
    """Filtered noise whose colour glides (a swipe, a slide on ice), swelling in and out."""
    n = int(round(p["dur"] * SR))
    rng = np.random.default_rng(p["seed"])
    x = sweep_lowpass(rng.standard_normal(n), p["f0"], p["f1"])
    x = signal.sosfilt(signal.butter(2, p["band_low"], "high", fs=SR, output="sos"), x)
    u = np.arange(n) / n
    return x * np.sin(np.pi * u) ** 1.5


def r_tick(p):
    """A small mechanical tick: short filtered clicks over a tiny woody body."""
    n = int(round(p["dur"] * SR))
    rng = np.random.default_rng(p["seed"])
    buf = np.zeros(n)
    for start, gain in p["clicks"]:
        m = int(0.03 * SR)
        click = bandpass(rng.standard_normal(m), p["f"] * 0.6, p["f"] * 1.4) * decay(m, 0.004, 0.0005)
        body = 0.6 * osc(np.full(m, p["body"])) * decay(m, 0.012, 0.001)
        place(buf, gain * (click + body), start)
    return buf


def r_thump(p):
    """A soft knock: a low sine falling in pitch and a puff of low-passed noise."""
    n = int(round(p["dur"] * SR))
    t = np.arange(n) / SR
    f = p["f1"] + (p["f0"] - p["f1"]) * np.exp(-t / 0.03)
    rng = np.random.default_rng(p["seed"])
    x = (osc(f) + 0.3 * osc(2 * f)) * decay(n, p["tau"], 0.002)
    x += p["noise"] * lowpass(rng.standard_normal(n), p["noise_hz"]) * decay(n, p["tau"] * 0.5, 0.002)
    return x


def bubble(freq, rise, dur=0.07):
    n = int(round(dur * SR))
    t = np.arange(n) / SR
    return osc(freq * (1 + rise * t / dur)) * decay(n, dur * 0.35, 0.002)


def r_bubbles(p):
    """Water: little rising bubbles over a soft flow (or falling, with 'glide', for something that sinks)."""
    n = int(round(p["dur"] * SR))
    rng = np.random.default_rng(p["seed"])
    buf = np.zeros(n + int(0.1 * SR))
    flow = lowpass(rng.standard_normal(n), 900) * np.sin(np.pi * np.arange(n) / n) ** 2
    flow *= 1 + 0.5 * np.sin(2 * np.pi * 7 * np.arange(n) / SR)
    place(buf, p["flow"] * flow, 0)
    starts = np.sort(rng.uniform(0, p["dur"] * 0.8, p["count"]))
    for i, s in enumerate(starts):
        f = rng.uniform(p["f_low"], p["f_high"])
        gain = 0.5 * (1 - 0.5 * s / p["dur"])
        place(buf, gain * bubble(f, p["rise"]), s)
    if p.get("glide"):
        f0, f1 = p["glide"]
        m = int(0.35 * SR)
        f = f0 * (f1 / f0) ** (np.arange(m) / m)
        place(buf, 0.7 * osc(f) * decay(m, 0.12, 0.01), 0)
    if p.get("chime"):
        place(buf, 0.6 * voice(note(p["chime"]), 0.35, "bell", 0.10), 0.08)
    return buf


def r_vroom(p):
    """A small stylized car leaving: a rounded buzzing tone gliding up through a soft opening filter, fading away."""
    n = int(round(p["dur"] * SR))
    u = np.arange(n) / n
    f = p["f0"] * (p["f1"] / p["f0"]) ** (u ** 0.7)
    f *= 1 + 0.02 * np.sin(2 * np.pi * 18 * np.arange(n) / SR)       # a little engine wobble
    x = sum(osc(f * k) / k for k in range(1, 14))                       # band-limited saw: no aliasing
    x = sweep_lowpass(x, p["cutoff0"], p["cutoff1"])
    rng = np.random.default_rng(p["seed"])
    x += 0.15 * lowpass(rng.standard_normal(n), 1200)
    env = np.clip(u / 0.12, 0, 1) * (1 - u) ** 1.4
    return x * env


def r_splash(p):
    """A cartoon splash: a bright puff of noise falling in colour, then a few bubbles."""
    n = int(round(p["dur"] * SR))
    rng = np.random.default_rng(p["seed"])
    burst = sweep_lowpass(rng.standard_normal(n), 3500, 500) * decay(n, 0.09, 0.006)
    buf = np.zeros(n + int(0.1 * SR))
    place(buf, 1.4 * burst, 0)
    place(buf, 0.5 * osc(np.linspace(320, 180, int(0.12 * SR))) * decay(int(0.12 * SR), 0.04, 0.003), 0)
    for s in (0.12, 0.2, 0.27, 0.36):
        place(buf, 0.22 * bubble(rng.uniform(420, 700), 1.3), s)
    return buf


RECIPES = {"notes": r_notes, "blip": r_blip, "whoosh": r_whoosh, "tick": r_tick, "thump": r_thump, "bubbles": r_bubbles,
           "vroom": r_vroom, "splash": r_splash}


# ----------------------------------------------------------------------------------------------------------------------------------
# Loudness and finish
# ----------------------------------------------------------------------------------------------------------------------------------

def biquad(b, a):
    return np.array([[b[0] / a[0], b[1] / a[0], b[2] / a[0], 1.0, a[1] / a[0], a[2] / a[0]]])


def k_weighting():
    """ITU-R BS.1770 K-weighting at SR (high shelf + high-pass, RBJ forms with the standard constants), then the small speaker."""
    f0, gain, q = 1681.974450955533, 3.999843853973347, 0.7071752369554196
    A = 10 ** (gain / 40)
    w = 2 * np.pi * f0 / SR
    alpha = np.sin(w) / (2 * q)
    shelf = biquad([A * ((A + 1) + (A - 1) * np.cos(w) + 2 * np.sqrt(A) * alpha),
                    -2 * A * ((A - 1) + (A + 1) * np.cos(w)),
                    A * ((A + 1) + (A - 1) * np.cos(w) - 2 * np.sqrt(A) * alpha)],
                   [(A + 1) - (A - 1) * np.cos(w) + 2 * np.sqrt(A) * alpha,
                    2 * ((A - 1) - (A + 1) * np.cos(w)),
                    (A + 1) - (A - 1) * np.cos(w) - 2 * np.sqrt(A) * alpha])
    f0, q = 38.13547087602444, 0.5003270373238773
    w = 2 * np.pi * f0 / SR
    alpha = np.sin(w) / (2 * q)
    hp = biquad([(1 + np.cos(w)) / 2, -(1 + np.cos(w)), (1 + np.cos(w)) / 2], [1 + alpha, -2 * np.cos(w), 1 - alpha])
    speaker = signal.butter(2, SPEAKER_HZ, "high", fs=SR, output="sos")
    return np.vstack([shelf, hp, speaker])


K_SOS = k_weighting()


def loudness_db(x):
    """K-weighted loudness (through a phone speaker) of the loudest 100 ms window: short sounds are judged on their body."""
    y = signal.sosfilt(K_SOS, x)
    win = int(0.1 * SR)
    padded = np.concatenate([y, np.zeros(win)])
    power = np.convolve(padded ** 2, np.ones(win) / win, mode="valid")
    return -0.691 + 10 * np.log10(max(power.max(), 1e-12))


def finish(x, level_db=0.0):
    """Style and safety for every sound: low-pass, no DC, trimmed tail, fades, same loudness, peak limit."""
    x = np.asarray(x, dtype=np.float64)
    x = lowpass(x, LOWPASS_HZ)
    x = signal.sosfilt(signal.butter(2, HIGHPASS_HZ, "high", fs=SR, output="sos"), x)
    # trim the silent tail (below -60 dB of the peak), keep a little room
    peak = np.abs(x).max()
    loud = np.nonzero(np.abs(x) > peak * 1e-3)[0]
    x = x[:min(len(x), loud[-1] + int(0.02 * SR))]
    a, b = int(FADE_IN * SR), int(FADE_OUT * SR)
    x[:a] *= 0.5 - 0.5 * np.cos(np.pi * np.arange(a) / a)
    x[-b:] *= 0.5 + 0.5 * np.cos(np.pi * np.arange(b) / b)
    gain = 10 ** ((TARGET_LOUDNESS_DB + level_db - loudness_db(x)) / 20)
    x *= gain
    limited = np.abs(x).max() > PEAK_LIMIT
    if limited:
        x *= PEAK_LIMIT / np.abs(x).max()
    return x, limited


def main():
    os.makedirs(OUT, exist_ok=True)
    problems = []
    print(f"{'sound':<16}{'length':>8}{'peak dBFS':>11}{'loudness':>10}")
    for name, p in SOUNDS.items():
        x, limited = finish(RECIPES[p["kind"]](p), p.get("level_db", 0.0))
        pcm = np.round(np.clip(x, -1, 1) * 32767).astype(np.int16)
        path = os.path.join(OUT, name + ".wav")
        tmp = path + ".tmp"
        wavfile.write(tmp, SR, pcm)
        os.replace(tmp, path)
        length = len(pcm) / SR
        limit = MAX_JINGLE if name.startswith("jingle_") else MAX_EFFECT
        note_ = " (peak-limited: a bit quieter)" if limited else ""
        print(f"{name:<16}{length:>7.2f}s{20 * np.log10(np.abs(x).max()):>11.1f}{loudness_db(x):>10.1f}{note_}")
        if length > limit:
            problems.append(f"{name}: {length:.2f} s (max {limit} s)")
    if problems:
        print("PROBLEMS:")
        for pr in problems:
            print("  - " + pr)
        sys.exit(1)
    print(f"OK: {len(SOUNDS)} sounds in {os.path.relpath(OUT, ROOT)}")


if __name__ == "__main__":
    main()
