# Level 2 Skyline Zone - layout generator v2 (10 Nov, after user feedback)
# 34x34. Wide 2-cell sky bridges (pairs of rows/columns every 6 cells) between 4x4 skyscraper blocks.
# Some bridge segments are removed to make a maze, but every intersection keeps >= 2 exits: NO dead ends.
# 3 narrow 1-cell passages straight through blocks (shortcuts). Generator platform 4x4 in the centre.
# Legend as Level 1 + S = Sentinel (patrols through the nearest Nexus Core).
import random
from collections import deque
N=34
rng=random.Random(20261110)
L=[1,7,13,19,25,31]                 # first row/col of each 2-wide street
ST=set(); [ST.update((a,a+1)) for a in L]
g=[['#']*N for _ in range(N)]
for y in range(1,N-1):
    for x in range(1,N-1):
        if x in ST or y in ST: g[y][x]='.'
for y in range(15,19):
    for x in range(15,19): g[y][x]='.'   # central platform
G=(16,16); P=(16,32); H=(16,1)
D=((1,0),(-1,0),(0,1),(0,-1))
def walk(x,y): return 0<=x<N and 0<=y<N and g[y][x]!='#'
def bfs(s):
    d={s:0};q=deque([s])
    while q:
        c=q.popleft()
        for dx,dy in D:
            n=(c[0]+dx,c[1]+dy)
            if walk(*n) and n not in d: d[n]=d[c]+1;q.append(n)
    return d
def total(): return sum(walk(x,y) for y in range(N) for x in range(N))
# intersection graph: node (i,j) = 2x2 square at (L[i],L[j]); segments between neighbours
segs=[]
for j in range(6):
    for i in range(5):
        # horizontal segment on street row L[j], between columns L[i]+2 .. L[i+1]-1
        cells=[(x,y) for x in range(L[i]+2,L[i+1]) for y in (L[j],L[j]+1)]
        segs.append({'cells':cells,'a':(i,j),'b':(i+1,j),'open':True})
        cells=[(x,y) for y in range(L[i]+2,L[i+1]) for x in (L[j],L[j]+1)]
        segs.append({'cells':cells,'a':(j,i),'b':(j,i+1),'open':True})
def degree(n): return sum(1 for s in segs if s['open'] and (s['a']==n or s['b']==n))
def near(c,p,r): return abs(c[0]-p[0])<=r and abs(c[1]-p[1])<=r
def protected(s): return any(near(c,(16.5,16.5),5) or near(c,P,4) or near(c,H,3) for c in s['cells'])
rng.shuffle(segs)
closed=0
for s in segs:
    if closed>=16: break
    if protected(s): continue
    s['open']=False
    for x,y in s['cells']: g[y][x]='#'
    if degree(s['a'])>=2 and degree(s['b'])>=2 and len(bfs(P))==total():
        closed+=1; continue
    s['open']=True
    for x,y in s['cells']: g[y][x]='.'
# narrow shortcuts straight through a block (1 cell wide, open at both ends)
cuts=0
for (x,y,dx,dy) in [(10,3,0,1),(22,27,0,1),(3,22,1,0),(27,10,1,0),(10,27,0,1),(22,3,0,1)]:
    cells=[(x+dx*k,y+dy*k) for k in range(4)]
    a=(x-dx,y-dy); b=(x+dx*4,y+dy*4)
    if walk(*a) and walk(*b) and all(g[cy][cx]=='#' for cx,cy in cells) and cuts<3:
        for cx,cy in cells: g[cy][cx]='.'
        cuts+=1
ds=bfs(P); cells=list(ds)
assert len(cells)==total()
def nb(c): return sum(walk(c[0]+dx,c[1]+dy) for dx,dy in D)
assert not [c for c in cells if nb(c)<=1], "dead end cell"
# Nexus Cores: spread out over the bridges (not on the platform / start / hunter)
cand=[c for c in cells if ds[c]>=8 and not near(c,(16.5,16.5),3) and not near(c,H,2)]
cores=[]
for t in [(2,2),(31,2),(2,31),(31,31)]:
    cores.append(min(cand,key=lambda c:abs(c[0]-t[0])+abs(c[1]-t[1])))
while len(cores)<10:
    cores.append(max((c for c in cand if c not in cores),key=lambda c:min(abs(c[0]-o[0])+abs(c[1]-o[1]) for o in cores+[P,G,H])))
for x,y in cores: g[y][x]='C'
g[G[1]][G[0]]='G'; g[P[1]][P[0]]='P'; g[H[1]][H[0]]='H'
# 3 Sentinels: the 3 cores farthest from the start, standing 3 cells away
sent=[]
for c in sorted(cores,key=lambda c:-ds[c])[:3]:
    dc=bfs(c)
    s=max((n for n,d in dc.items() if d==3 and g[n[1]][n[0]]=='.'),key=lambda n:ds[n])
    g[s[1]][s[0]]='S'; sent.append(s)
# Patrollers: row 7 and column 25 (route ends on plain bridge cells)
def ends_row(y): xs=[x for x in range(N) if g[y][x]=='.']; return (xs[0],y),(xs[-1],y)
def ends_col(x): ys=[y for y in range(N) if g[y][x]=='.']; return (x,ys[0]),(x,ys[-1])
# (v2.1: only one Patroller - the second one made the level too hard in user testing)
for d,(a,b) in (('1',ends_row(7)),):
    for x,y in (a,b): g[y][x]=d
txt='\n'.join(''.join(r) for r in g)+'\n'
open('Level2_SkylineZone.txt','w').write(txt)
print(txt); print("closed",closed,"cuts",cuts,"cores",cores,"sentinels",sent,"walkable",total())
