# Level 1 Neon District - city grid (Barcelona Eixample style), version 2 (10 Nov)
# Changes from v1: no chamfer pockets (they made pre-pressed turns go into 1-cell nooks),
# Blockers stand 5 cells from their core on the approach from the Generator (the other side stays open).
N=33
g=[['#']*N for _ in range(N)]
streets=[1+5*i for i in range(7)]          # 1,6,...,31
for y in range(1,N-1):
    for x in range(1,N-1):
        if x in streets or y in streets: g[y][x]='.'
# Gran Via (row 12) and Passeig de Gracia (column 22): extra avenues
for x in range(1,N-1): g[12][x]='.'
for y in range(1,N-1): g[y][22]='.'
# Avinguda Diagonal: 2-wide staircase from bottom-left to top-right
for i in range(1,N-2):
    y=N-1-i
    if 1<=y<=N-2:
        g[y][i]='.'; g[y][i+1]='.'
# Central plaza with the Generator
for y in range(17,21):
    for x in range(17,21): g[y][x]='.'
def walk(x,y): return 0<=x<N and 0<=y<N and g[y][x]!='#'
G=(18,18); P=(16,31); H=(16,1)
from collections import deque
def bfs(s):
    d={s:0};q=deque([s])
    while q:
        x,y=q.popleft()
        for dx,dy in((1,0),(-1,0),(0,1),(0,-1)):
            n=(x+dx,y+dy)
            if walk(*n) and n not in d: d[n]=d[(x,y)]+1;q.append(n)
    return d
ds=bfs(P)
cells=list(ds)
assert len(cells)==sum(walk(x,y) for y in range(N) for x in range(N)), "not connected"
def nb(c): return sum(walk(c[0]+dx,c[1]+dy) for dx,dy in((1,0),(-1,0),(0,1),(0,-1)))
dead=[c for c in cells if nb(c)<=1]
print("dead ends:",dead)
onstreet=lambda c: c[0] in streets or c[1] in streets or c[1]==12 or c[0]==22
cand=[c for c in cells if onstreet(c) and ds[c]>=8 and not (16<=c[0]<=21 and 16<=c[1]<=21) and c!=H]
cores=[]
for target in [(1,1),(31,1),(1,31),(31,31)]:
    cores.append(min(cand,key=lambda c:abs(c[0]-target[0])+abs(c[1]-target[1])))
while len(cores)<10:
    cores.append(max((c for c in cand if c not in cores),key=lambda c:min(abs(c[0]-o[0])+abs(c[1]-o[1]) for o in cores+[P,G])))
for x,y in cores: g[y][x]='C'
g[G[1]][G[0]]='G'; g[P[1]][P[0]]='P'; g[H[1]][H[0]]='H'
# Blockers: guard the two cores farthest from the start, 5 cells away on the route towards the Generator
dg=bfs(G)
far=sorted(cores,key=lambda c:-ds[c])[:2]
blk=[]
for c in far:
    dc=bfs(c)
    opts=[n for n,d in dc.items() if d==5 and g[n[1]][n[0]]=='.']
    b=min(opts,key=lambda n:dg[n])
    g[b[1]][b[0]]='B'; blk.append(b)
g[12][1]='1'; g[12][31]='1'
g[1][22]='2'; g[27][22]='2'
txt='\n'.join(''.join(r) for r in g)+'\n'
open('Level1_NeonDistrict.txt','w').write(txt)
print(txt)
print("cores",cores,"blockers",blk,"far",far)
# pockets: walkable cells off the street grid with a wall straight ahead in both axes (1-cell nooks)
print("cells off main streets:",[c for c in cells if not onstreet(c) and not (17<=c[0]<=20 and 17<=c[1]<=20)][:60])
