"""Turn the four pieces of music of Resolver Master into seamless loops (ErNa-015).

Input:  ArtSource/Music/*.wav (or ArtSource/Audio/Music/), recognised by name: "home", "plumber", "tj"/"traffic", "pino"/"penguin".
        The originals are only read, never changed or moved.
Output: Assets/Resources/Audio/Music/Music_Home|Music_Plumber|Music_TrafficJam|Music_Penguin.ogg
        (Ogg Vorbis, quality 6 ~ 160 kbps, stereo, 44.1 kHz). The game loads them by name (AudioCatalog.MusicFor).

For every piece:
  1. analysis: length, silence at the two ends, a fade-out at the end, tempo (BPM) and beat grid (onset envelope + comb search),
     the bar phase (low-frequency onsets);
  2. the loop: start S and end E on the same beat of the bar, E - S a whole number of bars, chosen so that what follows E sounds
     like what follows S (chroma + band spectrum + level around the cut); the longest loop among the good ones (never inside the
     silence or the fade-out); E is refined to the sample by cross-correlation with S (the crossfade adds two waves in phase);
  3. the seam: a short crossfade (CROSSFADE s) ending exactly at E, from the end of the piece into the music just before S, so the
     last sample of the loop leads into the first one without a click or a jump in level;
  4. the same perceived loudness for all the pieces (ITU-R BS.1770 integrated, gated, TARGET_LUFS), true peak under PEAK_DBTP;
  5. Ogg Vorbis through ffmpeg (libvorbis), then a check of the decoded file: length, seam, loudness.

It also prints the loudness of the music as a phone speaker plays it, measured like the effects of make_sfx.py, and the music
volume (AudioConfig.musicVolume) that keeps the music MUSIC_BELOW_EFFECTS_DB under the effects.

Deterministic: the same originals give the same files. Usage (from the project root):  python Tools/Audio/make_music_loops.py
Needs numpy, scipy and ffmpeg on the PATH (with libvorbis).
"""
import os
import re
import shutil
import subprocess
import sys
import tempfile
from math import gcd

import numpy as np
from scipy import signal
from scipy.io import wavfile

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SOURCES = [os.path.join(ROOT, "ArtSource", "Music"), os.path.join(ROOT, "ArtSource", "Audio", "Music")]
OUT = os.path.join(ROOT, "Assets", "Resources", "Audio", "Music")

# final name -> words that recognise the original file (lower case, compared with the words of the file name)
TRACKS = [
    ("Music_Home", ["home", "menu"]),
    ("Music_Plumber", ["plumber", "idraulico", "pipes"]),
    ("Music_TrafficJam", ["tj", "traffic", "trafficjam"]),
    ("Music_Penguin", ["pino", "penguin"]),
]

SR = 44100                    # output sample rate
SILENCE_DB = -50.0            # below this (dBFS, peak of 10 ms) the ends are silence
FADE_DROP_DB = 4.0            # the end is a fade-out where the level stays this much under the body of the piece
BPM_RANGE = (70.0, 180.0)
BEATS_PER_BAR = 4
START_SEARCH_S = 24.0         # the loop may start this far into the piece at most (skip a soft intro if it does not fit)
SCORE_TOLERANCE = 0.06        # loops this close to the best seam are as good: the longest of them wins (2-3 minutes when the
MIN_SCORE = 0.90              # piece is long enough, never a seam scoring under MIN_SCORE when a better one exists)
CROSSFADE = 0.12              # seconds of crossfade at the seam
ALIGN_MS = 12.0               # E is moved by at most this much to line its wave up with S
TARGET_LUFS = -18.0           # the same loudness for every piece
PEAK_DBTP = -1.5              # true peak (4x oversampled) never above this
VORBIS_QUALITY = 6            # ~160 kbps stereo
SPEAKER_HZ = 300              # as make_sfx.py: the phone speaker gives little below this
EFFECTS_LOUDNESS_DB = -19.0   # make_sfx.py TARGET_LOUDNESS_DB (loudest 100 ms of an effect, mono, through the speaker)
MUSIC_BELOW_EFFECTS_DB = 11.0


# ----------------------------------------------------------------------------------------------------------------------------------
# Files

def words(name):
    return [w for w in re.split(r"[^a-z0-9]+", name.lower()) if w]


def find_sources():
    """The original of every track: the first file of the source folders whose name has one of its words."""
    files = []
    for d in SOURCES:
        if os.path.isdir(d):
            files += [os.path.join(d, f) for f in sorted(os.listdir(d)) if f.lower().endswith(".wav")]
    found = {}
    for key, keys in TRACKS:
        for f in files:
            stem = os.path.splitext(os.path.basename(f))[0]
            w = words(stem)
            joined = "".join(w)
            if any(k in w or (len(k) >= 4 and k in joined) for k in keys):
                found[key] = f
                break
    return found


def read_wav(path):
    sr, x = wavfile.read(path)
    if x.dtype == np.int16:
        x = x.astype(np.float64) / 32768.0
    elif x.dtype == np.int32:
        x = x.astype(np.float64) / 2147483648.0
    elif x.dtype == np.uint8:
        x = (x.astype(np.float64) - 128.0) / 128.0
    else:
        x = x.astype(np.float64)
    if x.ndim == 1:
        x = np.stack([x, x], axis=1)
    x = x[:, :2]
    if sr != SR:
        g = gcd(SR, sr)
        x = signal.resample_poly(x, SR // g, sr // g, axis=0)
    return x


# ----------------------------------------------------------------------------------------------------------------------------------
# Loudness (ITU-R BS.1770)

def biquad(b, a):
    return np.array([[b[0] / a[0], b[1] / a[0], b[2] / a[0], 1.0, a[1] / a[0], a[2] / a[0]]])


def k_weighting(sr, speaker=False):
    f0, gain, q = 1681.974450955533, 3.999843853973347, 0.7071752369554196
    A = 10 ** (gain / 40)
    w = 2 * np.pi * f0 / sr
    alpha = np.sin(w) / (2 * q)
    shelf = biquad([A * ((A + 1) + (A - 1) * np.cos(w) + 2 * np.sqrt(A) * alpha),
                    -2 * A * ((A - 1) + (A + 1) * np.cos(w)),
                    A * ((A + 1) + (A - 1) * np.cos(w) - 2 * np.sqrt(A) * alpha)],
                   [(A + 1) - (A - 1) * np.cos(w) + 2 * np.sqrt(A) * alpha,
                    2 * ((A - 1) - (A + 1) * np.cos(w)),
                    (A + 1) - (A - 1) * np.cos(w) - 2 * np.sqrt(A) * alpha])
    f0, q = 38.13547087602444, 0.5003270373238773
    w = 2 * np.pi * f0 / sr
    alpha = np.sin(w) / (2 * q)
    hp = biquad([(1 + np.cos(w)) / 2, -(1 + np.cos(w)), (1 + np.cos(w)) / 2], [1 + alpha, -2 * np.cos(w), 1 - alpha])
    sos = [shelf, hp]
    if speaker:
        sos.append(signal.butter(2, SPEAKER_HZ, "high", fs=sr, output="sos"))
    return np.vstack(sos)


def integrated_lufs(x, speaker=False, mono_equivalent=False):
    """Integrated, gated loudness (400 ms blocks, 75 % overlap, -70 LUFS and -10 LU gates). mono_equivalent: the mean of the two
    channels instead of their sum (how make_sfx.py measures a mono effect, which plays the same on both channels)."""
    y = signal.sosfilt(k_weighting(SR, speaker), x, axis=0)
    block, hop = int(0.4 * SR), int(0.1 * SR)
    sq = np.cumsum(np.concatenate([np.zeros((1, y.shape[1])), y ** 2]), axis=0)
    starts = np.arange(0, len(y) - block + 1, hop)
    ms = (sq[starts + block] - sq[starts]) / block
    power = ms.mean(axis=1) if mono_equivalent else ms.sum(axis=1)
    loud = -0.691 + 10 * np.log10(np.maximum(power, 1e-12))
    gated = power[loud > -70]
    rel = -0.691 + 10 * np.log10(gated.mean()) - 10
    gated = power[(loud > -70) & (loud > rel)]
    return -0.691 + 10 * np.log10(gated.mean())


def true_peak_db(x):
    up = signal.resample_poly(x, 4, 1, axis=0)
    return 20 * np.log10(max(np.abs(up).max(), 1e-12))


# ----------------------------------------------------------------------------------------------------------------------------------
# Analysis

AN_SR = 22050
N_FFT = 2048
HOP = 256
FPS = AN_SR / HOP


def frame_db(m, sr, seconds):
    w = int(seconds * sr)
    n = len(m) // w
    r = np.sqrt((m[:n * w].reshape(n, w) ** 2).mean(axis=1))
    return 20 * np.log10(r + 1e-9)


def trim_ends(x):
    """First and last sample above SILENCE_DB (peak of 10 ms blocks)."""
    a = np.abs(x).max(axis=1)
    w = int(0.01 * SR)
    n = len(a) // w
    peaks = a[:n * w].reshape(n, w).max(axis=1)
    loud = np.nonzero(peaks > 10 ** (SILENCE_DB / 20))[0]
    return loud[0] * w, min(len(x), (loud[-1] + 1) * w)


def fade_out_start(m, start, end):
    """Where a fade-out (or a soft ending) begins: the last 0.5 s block (1 s smoothed) still near the body level of the piece."""
    db = frame_db(m[start:end], SR, 0.5)
    smooth = np.convolve(db, np.ones(2) / 2, mode="same")
    body = np.median(db)
    near = np.nonzero(smooth >= body - FADE_DROP_DB)[0]
    last = near[-1] + 1 if len(near) else len(db)
    return start + min(len(m) - start, last * int(0.5 * SR))


def spectrogram(m):
    y = signal.resample_poly(m, AN_SR, SR)
    _, _, z = signal.stft(y, fs=AN_SR, nperseg=N_FFT, noverlap=N_FFT - HOP, boundary=None, padded=False)
    return np.abs(z)   # (bins, frames); frame i is centred at (i * HOP + N_FFT / 2) / AN_SR


def frame_time(i):
    return (np.asarray(i) * HOP + N_FFT / 2) / AN_SR


def onset_envelope(mag, low_only=False):
    freqs = np.fft.rfftfreq(N_FFT, 1 / AN_SR)
    band = (freqs >= 30) & (freqs <= (200 if low_only else 8000))
    s = np.log1p(100 * mag[band])
    flux = np.maximum(0, np.diff(s, axis=1)).sum(axis=0)
    flux = np.concatenate([[0], flux])
    flux -= np.convolve(flux, np.ones(int(FPS)) / int(FPS), mode="same")   # remove the slow level changes
    return np.maximum(flux, 0)


def tempo(env):
    """BPM and the time of one beat: autocorrelation for the period, then a fine comb search for period and phase."""
    e = env - env.mean()
    ac = np.correlate(e, e, mode="full")[len(e) - 1:]
    lags = np.arange(len(ac))
    bpm_of = lambda lag: 60 * FPS / np.maximum(lag, 1e-9)
    ok = (bpm_of(lags) >= BPM_RANGE[0]) & (bpm_of(lags) <= BPM_RANGE[1])
    weight = np.exp(-0.5 * (np.log2(bpm_of(lags) / 120.0) / 1.0) ** 2)   # prefer the usual tempos (as librosa)
    score = np.where(ok, ac * weight, -np.inf)
    lag = int(np.argmax(score))
    best = (-np.inf, None, None)
    t = np.arange(len(env)) / FPS
    for bpm in np.arange(bpm_of(lag) * 0.97, bpm_of(lag) * 1.03, 0.01):
        period = 60.0 / bpm
        for phase in np.arange(0, period, 1 / FPS / 2):
            beats = np.arange(phase, t[-1], period)
            v = np.interp(beats, frame_time(np.arange(len(env))), env).sum() / len(beats)
            if v > best[0]:
                best = (v, bpm, phase)
    return best[1], best[2]


def chroma_and_bands(mag):
    freqs = np.fft.rfftfreq(N_FFT, 1 / AN_SR)
    ok = (freqs >= 60) & (freqs <= 5000)
    pitch = np.round(12 * np.log2(freqs[ok] / 440.0)).astype(int) % 12
    p = mag[ok] ** 2
    chroma = np.zeros((12, mag.shape[1]))
    for c in range(12):
        chroma[c] = p[pitch == c].sum(axis=0)
    edges = np.geomspace(40, 10000, 17)
    bands = np.zeros((16, mag.shape[1]))
    for b in range(16):
        sel = (freqs >= edges[b]) & (freqs < edges[b + 1])
        bands[b] = (mag[sel] ** 2).sum(axis=0)
    return chroma, np.log10(bands + 1e-10)


def window_feature(feat, t0, t1):
    i0, i1 = int(max(0, t0) * FPS), int(t1 * FPS)
    i1 = max(i0 + 1, min(i1, feat.shape[1]))
    return feat[:, i0:i1].mean(axis=1)


def cosine(a, b):
    return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-12))


# ----------------------------------------------------------------------------------------------------------------------------------
# The loop

def find_loop(x, start, usable_end, bpm, phase):
    m = x.mean(axis=1)
    mag = spectrogram(m)
    chroma, bands = chroma_and_bands(mag)
    period = 60.0 / bpm
    bar = period * BEATS_PER_BAR
    low = onset_envelope(mag, low_only=True)
    # the bar phase: the beat of the bar with the strongest low onsets (the kick of the downbeat)
    beats = np.arange(phase, len(m) / SR, period)
    strength = np.interp(beats, frame_time(np.arange(len(low))), low)
    downbeat = int(np.argmax([strength[k::BEATS_PER_BAR].mean() for k in range(BEATS_PER_BAR)]))
    downbeats = beats[downbeat::BEATS_PER_BAR]

    t_start, t_end = start / SR, usable_end / SR
    starts = downbeats[(downbeats >= t_start - 1e-6) & (downbeats <= t_start + START_SEARCH_S)]
    if len(starts) == 0 or downbeats[0] > t_start:
        starts = np.concatenate([[downbeats[downbeats >= t_start][0]], starts]) if len(starts) == 0 else starts
    level = frame_db(m, SR, 0.05)

    def lvl(t0, t1):
        i0, i1 = int(t0 / 0.05), max(int(t0 / 0.05) + 1, int(t1 / 0.05))
        return 10 * np.log10(np.mean(10 ** (level[i0:i1] / 10)))

    candidates = []
    total = len(m) / SR
    for s in starts:
        for e in downbeats[downbeats > s + 4 * bar]:
            # the loop never plays what follows E (the crossfade uses the music before S): near the end of the piece the
            # comparison of "what follows" is only as long as there is music left, at least half a bar
            w = min(2 * bar, total - e)
            if e > t_end or w < 0.5 * bar:
                break
            after = 0.5 * cosine(window_feature(chroma, s, s + w), window_feature(chroma, e, e + w)) + \
                0.5 * cosine(window_feature(bands, s, s + w), window_feature(bands, e, e + w))
            before = after
            if s - bar >= 0:
                before = 0.5 * cosine(window_feature(chroma, s - bar, s), window_feature(chroma, e - bar, e)) + \
                    0.5 * cosine(window_feature(bands, s - bar, s), window_feature(bands, e - bar, e))
            # what the listener hears at the seam: the bar before E, then the bar after S
            jump = abs(lvl(e - bar, e) - lvl(s, s + bar))
            score = 0.65 * after + 0.35 * before - 0.02 * max(0.0, jump - 1.0)
            candidates.append((score, s, e))
    if not candidates:
        raise RuntimeError("no loop of at least 4 bars fits")
    best = max(c[0] for c in candidates)
    pool = [c for c in candidates if c[0] >= max(MIN_SCORE, best - SCORE_TOLERANCE)] or [max(candidates)]
    score, s, e = max(pool, key=lambda c: (c[2] - c[1], c[0]))
    bars = int(round((e - s) / bar))

    # how sudden the seam is (the bar before E, then the bar after S) against the ordinary bar changes of the piece
    def change(t_before, t_after):
        return 1 - 0.5 * (cosine(window_feature(chroma, t_before - bar, t_before), window_feature(chroma, t_after, t_after + bar)) +
                          cosine(window_feature(bands, t_before - bar, t_before), window_feature(bands, t_after, t_after + bar)))
    natural = [change(d, d) for d in downbeats if d - bar >= t_start and d + bar <= t_end]
    seam_change = change(e, s)
    percentile = 100.0 * np.mean(np.array(natural) < seam_change) if natural else 0.0

    # to the sample: S on the grid (its wave untouched), E moved by at most ALIGN_MS so that the two waves the crossfade adds
    # (before E and before S, see make_loop; after them when S is at the very start) are in phase
    S = int(round(s * SR))
    E0 = int(round(e * SR))
    X = int(CROSSFADE * SR)
    before_s = S >= X
    ref = m[S - X:S] if before_s else m[S:S + X]
    r = int(ALIGN_MS / 1000 * SR)
    best_c, E = -np.inf, E0
    for d in range(-r, r + 1):
        seg = m[E0 + d - X:E0 + d] if before_s else m[E0 + d:E0 + d + X]
        if len(seg) < X:
            continue
        c = np.dot(ref, seg) / (np.linalg.norm(ref) * np.linalg.norm(seg) + 1e-12)
        if c > best_c:
            best_c, E = c, E0 + d
    return dict(S=S, E=E, bars=bars, score=score, best=best, align=(E - E0) / SR * 1000, corr=best_c, downbeat=downbeat,
                candidates=candidates, bar=bar, percentile=percentile)


def make_loop(x, S, E):
    """x[S:E] with a crossfade that ends at E: the end of the piece fades into the music just before S (or, when there is not
    enough before S, the start fades in from the music just after E). Linear when the two waves are in phase, equal power when not."""
    X = int(CROSSFADE * SR)
    out = x[S:E].copy()
    t = (np.arange(X) + 0.5) / X
    if S >= X:
        a, b = x[E - X:E], x[S - X:S]           # a: end of the loop, b: what leads into S
        corr = np.sum(a * b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-12)
        fin, fout = (t, 1 - t) if corr > 0.5 else (np.sin(t * np.pi / 2), np.cos(t * np.pi / 2))
        out[-X:] = a * fout[:, None] + b * fin[:, None]
        mode = "end"
    else:
        a, b = x[S:S + X], x[E:E + X]           # a: start of the loop, b: what follows E
        corr = np.sum(a * b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-12)
        fin, fout = (t, 1 - t) if corr > 0.5 else (np.sin(t * np.pi / 2), np.cos(t * np.pi / 2))
        out[:X] = b * fout[:, None] + a * fin[:, None]
        mode = "start"
    return out, mode, float(corr)


def seam_jump(y):
    """The step from the last sample to the first, against the usual step between two samples (1 = an ordinary step)."""
    d = np.abs(np.diff(y, axis=0)).max(axis=1)
    usual = np.percentile(d, 99)
    return float(np.abs(y[0] - y[-1]).max() / (usual + 1e-12))


# ----------------------------------------------------------------------------------------------------------------------------------
# Encoding

def ffmpeg():
    exe = shutil.which("ffmpeg")
    if exe is None:
        sys.exit("ffmpeg not found on the PATH (it encodes the Ogg Vorbis files)")
    return exe


def encode(y, path, tmp):
    wav = os.path.join(tmp, "loop.wav")
    wavfile.write(wav, SR, np.clip(y, -1, 1).astype(np.float32))
    subprocess.run([ffmpeg(), "-y", "-v", "error", "-i", wav, "-map_metadata", "-1", "-fflags", "+bitexact", "-c:a", "libvorbis",
                    "-q:a", str(VORBIS_QUALITY), "-ar", str(SR), "-ac", "2", path], check=True)


def decode(path, tmp):
    wav = os.path.join(tmp, "check.wav")
    subprocess.run([ffmpeg(), "-y", "-v", "error", "-i", path, "-c:a", "pcm_f32le", wav], check=True)
    return read_wav(wav)


def fmt_time(s):
    return f"{int(s // 60)}:{s % 60:05.2f}"


def main():
    found = find_sources()
    missing = [k for k, _ in TRACKS if k not in found]
    if missing:
        sys.exit(f"originals not found for {', '.join(missing)} (looked in {', '.join(SOURCES)})")
    os.makedirs(OUT, exist_ok=True)
    loops = {}
    for key, _ in TRACKS:
        src = found[key]
        x = read_wav(src)
        a, b = trim_ends(x)
        m = x.mean(axis=1)
        fade = fade_out_start(m, a, b)
        mag = spectrogram(m)
        bpm, phase = tempo(onset_envelope(mag))
        loop = find_loop(x, a, fade, bpm, phase)
        y, mode, corr = make_loop(x, loop["S"], loop["E"])
        loops[key] = dict(src=src, x_len=len(x) / SR, trim=(a / SR, b / SR), fade=fade / SR, bpm=bpm, y=y, mode=mode, xcorr=corr,
                          **loop)

    # the same loudness for all; a common lower target if one of them would peak too high
    target = TARGET_LUFS
    for v in loops.values():
        v["lufs_in"] = integrated_lufs(v["y"])
        v["tp_in"] = true_peak_db(v["y"])
        target = min(target, PEAK_DBTP - (v["tp_in"] - v["lufs_in"]))

    speaker_levels = []
    with tempfile.TemporaryDirectory() as tmp:
        for key, v in loops.items():
            y = v["y"] * 10 ** ((target - v["lufs_in"]) / 20)
            path = os.path.join(OUT, key + ".ogg")
            encode(y, path, tmp)
            d = decode(path, tmp)
            v.update(out=path, size=os.path.getsize(path), samples_ok=len(d) == len(y), dec_len=len(d), lufs=integrated_lufs(d),
                     tp=true_peak_db(d), seam=seam_jump(d), seam_src=seam_jump(y))
            speaker_levels.append(integrated_lufs(d, speaker=True, mono_equivalent=True))

    print(f"Target loudness {target:.1f} LUFS (asked {TARGET_LUFS}), true peak <= {PEAK_DBTP} dBTP, Vorbis q{VORBIS_QUALITY}\n")
    for key, v in loops.items():
        S, E = v["S"] / SR, v["E"] / SR
        print(f"{key}  <- {os.path.relpath(v['src'], ROOT)}")
        print(f"  original {fmt_time(v['x_len'])}, sound {fmt_time(v['trim'][0])}-{fmt_time(v['trim'][1])}, "
              f"fade-out/soft end from {fmt_time(v['fade'])}")
        print(f"  tempo {v['bpm']:.2f} BPM, downbeat = beat {v['downbeat'] + 1} of the grid")
        print(f"  loop {fmt_time(S)} -> {fmt_time(E)}  = {fmt_time(E - S)}, {v['bars']} bars; seam score {v['score']:.3f} "
              f"(best {v['best']:.3f}), E aligned {v['align']:+.1f} ms (wave corr {v['corr']:.2f})")
        print(f"  the seam changes more than {v['percentile']:.0f} % of the ordinary bar changes of the piece")
        print(f"  crossfade {CROSSFADE * 1000:.0f} ms at the {v['mode']} (corr {v['xcorr']:.2f}); seam step {v['seam']:.2f}x a usual "
              f"step (before encoding {v['seam_src']:.2f}x)")
        print(f"  {os.path.relpath(v['out'], ROOT)}: {v['size'] / 1024:.0f} KB, {v['lufs']:.1f} LUFS, true peak {v['tp']:.1f} dBTP, "
              f"length {'exact' if v['samples_ok'] else 'CHANGED (%d samples)' % v['dec_len']}\n")

    level = float(np.mean(speaker_levels))
    volume = 10 ** ((EFFECTS_LOUDNESS_DB - MUSIC_BELOW_EFFECTS_DB - level) / 20)
    print(f"Music through a phone speaker (as the effects are measured): {level:.1f} dB at volume 1 "
          f"({', '.join(f'{v:.1f}' for v in speaker_levels)}).")
    print(f"Effects: {EFFECTS_LOUDNESS_DB:.1f} dB. For the music {MUSIC_BELOW_EFFECTS_DB:.0f} dB under the effects: "
          f"AudioConfig.musicVolume = {volume:.2f}")
    bad = [k for k, v in loops.items() if not v["samples_ok"] or v["seam"] > 3.0]
    if bad:
        sys.exit(f"check failed for {', '.join(bad)} (length changed or a step at the seam)")


if __name__ == "__main__":
    main()
