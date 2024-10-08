/******************************************************************************/
/*
Project   - MudBun
Publisher - Long Bunny Labs
            http://LongBunnyLabs.com
Author    - Ming-Lun "Allen" Chou
            http://AllenChou.net
*/
/******************************************************************************/

using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using AOT;

#if MUDBUN_BURST
using Unity.Burst;
using Unity.Mathematics;
#endif

namespace MudBun
{
    #if MUDBUN_BURST
    [BurstCompile]
    #endif
    public class SDFTextureSolidBrush : MudSolid
    {
        // this value matches kCustomDistortion in CustomBrush.cginc
        public static readonly int TypeId = 960;
        private Texture3D lastSDFTexture;
        public Texture3D sdfTexture;
        private int index = -1;
        public SDFTextureCollection collection;

        [SerializeField] [Range(-0.1f, 0.1f)] private float m_offset = 0.0f;
        public float Offset { get => m_offset; set { m_offset = value; MarkDirty(); } }

        private void Start(){
            TryUpdateTexture();
        }

        public override Aabb RawBoundsRs
        {
            get
            {
            Vector3 posRs = PointRs(transform.position);
            Vector3 r = 0.5f * VectorUtil.Abs(transform.localScale);
            Aabb bounds = new Aabb(-r, r);
            bounds.Rotate(RotationRs(transform.rotation));
            bounds.Min += posRs;
            bounds.Max += posRs;
            return bounds;
            }
        }

        public override void SanitizeParameters()
        {
            base.SanitizeParameters();
            if(lastSDFTexture != sdfTexture){
                TryUpdateTexture();
            }
        }

        private void TryUpdateTexture(){
            if(sdfTexture){
                if(lastSDFTexture){
                    collection.UnregisterTexture(lastSDFTexture);
                }
                index = collection.RegisterTexture(sdfTexture);
                lastSDFTexture = sdfTexture;
            }
        }

        public override int FillComputeData(NativeArray<SdfBrush> aBrush, int iStart, List<Transform> aBone)
        {
            SdfBrush brush = SdfBrush.New();
            if(index == -1){
                TryUpdateTexture();
            }
            brush.Type = TypeId;
            brush.Data0.y = m_offset;
            brush.Data1 = collection.IndexToOrigin(index);
            brush.Data2 = transform.localScale;
            brush.Data3.x = index;

            if (aBone != null)
            {
                brush.BoneIndex = aBone.Count;
                aBone.Add(gameObject.transform);
            }

            aBrush[iStart] = brush;

            return 1;
        }
        #if MUDBUN_BURST
        [BurstCompile]
        [MonoPInvokeCallback(typeof(Sdf.SdfBrushEvalFunc))]
        [RegisterSdfBrushEvalFunc(960)]
        public static unsafe float EvaluateSdf(float res, ref float3 p, in float3 pRel, SdfBrush* aBrush, int iBrush)
        {
            int textureIndex = (int)math.round(aBrush[iBrush].Data3.x);

            float3 scale = new float3(aBrush[iBrush].Data2.x, aBrush[iBrush].Data2.y, aBrush[iBrush].Data2.z);
            float3 origin = new float3(aBrush[iBrush].Data1.x, aBrush[iBrush].Data1.y, aBrush[iBrush].Data1.z);
            float shift = aBrush[iBrush].Data0.y;

            //TOOD: Cleanup magic numbers and replace with numbers related to cube scale.
            float insetFromBorder = 1.0f / SDFTextureCollection.MudbunSDFTextureResolution.Data ;
            float sampleRadius = .1f;

            float3 boundsMin = origin + insetFromBorder;
            float3 boundsMax = origin - insetFromBorder;
            float3 localPosition = pRel.xyz / scale.xyz;

            float3 clampedPosition = math.clamp(localPosition, new float3(-.5f), new float3(.5f)); //pRel / scale in range -.5, .5

            float3 gradient = sample_sdf_tex3D_gradient(textureIndex, ref localPosition, ref boundsMin, ref boundsMax, sampleRadius);
            float dist = sample_sdf_tex3D_distance(textureIndex, ref localPosition, ref boundsMin, ref boundsMax);

            float3 vectorInBounds = -math.normalize(gradient) * dist;
            float3 vectorToBounds = clampedPosition - localPosition;
            float3 combinedVector = vectorInBounds + vectorToBounds;
            combinedVector *= scale; //TODO: Handle this sometimes giving incorrect values.
            float result = math.length(combinedVector) * math.sign(dist);
            result += shift;
            return result;
        }

        public static unsafe float sample_sdf_tex3D_distance(int textureIndex, ref float3 position, ref float3 boundsMin, ref float3 boundsMax)
        {
            position = math.clamp(position, boundsMin, boundsMax);
            if (SDFTextureCollection.DataCache.Data.IsCreated)
            {
                return SDFTextureCollection.DataCache.Data[textureIndex];
            } else
            {
                return 0;
            }
        }

        public static unsafe float3 sample_sdf_tex3D_gradient(int textureIndex, ref float3 position, ref float3 boundsMin, ref float3 boundsMax, float sampleRadius)
        {
            float3 normal = new float3(0, 0, 0);

            float3 pos1 = position + new float3(1, -1, -1) * sampleRadius;
            float3 pos2 = position + new float3(-1, -1, 1) * sampleRadius;
            float3 pos3 = position + new float3(-1, 1, -1) * sampleRadius;
            float3 pos4 = position + new float3(1, 1, 1) * sampleRadius;

            normal += new float3(1, -1, -1) * sample_sdf_tex3D_distance(textureIndex, ref pos1, ref boundsMin, ref boundsMax);
            normal += new float3(-1, -1, 1) * sample_sdf_tex3D_distance(textureIndex, ref pos2, ref boundsMin, ref boundsMax);
            normal += new float3(-1, 1, -1) * sample_sdf_tex3D_distance(textureIndex, ref pos3, ref boundsMin, ref boundsMax);
            normal += new float3(1, 1, 1) * sample_sdf_tex3D_distance(textureIndex, ref pos4, ref boundsMin, ref boundsMax);
            return normal;
        }


#endif
        public override void DrawSelectionGizmosRs()
        {
            base.DrawSelectionGizmosRs();

            GizmosUtil.DrawInvisibleBox(PointRs(transform.position), transform.localScale, RotationRs(transform.rotation));
        }

        public override void DrawOutlineGizmosRs()
        {
            base.DrawOutlineGizmosRs();

            GizmosUtil.DrawWireBox(PointRs(transform.position), transform.localScale, RotationRs(transform.rotation));
        }
        private void OnDestroy(){
            collection.UnregisterTexture(sdfTexture);
        }
    }
}

