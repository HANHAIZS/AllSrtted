#!/usr/bin/env python3
"""Compile only the first five local2165 stages. Compatible generation, not original RNG.
Run from any directory; validates legal solutions before atomically publishing data.
No source archive, native scene/prefab or player save is modified.
"""
from pathlib import Path
import json,random,hashlib,collections,copy,time
ROOT=Path(__file__).resolve().parents[1];SRC=ROOT/'Assets/Art/DesignImports/SortExpress/Levels';OUT=ROOT/'TestResults';OUT.mkdir(exist_ok=True)
raws=[json.loads(next(SRC.glob('Level%05d_*.json'%n)).read_text()) for n in range(1,6)]
def ids(v):
 if isinstance(v,dict):
  if v.get('iid'):yield v['iid']
  for x in v.values():yield from ids(x)
 elif isinstance(v,list):
  for x in v:yield from ids(x)
IIDS=sorted(set(x for d in raws for x in ids(d)));KINDS={x:100+i for i,x in enumerate(IIDS)}
def goods(st):return sum(c>=0 for sh in st for row in sh for c in row)
def front(sh):return sh[0] if sh else (-1,-1,-1)
def settle(st,locks,initial):
 st=[[list(r) for r in sh] for sh in st]
 while True:
  changed=False;matches=(initial-goods(st))//3
  for i,sh in enumerate(st):
   if locks[i]>matches:continue
   while sh and all(c<0 for c in sh[0]):sh.pop(0);changed=True
   if sh and sh[0][0]>=0 and len(set(sh[0]))==1:sh[0]=[-1]*3;changed=True
  if not changed:break
 return tuple(tuple(tuple(r) for r in sh) for sh in st)
def move(st,locks,initial,a,b,c,d):
 matches=(initial-goods(st))//3
 if locks[a]>matches or locks[c]>matches or front(st[a])[b]<0 or front(st[c])[d]>=0 or (a,b)==(c,d):return None
 v=[[list(r) for r in sh] for sh in st]
 if not v[c]:v[c].append([-1]*3)
 v[c][0][d]=v[a][0][b];v[a][0][b]=-1
 return settle(v,locks,initial)
def macros(st,locks,initial):
 matches=(initial-goods(st))//3;unlocked=[i for i in range(len(st)) if locks[i]<=matches];groups=collections.defaultdict(list)
 for s in unlocked:
  for c,k in enumerate(front(st[s])):
   if k>=0:groups[k].append((s,c))
 for kind,positions in groups.items():
  if len(positions)<3:continue
  for target in sorted(set(s for s,c in positions),key=lambda s:-front(st[s]).count(kind)):
   v=st;path=[]
   for iteration in range(8):
    if goods(v)<goods(st):break
    empties=[z for z,x in enumerate(front(v[target])) if x<0]
    sources=[(s,z) for s in unlocked if s!=target for z,x in enumerate(front(v[s])) if x==kind]
    if empties and sources:step=(*sources[0],target,empties[0])
    else:
     blockers=[z for z,x in enumerate(front(v[target])) if x>=0 and x!=kind]
     outside=[(s,z) for s in unlocked if s!=target for z,x in enumerate(front(v[s])) if x<0]
     if not blockers or not outside:break
     step=(target,blockers[0],*outside[0])
    v=move(v,locks,initial,*step);path.append(step)
   if goods(v)<goods(st):yield v,path
 # Clearing a partial front exposes stock when no accessible triple exists.
 if not any(len(v)>=3 for v in groups.values()):
  for target in unlocked:
   occupied=[c for c,k in enumerate(front(st[target])) if k>=0]
   if not occupied:continue
   v=st;path=[]
   for c in occupied:
    empties=[(s,z) for s in unlocked if s!=target for z,k in enumerate(front(v[s])) if k<0]
    if not empties:break
    step=(target,c,*empties[0]);v=move(v,locks,initial,*step);path.append(step)
   else:
    if v!=st:yield v,path
def solve(st,locks,limit=12000):
 initial=goods(st);seen=set();steps=0
 def dfs(v,path):
  nonlocal steps
  if goods(v)==0:return path
  if v in seen:return None
  seen.add(v);steps+=1
  if steps>limit:return None
  candidates=list(macros(v,locks,initial));candidates.sort(key=lambda q:(goods(q[0]),len(q[1])))
  for n,p in candidates:
   ans=dfs(n,path+p)
   if ans is not None:return ans
  return None
 return dfs(st,[])
def generate(n,d,seed):
 r=random.Random(seed);shelves=[];placeholders=[]
 for s in d['s']:
  rows=[]
  for rd in s['si']:
   rows.append([KINDS[c['iid']] if c['it']==1 else -1 for c in rd['rd']])
  shelves.append(rows)
 for s,sh in enumerate(d['s']):
  for z,row in enumerate(sh['si']):
   for c,cell in enumerate(row['rd']):
    if cell['it']==-1:placeholders.append((s,z,c))
 if placeholders:
  g=d['ig'][0]['isg'][0];pool=[]
  for rec in g['icd']:pool += [KINDS[rec['id2']['iid']] if rec['id2']['it']==1 else -1]*rec['ic']
  # Preserve all explicit cells; append complete shared layers only on generator shelves.
  slots=list(placeholders);generated=sorted(set(s for s,z,c in placeholders));layer=0
  while len(slots)<len(pool):
   for s in generated:
    z=len(shelves[s]);shelves[s].append([-1]*3);slots.extend((s,z,c) for c in range(3))
   layer+=1
  # Stable weighted placement: favor a second copy on the same row (ssp),
  # matching current layer (slw), matching preceding layer (blw), base random(rw).
  for s,z,c in slots:
   if not pool:break
   candidates=collections.Counter(pool);weights=[];keys=list(candidates)
   row=shelves[s][z]
   for k in keys:
    weight=g['rw']
    if k>=0 and row.count(k)==1:weight+=g['slw']*g['ssp']
    if k>=0 and z>0 and k in shelves[s][z-1]:weight+=g['blw']
    if k>=0 and row.count(k)>=2:weight=0 # no already-complete starting triples
    weights.append(max(.001,weight)*candidates[k])
   k=r.choices(keys,weights=weights,k=1)[0];pool.remove(k);row[c]=k
 return tuple(tuple(tuple(row) for row in sh) for sh in shelves)
def main():
 levels=[]
 for n,d in enumerate(raws,1):
  locks=[s['lmc'] for s in d['s']];solution=None
  for attempt in range(400):
   seed=216500+n*1000+attempt;st=generate(n,d,seed);solution=solve(st,locks,limit=3000 if n>=4 else 20000)
   if solution is not None:break
  if solution is None:raise RuntimeError('No legal witness '+str(n))
  f=next(SRC.glob('Level%05d_*.json'%n));level={'number':n,'duration':d['tl'],'compatibleGeneration':n>=4,'seed':seed,'sourceSha256':hashlib.sha256(f.read_bytes()).hexdigest(),'shelves':[{'id':s['id'],'x':s['pos']['x'],'y':s['pos']['y'],'lockMatches':s['lmc'],'rows':[{'cells':list(row)} for row in st[i]]} for i,s in enumerate(d['s'])],'solution':[{'a':a,'b':b,'c':c,'d':dd} for a,b,c,dd in solution]};levels.append(level);print(n,'items',goods(st),'shelves',len(st),'solution moves',len(solution),'seed',seed,flush=True)
 shapes={f:i%4 for i,f in enumerate(sorted(set(s[5:].split('_')[0] for s in IIDS)))}
 colors={'red':'EF5350','green':'57C96D','blue':'4C9FFF','yellow':'FFD451','purple':'A67AF4','orange':'FF9F43','pink':'FF79BD','white':'F3EEE4','default':'64CECC'}
 items=[]
 for iid in IIDS:
  family=iid[5:].split('_')[0];tail=iid[5+len(family)+1:];col=next((v for k,v in colors.items() if k in tail),colors['default']);variant=int(hashlib.sha256(iid.encode()).hexdigest()[:2],16)
  items.append({'kind':KINDS[iid],'iid':iid,'family':family,'shape':shapes[family],'colorHex':col,'variant':variant})
 compiled={'version':1,'sourceVersion':2165,'generationNote':'Compatible deterministic weighted materialization with preserved source quotas/locks/fixed cells; original RNG is not recovered. Each result includes a no-booster legal-move witness.','items':items,'levels':levels}
 target=ROOT/'Assets/Resources/Configs/Sorting/SortExpressFirstFive.json';target.parent.mkdir(parents=True,exist_ok=True);temporary=target.with_suffix(".json.tmp");temporary.write_text(json.dumps(compiled,ensure_ascii=False,indent=2));temporary.replace(target);(OUT/'compiled-evidence.json').write_text(json.dumps({'levels':[{'number':x['number'],'seed':x['seed'],'moves':len(x['solution']),'items':sum(c>=0 for sh in x['shelves'] for row in sh['rows'] for c in row['cells']),'shelves':len(x['shelves'])} for x in levels],'distinctItemIds':len(items),'families':len(shapes)},indent=2))
if __name__=='__main__':main()
