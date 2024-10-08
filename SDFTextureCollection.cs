using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
using Unity.Collections;
using UnityEngine.Events;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SDFTextureCollection", order = 1)]
public class SDFTextureCollection : ScriptableObject
{
    private Texture3D[] sdfTextures;
    private int[] usersPerTexture;

    public ComputeShader blitShader;
    public UnityEvent OnRegenerate;

    private RenderTexture sdfArray;
    private const string textureName = "_MudbunSDFTextures";


    public enum Dimension { _4x4x4 = 4, _3x3x3 = 3, _2x2x2 = 2, _1x1x1 = 1 };
    public enum Resolution { _512 = 512, _256 = 256, _128 = 128, _64 = 64, _32 = 32 };
    public Resolution resolutionPerSDF = Resolution._128;
    public Dimension numberOfSDFPerDimension = Dimension._4x4x4;

    private bool useMipmaps = false;

    //TODO: This is a major hack added to handle an exception 
    //      "Compute shader (MarchingCubes): Property (_MudbunSDFTextures) at kernel index (0) is not set"
    //      On line 828 of MudRenderer in the OnEnable function I add "SDFTextureCollection.Instance.Init();"
    //      This initializes the array in scenes including ones that don't utilize this brush type.
    //      I'm not happy with this solution at the moment but it's better than nothing.
    //      The main issue with it is that it adds a graphics memory overhead to scenes that don't need it.
    //
    //      I've looked into shader variants with keywords but haven't had much success.
    //      I think part of the problem is nested CGIncludes and the other part is inconsistencies between frag / vert shader / compute shader code
    private static SDFTextureCollection _instance;
    public static SDFTextureCollection Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SDFTextureCollection>("SDFTextureCollection");
                Debug.Log("Loaded instance " + _instance.name);
                return _instance;
            }
            else
            {
                return _instance;
            }
        }
    }

    public void Init(){
        RegisterShader();
    }
    private void RegisterShader(){
        if(sdfArray == null){
            InitTexArray();
        }
        if(!Shader.GetGlobalTexture(textureName)){
            Debug.Log("Registered " + textureName);
        }
        Shader.SetGlobalTexture(textureName, sdfArray);
        Shader.SetGlobalFloat("_MudbunSDFTexturesPerDimension", (int)numberOfSDFPerDimension);
        Shader.SetGlobalFloat("_MudbunSDFTextureResolution", (int)resolutionPerSDF);
    }

    private void OnEnable(){
        SceneManager.activeSceneChanged += (x,y) => RegisterShader();
        RegisterShader();
    }
    private void OnDisable(){
        SceneManager.activeSceneChanged -= (x,y) => RegisterShader();
    }
    [ContextMenu("Reset Values")]
    private void ResetValues()
    {
        RegisterShader();
        InitTexArray();
    }

    private int GetTextureSize()
    {
        return (int)resolutionPerSDF * (int)numberOfSDFPerDimension;
    }

    private void InitTexArray(){

        //how big of a texture do you want 
        //how many do you want

        int targetSize = GetTextureSize();
        int textureCount = (int)numberOfSDFPerDimension * (int)numberOfSDFPerDimension * (int)numberOfSDFPerDimension;

        sdfTextures = new Texture3D[textureCount];
        usersPerTexture = new int[textureCount];

        RenderTextureDescriptor d = new RenderTextureDescriptor(targetSize, targetSize, RenderTextureFormat.RHalf);
        d.useMipMap = useMipmaps;
        d.autoGenerateMips = false;
        d.mipCount = 8;
        d.dimension = TextureDimension.Tex3D;
        d.volumeDepth = targetSize;
        d.enableRandomWrite = true;

        sdfArray = new RenderTexture(d);
        sdfArray.wrapMode = TextureWrapMode.Clamp;
        sdfArray.filterMode = FilterMode.Trilinear;
        sdfArray.Create();
        sdfArray.name = this.name;

        Debug.Log("Created SDF Array: " + sdfArray.name);
    }

    private int GetNextFreeIndex(){
        for(int index = 0; index < sdfTextures.Length; index++){
            if(sdfTextures[index] == null){
                return index;
            }
        }
        return -1;
    }

    public Vector4 IndexToOrigin(int index){
        int column = index % (int)numberOfSDFPerDimension; //every time we increment
        int row = Mathf.FloorToInt((float)index / (int)numberOfSDFPerDimension) % (int)numberOfSDFPerDimension; //every time we finish a columm
        int depth = Mathf.FloorToInt((float)index / ((int)numberOfSDFPerDimension * (int)numberOfSDFPerDimension)); //every time we finish a sheet
        return new Vector4(column, row, depth, (int)numberOfSDFPerDimension) * (1.0f/ (int)numberOfSDFPerDimension);
    }

    public Texture3D GetTexture(int index)
    {
        return sdfTextures[index];
    }

    public int RegisterTexture(Texture3D texture){
        RegisterShader();

        int texIndex;
        if(texture == null){
            Debug.LogWarning("Texture not yet set");
            return -1;
        }
        if(sdfArray == null){
            InitTexArray();
        }
        if(!sdfTextures.Contains(texture)){
            texIndex = GetNextFreeIndex();
            if(texIndex == -1){
                Debug.LogWarning("Too many textures in " + this.name);
                return -1;
            }
            sdfTextures[texIndex] = texture;
            blitShader.SetTexture(0, "_Result", sdfArray);
            blitShader.SetTexture(0, "_Source", texture);
            blitShader.SetVector("_Scale", Vector3.one * ((float)texture.width / (float)resolutionPerSDF));
            blitShader.SetVector("_Origin", IndexToOrigin(texIndex) * GetTextureSize());
            //Debug.Log(IndexToOrigin(texIndex));
            blitShader.Dispatch(0, (int)resolutionPerSDF, (int)resolutionPerSDF, (int)resolutionPerSDF);
            if (sdfArray.useMipMap)
            {
                sdfArray.GenerateMips();
            }
            usersPerTexture[texIndex]++;
            Debug.LogWarning("Registered " + texture.name);
            return texIndex;
        } else {
            Debug.LogWarning("Re-using " + texture.name);
            texIndex = Array.IndexOf(sdfTextures, texture);
            usersPerTexture[texIndex]++;
            return texIndex;
        }
    }
    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        Dispose();
        Init();
        OnRegenerate.Invoke();
    }

    public void UnregisterTexture(Texture3D texture){
        if(sdfTextures.Contains(texture)){
            int index = Array.IndexOf(sdfTextures, texture);
            usersPerTexture[index]--;
            if(usersPerTexture[index] <= 0){
                sdfTextures[index] = null;
            }
        }
    }

    private void Dispose()
    {
        sdfArray.Release();
        sdfArray = null;
    }
}
