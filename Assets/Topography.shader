Shader "Custom/Topography"
{
    Properties
    {
        [Header(Height Mapping)]
        _HeightMin ("Height Min", Float) = 0.0
        _HeightMax ("Height Max", Float) = 5.0
        _ColorHeightMax ("Color Reference Height", Float) = 5.0
        _ColorRamp ("Color Ramp", 2D) = "white" {}

        [Header(Contour Lines)]
        _ContourInterval ("Contour Interval", Float) = 0.5
        _ContourThickness ("Contour Thickness", Float) = 0.02
        _ContourColor ("Contour Color", Color) = (0.3, 0.3, 0.3, 1)
        _DiscreteBands ("Use Discrete Bands", Float) = 0
        
        [Header(GPGPU Rendering)]
        _Procedural ("Procedural Enabled", Float) = 0
        
        [Header(Sand Textures)]
        _SandTex ("Sand Albedo", 2D) = "white" {}
        _SandNormal ("Sand Normal", 2D) = "bump" {}
        _SandRoughness ("Sand Roughness", 2D) = "white" {}
        _SandOcclusion ("Sand AO", 2D) = "white" {}
        _SandScale ("Sand Scale", Float) = 10.0
        _SandColor ("Sand Tint", Color) = (1, 1, 1, 1)

        [Header(Sparkles)]
        _SparkleScale ("Sparkle Scale", Float) = 20.0
        _SparkleThreshold ("Sparkle Threshold", Range(0.8, 1.0)) = 0.95
        _SparkleIntensity ("Sparkle Intensity", Float) = 2.0

        [Header(Water Settings)]
        _WaterLevel ("Water Level", Float) = 0.5
        _WaterColor ("Water Color", Color) = (0, 0.2, 1, 1)
        _WaterOpacity ("Water Opacity", Range(0, 1)) = 0.6
        
        [Header(Water Visuals)]
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveFreq ("Wave Frequency", Float) = 2.0
        _Glossiness ("Water Glossiness", Float) = 100.0
        _FresnelPower ("Fresnel Power", Float) = 5.0
        _SpecularColor ("Specular Color", Color) = (0.2, 0.2, 0.2, 1)
        
        _Brightness ("Brightness", Float) = 1.0
        
        [Header(Color Blending)]
        _TintStrength ("Gradient Tint Strength", Range(0.0, 1.0)) = 0.5
        _ColorShift ("Color Shift", Float) = 0.0

        [Header(Water Caustics)]
        _CausticTex ("Caustic Texture", 2D) = "black" {}
        _CausticScale ("Caustic Scale", Float) = 0.5
        _CausticSpeed ("Caustic Speed", Float) = 0.5
        _CausticIntensity ("Caustic Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _PROCEDURAL_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float2 uv2          : TEXCOORD1; // Channel for Height Data
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv2          : TEXCOORD2;
                float2 uv           : TEXCOORD3;
            };

            // SRP Batcher Compatibility (UnityPerMaterial)
            CBUFFER_START(UnityPerMaterial)
                float _HeightMin;
                float _HeightMax;
                float _ColorHeightMax;
                float _ContourInterval;
                float _ContourThickness;
                half4 _ContourColor;
                float _DiscreteBands;
                
                float _WaterLevel;
                float4 _WaterColor;
                float _WaterOpacity;

                float _WaveSpeed;
                float _WaveFreq;
                float _Glossiness;
                float _FresnelPower;
                float4 _SpecularColor;
                float _Procedural;
                
                float4 _ColorRamp_ST;
                float _Brightness;

                float _SandScale;
                float4 _SandColor;
                float _SparkleScale;
                float _SparkleThreshold;
                float _SparkleIntensity;
                
                float _TintStrength;
                float _ColorShift;

                float _CausticScale;
                float _CausticSpeed;
                float _CausticIntensity;
            CBUFFER_END

            TEXTURE2D(_ColorRamp);
            SAMPLER(sampler_ColorRamp);

            TEXTURE2D(_CausticTex);
            SAMPLER(sampler_CausticTex);

            TEXTURE2D(_SandTex);
            TEXTURE2D(_SandNormal);
            TEXTURE2D(_SandRoughness);
            TEXTURE2D(_SandOcclusion);
            SAMPLER(sampler_SandTex); // Shared sampler

            // --- ZERO-COPY GPGPU DATA ---
            struct TerrainVertex
            {
                float3 pos;
                float3 normal;
                float2 uv;
                float2 uv2;
            };

            #if defined(_PROCEDURAL_ON)
                StructuredBuffer<TerrainVertex> _VertexBuffer;
            #endif

            Varyings vert(Attributes IN, uint vid : SV_VertexID)
            {
                Varyings OUT;
                
                #if defined(_PROCEDURAL_ON)
                    // Fetch vertex data directly from GPU buffer (Zero-Copy Terrain)
                    TerrainVertex v = _VertexBuffer[vid];
                    VertexPositionInputs positionInputs = GetVertexPositionInputs(v.pos);
                    VertexNormalInputs normalInputs = GetVertexNormalInputs(v.normal);
                    OUT.uv = v.uv;
                    OUT.uv2 = v.uv2;
                    
                    OUT.positionHCS = positionInputs.positionCS;
                    OUT.positionWS = positionInputs.positionWS;
                    OUT.normalWS = normalInputs.normalWS;
                #else
                    // Standard Mesh Rendering (Side Walls / Legacy)
                    VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                    VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);
                    OUT.uv = IN.uv;
                    OUT.uv2 = IN.uv2;
                    
                    OUT.positionHCS = positionInputs.positionCS;
                    OUT.positionWS = positionInputs.positionWS;
                    OUT.normalWS = normalInputs.normalWS;
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Use UV2.x as the robust height signal (works even if mesh is flat)
                // Fallback to World Y if UV2 is empty (optional, but we'll assume script sets it)
                float height = IN.uv2.x; 

                float3 viewDir = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                // For lighting, we really want the "Flat" normal if in flat mode, 
                // OR the "Precomputed" normal if we want fake 3D lighting on a flat plane.
                // For now, let's just use the physical normal. 
                // Note: In flat mode, physical normal is (0,1,0), so lighting will look flat.
                // That's acceptable for a "projector" view.
                float3 normal = normalize(IN.normalWS);
                
                // --- 1. TRIPLANAR MAPPING (SAND) ---
                // World Space Position and Normal
                float3 worldPos = IN.positionWS * _SandScale;
                float3 absNormal = abs(normal);

                // Weights (Sharp blending via pow 8)
                float3 weights = pow(absNormal, 8.0);
                weights = weights / max(0.00001, weights.x + weights.y + weights.z);

                // Conditional sampling (Optimization optional, but we do full for quality)
                
                // Front (XY)
                float2 uvXY = worldPos.xy;
                half4 colXY = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvXY);
                half4 nrmXY = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandTex, uvXY);
                half4 rghXY = SAMPLE_TEXTURE2D(_SandRoughness, sampler_SandTex, uvXY);
                half4 occXY = SAMPLE_TEXTURE2D(_SandOcclusion, sampler_SandTex, uvXY);

                // Top (XZ)
                float2 uvXZ = worldPos.xz;
                half4 colXZ = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvXZ);
                half4 nrmXZ = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandTex, uvXZ);
                half4 rghXZ = SAMPLE_TEXTURE2D(_SandRoughness, sampler_SandTex, uvXZ);
                half4 occXZ = SAMPLE_TEXTURE2D(_SandOcclusion, sampler_SandTex, uvXZ);

                // Side (YZ)
                float2 uvYZ = worldPos.zy; // Swizzled ZY to avoid rotation
                half4 colYZ = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, uvYZ);
                half4 nrmYZ = SAMPLE_TEXTURE2D(_SandNormal, sampler_SandTex, uvYZ);
                half4 rghYZ = SAMPLE_TEXTURE2D(_SandRoughness, sampler_SandTex, uvYZ);
                half4 occYZ = SAMPLE_TEXTURE2D(_SandOcclusion, sampler_SandTex, uvYZ);

                // Blend
                half4 sandAlbedo = colXY * weights.z + colXZ * weights.y + colYZ * weights.x;
                half4 sandNormal = nrmXY * weights.z + nrmXZ * weights.y + nrmYZ * weights.x;
                half4 sandRough  = rghXY * weights.z + rghXZ * weights.y + rghYZ * weights.x;
                half4 sandAO     = occXY * weights.z + occXZ * weights.y + occYZ * weights.x;

                // Apply AO and Tint
                sandAlbedo *= sandAO;
                sandAlbedo *= _SandColor;

                // --- SPARKLES (Glitter) ---
                // Mask by Low Roughness (Smooth areas = wet/crystalline = sparkle)
                // Inverted Roughness map can act as Sparkle Mask
                // Inverted Roughness map acts as Sparkle Mask
                float sparkleMask = saturate(1.0 - sandRough.r);
                if (sparkleMask > _SparkleThreshold)
                {
                    // Use World Space Position for stable grains
                    float3 glitterVec = floor(IN.positionWS * _SparkleScale);
                    float noise = frac(sin(dot(glitterVec, float3(12.9898, 78.233, 45.164))) * 43758.5453);
                    
                    // Smooth Specular Glint (Smoothstep prevents 'strobe' flashes)
                    // Added safety check for normalize() to prevent NaNs at origin
                    float viewDependence = 0.0;
                    if (any(glitterVec)) {
                        viewDependence = dot(normalize(glitterVec), viewDir); 
                    }
                    float glint = smoothstep(0.8, 1.0, viewDependence * noise);
                    
                    sandAlbedo.rgb += glint * _SparkleIntensity * (sparkleMask - _SparkleThreshold);
                }

                // --- COLOR MAPPING MIX ---
                // Map height to 0-1 using FIXED color reference height (not geometry scale)
                // This ensures topological colors are ABSOLUTE, not relative to current HeightScale
                float colorRange = max(0.01, _ColorHeightMax - _HeightMin);
                float height01 = ((height - _HeightMin) / colorRange) + _ColorShift;
                
                // Fade out the gradient colors if the world is "Flat" (Hill Height near 0)
                // This prevents tiny sensor noise from looking like huge mountains.
                float scaleFade = saturate(_HeightMax * 2.0); 
                height01 *= scaleFade; 
                
                // Sample gradient
                
                // Discrete bands mode: posterize height into bands aligned with contours
                float sampleHeight = height01;
                if (_DiscreteBands > 0.5) {
                    // Calculate which contour interval we're in
                    float stepIndex = floor(height / _ContourInterval);
                    // Determine the world height of that step
                    float steppedHeight = stepIndex * _ContourInterval;

                    // Normalize this stepped height to get the UV for the color ramp
                    sampleHeight = ((steppedHeight - _HeightMin) / colorRange) + _ColorShift;
                    
                    // Apply the same flat-mode fade out logic 
                    sampleHeight *= scaleFade;
                }
                
                // Extended color support: fallback colors for extreme ColorShift values
                half4 gradientColor;
                if (sampleHeight < 0.0) {
                    // Below gradient: deep blue/purple for very low areas
                    half4 lowColor = half4(0.1, 0.0, 0.3, 1.0); // Deep purple
                    half4 edgeColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, float2(0.0, 0.5));
                    gradientColor = lerp(lowColor, edgeColor, saturate(sampleHeight + 1.0));
                } else if (sampleHeight > 1.0) {
                    // Above gradient: bright white/pink for very high areas
                    half4 highColor = half4(1.0, 0.8, 1.0, 1.0); // Bright pink-white
                    half4 edgeColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, float2(1.0, 0.5));
                    gradientColor = lerp(edgeColor, highColor, saturate(sampleHeight - 1.0));
                } else {
                    gradientColor = SAMPLE_TEXTURE2D(_ColorRamp, sampler_ColorRamp, float2(sampleHeight, 0.5));
                }
                
                // LUMINANCE BLEND: Preserves palette fidelity while keeping sand detail
                // 1. Calculate luminance (brightness) of the sand texture
                float lum = dot(sandAlbedo.rgb, float3(0.3, 0.59, 0.11));
                
                // 2. Apply luminance to the gradient color
                // We multiply by (lum * 2.0) so that mid-gray sand results in 100% color brightness,
                // darker sand grains create shadows, and lighter grains create highlights.
                half3 coloredSand = gradientColor.rgb * saturate(lum * 2.0);
                half4 tintedSand = half4(coloredSand, 1.0);

                // 3. Blend between pure sand and colored sand using Tint Strength
                half4 terrainColor = lerp(sandAlbedo, tintedSand, _TintStrength);
                
                // Preserve Gradient Alpha (usually 1)
                terrainColor.a = 1.0;
                
                // --- 2. Contour Lines ---
                // We utilize fwidth/ddx for consistent visual thickness regardless of slope
                
                // Calculate grid position
                float contourGrid = height / _ContourInterval;
                
                // Pixel width of the gradient (gradient magnitude)
                float fw = max(0.0001, fwidth(contourGrid));
                
                // Distance to nearest integer line, normalized by the local gradient magnitude
                float distToLine = (min(frac(contourGrid), 1.0 - frac(contourGrid))) / fw;
                
                // Calculate line strength using a pixel-based thickness
                float lineStrength = smoothstep(_ContourThickness, 0.0, distToLine);
                
                // --- Slope-Based Masking (Fix for Black Peak Blobs) ---
                // We calculate the gradient magnitude (slope). If the slope is zero (flat peak), 
                // we fade out the contour to prevent it from 'blobbing' over the entire plateau.
                float slope = length(float2(ddx(height), ddy(height)));
                float plateauMask = smoothstep(0.0, 0.001, slope); // Fade out if slope < 0.001
                lineStrength *= plateauMask;

                // Apply Contour
                terrainColor = lerp(terrainColor, _ContourColor, lineStrength);


                // --- 3. Water Logic ---
                // If under water level, blend water color and add specular highlights
                if (height < _WaterLevel)
                {
                    // Water Surface Normal
                    // UC Davis creates a normal from water texture gradient. We don't have a fluid sim texture.
                    // We simulate "waviness" by perturbing the normal with noise/sin waves as in original Unity shader.
                    float wave = sin(IN.positionWS.x * _WaveFreq + _Time.y * _WaveSpeed) 
                               + sin(IN.positionWS.z * _WaveFreq * 0.8 + _Time.y * _WaveSpeed * 1.2);
                    
                    float3 waveNormalOffset = float3(wave * 0.1, 1.0, wave * 0.1); // Small perturbation
                    float3 wetNormal = normalize(normal + waveNormalOffset * 0.2); 

                    // Specular (High Glossiness like UC Davis)
                    float3 lightDir = normalize(float3(0.5, 1.0, -0.5)); // Fixed directional light
                    float3 halfVector = normalize(lightDir + viewDir);
                    float NdotH = max(0, dot(wetNormal, halfVector));
                    float specular = pow(NdotH, _Glossiness);
                    
                    // Fresnel
                    // UC Davis mostly uses spec, assuming "shiny"
                    // We add Fresnel for viewing angle realism
                    float fresnel = pow(1.0 - saturate(dot(wetNormal, viewDir)), _FresnelPower);
                    
                    // Water Color Blending
                    // UC Davis: baseColor = mix(baseColor, waterColor, min(waterLevel * waterOpacity, 1.0))
                    // Here waterLevel is (height - actual_water_height)? No, depth.
                    float waterDepth = _WaterLevel - height;
                    float opacity = saturate(waterDepth * _WaterOpacity); // Deeper = more opaque
                    
                    half4 waterFinal = _WaterColor + (specular * _SpecularColor) + (fresnel * 0.5 * _WaterColor);
                    
                    // First, blend the sand with the water color
                    terrainColor = lerp(terrainColor, waterFinal, opacity);
                    
                    // --- CAUSTICS (Applies "through" the water volume) ---
                    // Project the caustic texture twice with different scales and speeds
                    float2 refraction = float2(wave * 0.02, wave * 0.02);
                    float2 causticUV1 = (IN.positionWS.xz + refraction) * _CausticScale + _Time.y * _CausticSpeed;
                    float2 causticUV2 = (IN.positionWS.xz - refraction) * (_CausticScale * 0.77) - _Time.y * (_CausticSpeed * 0.6);
                    
                    half4 c1 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, causticUV1);
                    half4 c2 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, causticUV2);
                    
                    // Sharpen and Combine: Squaring the result makes the light "webs" thinner and brighter
                    float causticPattern = pow(abs(c1.r * c2.r), 1.5); 
                    
                    // Museum Punch: Applied after the water blend so it pierces through the blue opacity
                    float causticDepthFade = saturate(waterDepth * 5.0);
                    terrainColor.rgb += causticPattern * _CausticIntensity * causticDepthFade * 5.0;
                }

                // --- 4. Brightness Adjustment (Wall Dimming) ---
                terrainColor.rgb *= _Brightness;
                
                // FINAL SAFETY CLAMP: Prevents random NaNs or Infinity from flashing the screen
                terrainColor.rgb = saturate(terrainColor.rgb);

                return terrainColor;
            }
            ENDHLSL
        }
    }
}
