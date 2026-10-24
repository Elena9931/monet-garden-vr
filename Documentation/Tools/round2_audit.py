from pathlib import Path
import hashlib,json
p=Path(r"D:\新建文件夹\111")
b=Path(r"C:\Users\25221\Documents\Codex\2026-10-08\https-chatgpt-com-share-6ac7af07-58c0\work\Round2Backup_20261009")
def sha(x):
 h=hashlib.sha256()
 with x.open('rb') as f:
  for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
 return h.hexdigest()
changes=[];added=[];missing=[]
for base in ['Assets','Packages','ProjectSettings']:
 for old in (b/base).rglob('*'):
  if not old.is_file():continue
  rel=old.relative_to(b);now=p/rel
  if not now.exists():missing.append(str(rel))
  elif sha(old)!=sha(now):changes.append(str(rel))
 for now in (p/base).rglob('*'):
  if now.is_file() and not (b/now.relative_to(p)).exists():added.append(str(now.relative_to(p)))
data={'changed':changes,'added':added,'missing':missing,'originalPaintingSHA256':sha(p/'Assets/MonetGarden/References/Reference03_MonetOriginal.png'),'backup':str(b)}
(p/'Documentation/Validation/round2_file_audit.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')
print(json.dumps(data,ensure_ascii=False,indent=2))
