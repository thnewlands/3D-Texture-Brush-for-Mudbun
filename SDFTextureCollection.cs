using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System;
using UnityEngine.SceneManagement;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SDFTextureCollection", order = 1)]
public class SDFTextureCollection : ScriptableObject
{
    private Texture3D[] sdfTextures;
    private int[] usersPerHeightmap;

    public ComputeShader blitShader;

    private RenderTexture sdfArray;
    public void Init(){
        RegisterShader();
    }
    private void RegisterShader(){
        if(sdfArray == null){
            InitTexArray();
        }
        string label = "_MudbunSDFTextures";
        if(!Shader.GetGlobalTexture(label)){
            Debug.Log("Registered " + label);
        }
        Shader.SetGlobalTexture(label, sdfArray);
    }

    private void OnEnable(){
        SceneManager.activeSceneChanged += (x,y) => RegisterShader();
        RegisterShader();
    }
    private void OnDisable(){
        SceneManager.activeSceneChanged -= (x,y) => RegisterShader();
    }
    [ContextMenu("Reset Values")]
    private void ResetValues(){
        RegisterShader();
        InitTexArray();
    }
    private void InitTexArray(){

        int targetSize = 512;
        int textureCount = 64; // (512 / 128) cubed

        sdfTextures = new Texture3D[textureCount];
        usersPerHeightmap = new int[textureCount];

        RenderTextureDescriptor d = new RenderTextureDescriptor(targetSize, targetSize, RenderTextureFormat.RFloat);
        d.useMipMap = true;
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
        //4 * 4 * 4 cube
        int column = index % 4; //every time we increment
        int row = Mathf.FloorToInt((float)index / 4) % 4; //every time we finish a columm
        int depth = Mathf.FloorToInt((float)index / (4*4)); //every time we finish a sheet
        return new Vector4(column, row, depth, 4.0f) * (1.0f/4.0f);
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
            blitShader.SetVector("_Origin", IndexToOrigin(texIndex) * 512);
            //Debug.Log(IndexToOrigin(texIndex));
            blitShader.Dispatch(0, texture.width / 8, texture.height / 8, texture.depth / 8);
            sdfArray.GenerateMips();
            usersPerHeightmap[texIndex]++;
            return texIndex;
        } else {
            texIndex = Array.IndexOf(sdfTextures, texture);
            usersPerHeightmap[texIndex]++;
            return texIndex;
        }
    }

    public void UnregisterTexture(Texture3D texture){
        if(sdfTextures.Contains(texture)){
            int index = Array.IndexOf(sdfTextures, texture);
            usersPerHeightmap[index]--;
            if(usersPerHeightmap[index] <= 0){
                sdfTextures[index] = null;
            }
        }
    }
}
