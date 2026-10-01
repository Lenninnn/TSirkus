Shader "TSirkus/Abandoned Bear URP"
{
 Properties {
  [MainTexture] _BaseMap("Original UV details",2D)="white"{}
  _DetailAlbedoMap("Fur repair and dirt",2D)="white"{}
  [MainColor] _BaseColor("Tint",Color)=(.9,.9,.9,1)
  _Smoothness("Smoothness",Range(0,1))=.12
  _Cutoff("Cutoff",Range(0,1))=.5
  [HideInInspector] _Cull("Cull",Float)=2
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Name "ForwardLit"
   Tags {"LightMode"="UniversalForwardOnly"}
   Cull Back ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.0
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   TEXTURE2D(_DetailAlbedoMap); SAMPLER(sampler_DetailAlbedoMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _Smoothness;
    half _Cutoff;
    half _Cull;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; half fog:TEXCOORD3; half3 vertexLighting:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
   Varyings Vert(Attributes a) {
    Varyings o=(Varyings)0; UNITY_SETUP_INSTANCE_ID(a); UNITY_TRANSFER_INSTANCE_ID(a,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);
    o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=TransformObjectToWorldNormal(a.normalOS);
    o.uv=TRANSFORM_TEX(a.uv,_BaseMap); o.fog=ComputeFogFactor(p.positionCS.z); o.vertexLighting=VertexLighting(p.positionWS,o.normalWS); return o;
   }
   half4 Frag(Varyings i):SV_Target {
    UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    half4 original=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
    // Sample only the lower-right fur region of the generated atlas: no facial islands.
    float2 patchUV=float2(.72,.04)+frac(i.uv*3.0)*float2(.22,.20);
    half3 fur=SAMPLE_TEXTURE2D(_DetailAlbedoMap,sampler_DetailAlbedoMap,patchUV).rgb;
    half brightness=max(original.r,max(original.g,original.b));
    half valid=original.a*smoothstep(.01,.035,brightness);
    half3 base=lerp(fur*.65,original.rgb,valid);
    half lum=dot(base,half3(.2126,.7152,.0722));
    base=lerp(base,lum.xxx,.28);
    // Preserve muzzle, eyes and claws; texture variation provides a light dust layer.
    half dust=saturate(dot(fur,half3(.3,.59,.11))-.18)*.18;
    base=lerp(base,half3(.24,.20,.15),dust)*_BaseColor.rgb;
    InputData inputData=(InputData)0;
    inputData.positionWS=i.positionWS; inputData.normalWS=NormalizeNormalPerPixel(i.normalWS);
    inputData.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
    inputData.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
    inputData.fogCoord=i.fog; inputData.vertexLighting=i.vertexLighting;
    inputData.bakedGI=SampleSH(inputData.normalWS);
    inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
    inputData.shadowMask=half4(1,1,1,1);
    SurfaceData surface=(SurfaceData)0;
    surface.albedo=base;surface.metallic=0;surface.specular=half3(.04,.04,.04);
    surface.smoothness=_Smoothness;surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;
    half4 color=UniversalFragmentPBR(inputData,surface);color.rgb=MixFog(color.rgb,i.fog);return color;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
 FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
