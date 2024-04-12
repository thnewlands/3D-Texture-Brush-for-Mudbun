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
    #if UNITY_EDITOR
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
      aBrush[iStart] = brush;

      return 1;
    }
    #if MUDBUN_BURST
    [BurstCompile]
    [MonoPInvokeCallback(typeof(Sdf.SdfBrushEvalFunc))]
    [RegisterSdfBrushEvalFunc(960)]
    public static unsafe float EvaluateSdf(float res, ref float3 p, in float3 pRel, SdfBrush* aBrush, int iBrush)
    {
        float3 pRelCopy = pRel;
        float3 h = math.abs(0.5f * aBrush[iBrush].Size);
        float pivotShift = aBrush[iBrush].Data0.x;
        pRelCopy.y += pivotShift * h.y;
        return Sdf.Box(pRelCopy, h, aBrush[iBrush].Radius);
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
    #endif
  }
}

