from pathlib import Path
from PIL import Image, ImageDraw
import zipfile,numpy as np,hashlib,json
root=Path(r'D:\新建文件夹\111');work=Path(r'C:\Users\25221\Documents\Codex\2026-10-08\https-chatgpt-com-share-6ac7af07-58c0\work')
z=zipfile.ZipFile(work/'NE2_LR_LC.zip');print(z.namelist())
tif=next(n for n in z.namelist() if n.lower().endswith('.tif'))
with z.open(tif) as f: base=Image.open(f).convert('RGB').resize((4096,2048),Image.Resampling.LANCZOS)
a=np.asarray(base).astype(np.float32)/255;h,w=a.shape[:2];rng=np.random.default_rng(1901)
mask=Image.new('L',(w,h));md=ImageDraw.Draw(mask)
geo=json.loads((work/'ne_10m_land.geojson').read_text())
for feature in geo['features']:
 g=feature['geometry'];polys=g['coordinates'] if g['type']=='MultiPolygon' else [g['coordinates']]
 for rings in polys:
  for j,ring in enumerate(rings):md.polygon([((lon+180)/360*(w-1),(90-lat)/180*(h-1)) for lon,lat in ring],fill=255 if j==0 else 0)
ocean=np.asarray(mask)==0
noise=np.asarray(Image.fromarray((rng.random((128,256))*255).astype('uint8')).resize((w,h),Image.Resampling.BICUBIC))/255
blue=np.array([14,43,105]);teal=np.array([29,94,133]);gold=np.array([209,174,78]);green=np.array([53,108,83]);ochre=np.array([161,119,61])
sea=blue+(teal-blue)*np.clip(noise*.75,0,1)[:,:,None]
greenness=np.clip((a[:,:,1]-a[:,:,0])*7+.36,0,1)
land=gold+(green-gold)*greenness[:,:,None];land=land*.82+(ochre+(gold-ochre)*noise[:,:,None])*.18
col=np.where(ocean[:,:,None],sea,land);col*=.88+.23*noise[:,:,None]
img=Image.fromarray(np.clip(col,0,255).astype('uint8'));draw=ImageDraw.Draw(img,'RGBA')
for _ in range(210000):
 x=int(rng.integers(w));y=int(rng.integers(h));length=int(rng.integers(4,21));thick=int(rng.integers(1,5));dy=int(-length*.32+rng.integers(-2,3));endx=x+length
 if ocean[y,x]!=ocean[max(0,min(h-1,y+dy)),endx%w]:continue
 c=np.clip(col[y,x]+rng.uniform(-25,31),0,255).astype(int);alpha=int(rng.integers(100,205))
 for offset in (0,-w):
  draw.line((x+offset,y,endx+offset,y+dy),fill=tuple(c)+(alpha,),width=thick)
  draw.line((x+offset,y-1,endx+offset,y+dy-1),fill=tuple(np.clip(c+10,0,255))+(65,),width=1)
a=np.array(img);edge=((a[:,0].astype(float)+a[:,-1].astype(float))*.5).astype('uint8');a[:,0]=edge;a[:,-1]=edge
out=root/'Assets/MonetGarden/Rendering/Earth_Impressionist_Albedo.png';Image.fromarray(a).save(out)
print('TEXTURE',out,'4096x2048','seam difference',int(np.abs(a[:,0].astype(int)-a[:,-1].astype(int)).max()))
print('SHA256',hashlib.sha256(out.read_bytes()).hexdigest())
(root/'Documentation/EARTH_TEXTURE_SOURCE.md').write_text('Natural Earth II NE2_LR_LC, version 3.2.0. Public domain: https://www.naturalearthdata.com/about/terms-of-use/\nSource: https://naturalearth.s3.amazonaws.com/10m_raster/NE2_LR_LC.zip\n4096x2048 plate carree. North at top, longitude -180 to +180. Programmatic multilayer brush synthesis, not AI. Coastline coordinates retained. First/last columns identical. Reference guides palette only.\n',encoding='utf-8')
