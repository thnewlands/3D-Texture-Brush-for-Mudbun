//Basic idea from here:
//https://kosmonautblog.wordpress.com/2017/05/09/signed-distance-field-rendering-journey-pt-2/
//Extra guidance from here:
//https://github.com/EmmetOT/IsoMesh/blob/bcc8e437683111b68d2692041967919746c3d95a/IsoMesh/Assets/Source/HLSL/MapSignedDistanceField.hlsl#L202

#define kSDFTextureSolid (960)
//TODO: This is a large CGInclude used for "UNITY_DECLARE_TEX3D_FLOAT" 
//      Could theoretically be reduced to a smaller snippet. 
#include "UnityCG.cginc"

UNITY_DECLARE_TEX3D_FLOAT(_MudbunSDFTextures);

float sample_sdf_tex3D_distance(float3 position, float3 boundsMin, float3 boundsMax)
{
    position = clamp(position, boundsMin, boundsMax);
    float result = UNITY_SAMPLE_TEX3D_LOD(_MudbunSDFTextures, position, 0);
    return result;
}

float3 sample_sdf_tex3D_gradient(float3 position, float3 boundsMin, float3 boundsMax, float sampleRadius)
{
    float3 normal = float3(0, 0, 0);
    normal += float3(1, -1, -1) * sample_sdf_tex3D_distance(position + float3(1, -1, -1) * sampleRadius, boundsMin, boundsMax);
    normal += float3(-1, -1, 1) * sample_sdf_tex3D_distance(position + float3(-1, -1, 1) * sampleRadius, boundsMin, boundsMax);
    normal += float3(-1, 1, -1) * sample_sdf_tex3D_distance(position + float3(-1, 1, -1) * sampleRadius, boundsMin, boundsMax);
    normal += float3(1, 1, 1) * sample_sdf_tex3D_distance(position + float3(1, 1, 1) * sampleRadius, boundsMin, boundsMax);
    return normal;
}

float sdf_texture3D_brush(SdfBrush brush, float3 pRel)
{
    float3 scale = brush.data2.xyz;
    float3 origin = brush.data1;
    float shift = brush.data0.y;
    
    //TOOD: Cleanup magic numbers and replace with numbers related to cube scale.
    float insetFromBorder = 0.001;
    float sampleRadius = 0.01;
            
    float3 boundsMin = origin + insetFromBorder;
    float3 boundsMax = origin + .25f - insetFromBorder;
    float3 localPosition = pRel.xyz / scale.xyz;
            
    float3 clampedPosition = clamp(localPosition, -.5, .5); //pRel / scale in range -.5, .5
    float3 textureSpacePosition = ((clampedPosition + .5) / 4) + origin; //position within 3D Texture
    float3 gradient = sample_sdf_tex3D_gradient(textureSpacePosition, boundsMin, boundsMax, sampleRadius);
    float dist = sample_sdf_tex3D_distance(textureSpacePosition, boundsMin, boundsMax);

    float3 vectorInBounds = -normalize(gradient) * dist;
    float3 vectorToBounds = clampedPosition - localPosition;
    float3 combinedVector = vectorInBounds + vectorToBounds;
    combinedVector *= scale; //TODO: Handle this sometimes giving incorrect values.
    float result = length(combinedVector) * sign(dist);
    result += shift;
    return result;
}
