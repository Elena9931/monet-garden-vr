using UnityEngine;
namespace MonetGarden
{
    public static class GardenGlobe
    {
        // Hand-drawn continent polygons: a schematic catalogue globe, not a GIS map.
        static readonly Vector2[][] Land = {
            new [] { new Vector2(-18,36),new Vector2(5,37),new Vector2(35,31),new Vector2(51,12),new Vector2(34,-25),new Vector2(20,-35),new Vector2(9,-9),new Vector2(-17,15) },
            new [] { new Vector2(-10,36),new Vector2(-10,60),new Vector2(15,72),new Vector2(50,68),new Vector2(110,72),new Vector2(165,55),new Vector2(141,30),new Vector2(120,18),new Vector2(100,1),new Vector2(72,22),new Vector2(50,14),new Vector2(35,40),new Vector2(10,43) },
            new [] {new Vector2(-168,65),new Vector2(-125,72),new Vector2(-60,55),new Vector2(-82,10),new Vector2(-106,22),new Vector2(-130,49)},
            new [] {new Vector2(-81,12),new Vector2(-48,-1),new Vector2(-35,-9),new Vector2(-67,-55),new Vector2(-77,-20)},
            new [] {new Vector2(113,-22),new Vector2(132,-12),new Vector2(153,-24),new Vector2(145,-39),new Vector2(115,-35)}
        };
        static bool Inside(Vector2 p, Vector2[] polygon)
        {
            bool inside = false;
            for (int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
                if ((polygon[i].y>p.y)!=(polygon[j].y>p.y) && p.x<(polygon[j].x-polygon[i].x)*(p.y-polygon[i].y)/(polygon[j].y-polygon[i].y)+polygon[i].x) inside=!inside;
            return inside;
        }
        public static Texture2D CreateTexture(int size)
        {
            var texture = new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels = new Color[size*size];
            Quaternion orientation = Quaternion.Euler(-27,-7,0);
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float nx=2f*x/(size-1)-1, ny=2f*y/(size-1)-1;
                float r=nx*nx+ny*ny;
                if(r>1) { pixels[y*size+x]=Color.clear; continue; }
                float nz=Mathf.Sqrt(1-r);
                Vector3 normal=orientation*new Vector3(nx,ny,nz);
                float lat=Mathf.Asin(normal.y)*Mathf.Rad2Deg;
                float lon=Mathf.Atan2(normal.x,normal.z)*Mathf.Rad2Deg;
                bool land=false; foreach(var p in Land) if(Inside(new Vector2(lon,lat),p)) {land=true;break;}
                Color c=land?new Color(.37f,.55f,.38f):new Color(.10f,.27f,.3f);
                if(Mathf.Abs(Mathf.DeltaAngle(lon%30,0))<.6f || Mathf.Abs(lat%15)<.5f) c=Color.Lerp(c,new Color(.75f,.84f,.74f),.27f);
                float light=.4f+.6f*Mathf.Clamp01(Vector3.Dot(new Vector3(nx,ny,nz),new Vector3(-.45f,.5f,.8f).normalized));
                c*=light;c.a=1;
                if(Mathf.Abs(lon-1.534f)<2 && Mathf.Abs(lat-49.075f)<2) c=new Color(1,.78f,.35f);
                pixels[y*size+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply();return texture;
        }
    }
}
