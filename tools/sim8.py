# -*- coding: utf-8 -*-
# 사건 시스템 v2 시뮬레이션 — C# PickEventCommand 와 동일 규칙
import random, sys, os, re, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from evdefs import EVENTS, EXISTING

DAY_CHANCE, NIGHT_CHANCE, DRY_BOOST = 65, 40, 20

def cond_ok(cond, st):
    if not cond: return True
    for tok in cond.split('&'):
        m = re.match(r'^(!?)([A-Za-z_][A-Za-z0-9_]*)(?:(>=|<=|==|>|<)(-?\d+))?$', tok)
        neg, name, op, num = m.groups()
        v = st.get(name, 0)
        if op is None: ok = v > 0
        else:
            n = int(num)
            ok = {'>=': v >= n, '<=': v <= n, '==': v == n, '>': v > n, '<': v < n}[op]
        if neg: ok = not ok
        if not ok: return False
    return True

def pick(st, slot, chance):
    day = st['day']
    dry = st.get('evdry', 0)
    eff = min(100, chance + DRY_BOOST * dry)
    cands = []
    for (eid, s, mn, mx, cond, after, gap, w) in EVENTS:
        if st.get('seen_' + eid, 0) > 0: continue
        if day < mn or (mx > 0 and day > mx): continue
        if s != 'a' and s != slot: continue
        if not cond_ok(cond, st): continue
        if after:
            if st.get('seen_' + after, 0) <= 0: continue
            if day - st.get('seenday_' + after, 0) < gap: continue
        cands.append((eid, w))
    st['cand_' + slot] = len(cands)
    if not cands: return None
    if random.randint(1, 100) > eff:
        st['evdry'] = dry + 1
        return None
    tot = sum(w for _, w in cands); r = random.uniform(0, tot)
    for eid, w in cands:
        r -= w
        if r <= 0: break
    st['seen_' + eid] = 1; st['seenday_' + eid] = day; st['evdry'] = 0
    return eid

def life(maxday=60):
    st = {'day': 1}
    yong = random.random()
    log = []
    nocand_day = 0
    for day in range(1, maxday + 1):
        st['day'] = day
        # 서사 플래그 타임라인 (근사)
        if day >= 3: st['f_soamet'] = 1
        if day >= 8: st['f_wanted'] = 1
        if day >= 15: st['rank'] = max(900, int(500000 * (0.78 ** (day - 15))))
        if day >= 12 and st.get('f_yongspared', 0): st['b_yong2'] = 1
        if day >= 27 and random.random() < 0.5: st['f_hmsaved'] = 1
        if day >= 11 and random.random() < 0.05: st['f_dokad'] = 1
        st['curse'] = min(100, 5 + 1.1 * day + random.uniform(-6, 6))
        st['sl'] = 0.3 * day + random.uniform(-2, 2)
        e1 = pick(st, 'd', DAY_CHANCE)
        if st.get('cand_d', 0) == 0: nocand_day += 1
        if e1 == 'E_Yong':
            if yong < 0.5: st['f_yongspared'] = 1
            elif yong < 0.75: st['f_yongkilled'] = 1
        e2 = pick(st, 'n', NIGHT_CHANCE)
        log.append((day, e1, e2))
    return st, log, nocand_day

N = 3000
random.seed(7)
tot_ev = collections.Counter(); phase = collections.Counter(); streaks = []; nocand = 0
seen_count = collections.Counter(); events_per_life = []; dup = 0
for _ in range(N):
    st, log, nc = life()
    nocand += nc
    ids = [e for d, a, b in log for e in (a, b) if e]
    if len(ids) != len(set(ids)): dup += 1
    events_per_life.append(len(ids))
    for e in ids: seen_count[e] += 1
    for d, a, b in log:
        n = (1 if a else 0) + (1 if b else 0)
        phase[(d - 1) // 10] += n
    # 빈 날 연속
    cur = 0; mx = 0
    for d, a, b in log:
        if a or b: cur = 0
        else: cur += 1; mx = max(mx, cur)
    streaks.append(mx)

print('lives=%d  dup-lives=%d  avg events/60d=%.1f  min=%d max=%d' % (N, dup, sum(events_per_life) / N, min(events_per_life), max(events_per_life)))
print('events per 10-day phase (avg per life):', ['%d-%d: %.1f' % (p * 10 + 1, p * 10 + 10, phase[p] / N) for p in range(6)])
sc = collections.Counter(streaks)
print('max empty-day streak distribution:', sorted(sc.items()))
print('day-slot calls with zero candidates per life: %.2f' % (nocand / N))
print('\n-- reach rate (%% of lives) --')
for (eid, s, mn, mx, cond, after, gap, w) in EVENTS:
    print('%-16s %5.1f%%  %s' % (eid, 100.0 * seen_count[eid] / N, '' if eid in EXISTING else 'new'))
